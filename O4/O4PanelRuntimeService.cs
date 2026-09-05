using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Crisis;
using EmergencyEvents.Director;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using Exiled.API.Features;
using MEC;

namespace EmergencyEvents.O4;

/// <summary>
/// M06 EXILED 运行时适配层，只写 O4 的单个 Hint 通道并协调投票超时。
/// </summary>
public sealed class O4PanelRuntimeService : IO4EventSelector
{
    private const int MaxRoundLocalO4Ids = 128;
    private readonly O4PanelConfig config;
    private readonly IO4EligibilityProvider eligibilityProvider;
    private readonly O4PanelRenderer renderer;
    private readonly O4VoteService voteService;
    private readonly Dictionary<int, string> roundLocalO4Ids = new Dictionary<int, string>();
    private CoroutineHandle refreshHandle;
    private CoroutineHandle selectionTimeoutHandle;
    private bool hasRefreshHandle;
    private bool hasSelectionTimeoutHandle;
    private bool isRoundActive;
    private bool isSuspended;
    private bool isUnavailable;
    private int nextRoundLocalO4Number = 1;
    private long roundId;
    private O4PanelViewModel? currentModel;
    private O4SelectionRequest? activeRequest;
    private Action<O4SelectionResult>? activeCompletion;

    public O4PanelRuntimeService(O4PanelConfig config, IO4EligibilityProvider? eligibilityProvider = null)
    {
        this.config = (config ?? new O4PanelConfig()).Normalize();
        this.eligibilityProvider = eligibilityProvider ?? new ExiledO4EligibilityProvider();
        renderer = new O4PanelRenderer(this.config);
        voteService = new O4VoteService(this.config);
    }

    public bool IsAvailable => config.Enabled && config.EnableEventSelection && isRoundActive && !isSuspended && !isUnavailable;

    public bool IsPanelRunning => config.Enabled && isRoundActive && !isSuspended && hasRefreshHandle;

    public bool IsSelectionOpen => voteService.ActiveSession is not null;

    public long RoundId => roundId;

    public int EligibleO4Count => GetCurrentO4Snapshots().Count;

    public string SessionId => voteService.ActiveSession?.SessionId ?? string.Empty;

    public long CycleId => voteService.ActiveSession?.CycleId ?? 0L;

    public int VotesReceived => voteService.ActiveSession?.VoteCount ?? 0;

    public int TimeRemainingSeconds
    {
        get
        {
            O4VoteSession? session = voteService.ActiveSession;
            if (session is null)
            {
                return 0;
            }

            return Math.Max(0, (int)Math.Ceiling((session.EndsAt - DateTime.UtcNow).TotalSeconds));
        }
    }

    public void StartRound(long currentRoundId)
    {
        CleanupRound("Restart");
        if (currentRoundId <= 0L)
        {
            return;
        }

        roundId = currentRoundId;
        isRoundActive = true;
        isSuspended = false;
        isUnavailable = false;
        LogInfo("O4_PANEL_STARTED", $"RoundId={roundId};Enabled={config.Enabled};SelectionEnabled={config.EnableEventSelection}");
        if (config.Enabled)
        {
            ScheduleRefresh(0.05f);
        }
    }

    public void UpdateEvaluation(
        DlrcEvaluationCompletedEvent completedEvent,
        CrisisAssessment? assessment,
        FacilityDisorderBand fdiBand,
        DateTime? nextEvaluationAt)
    {
        if (!isRoundActive || completedEvent is null || completedEvent.Snapshot.RoundId != roundId)
        {
            return;
        }

        if (!completedEvent.Result.IsValid || (assessment is not null && assessment.EvaluationId != completedEvent.EvaluationId))
        {
            isUnavailable = true;
            CancelAll("INVALIDATED");
            ShowToEligible(renderer.RenderUnavailable());
            LogWarn("O4_PANEL_UNAVAILABLE", $"RoundId={roundId};EvaluationId={completedEvent.EvaluationId};Reason=InvalidUpstream");
            return;
        }

        isUnavailable = false;
        string code = assessment?.EvaluationId == completedEvent.EvaluationId && !string.IsNullOrWhiteSpace(assessment.Code)
            ? assessment.Code
            : completedEvent.Result.Code;
        currentModel = new O4PanelViewModel(
            code,
            completedEvent.Result.FinalLevel,
            completedEvent.Result.ControlState,
            assessment?.EvaluationId == completedEvent.EvaluationId ? assessment.ActiveTags : Array.Empty<CrisisTag>(),
            fdiBand,
            completedEvent.Snapshot.Timestamp,
            nextEvaluationAt);
    }

    public bool TryCastVote(Player player, int candidateIndex, out string response)
    {
        response = "当前没有开放的 O4 投票。";
        if (!IsSelectionOpen || player is null || !eligibilityProvider.IsEligible(player))
        {
            return false;
        }

        if (!roundLocalO4Ids.TryGetValue(player.Id, out string? localO4Id))
        {
            localO4Id = GetOrAssignLocalId(player.Id);
            if (localO4Id is null)
            {
                response = "O4 局内编号已达到上限。";
                return false;
            }
        }

        O4VoteSession? session = voteService.ActiveSession;
        if (session is null)
        {
            return false;
        }

        bool succeeded = voteService.TryCastVote(
            session.SessionId,
            localO4Id,
            candidateIndex,
            DateTime.UtcNow,
            true,
            out _,
            out string reason);
        if (!succeeded)
        {
            response = $"投票被拒绝：{reason}。";
            return false;
        }

        O4SelectionRequest? request = activeRequest;
        string candidateId = request is not null
            && candidateIndex >= 1
            && candidateIndex <= request.Candidates.Count
            ? request.Candidates[candidateIndex - 1].EventId
            : "UNKNOWN";
        LogInfo(
            "O4_VOTE_CAST",
            $"RoundId={roundId};SessionId={session.SessionId};RoundLocalO4Id={localO4Id};CandidateId={candidateId};VoteAccepted=true");
        response = "O4 投票已记录。";
        return true;
    }

    public void RequestSelection(O4SelectionRequest request, Action<O4SelectionResult> completed)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (completed is null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        if (!IsAvailable || request.RoundId != roundId)
        {
            completed(O4SelectionResult.Skipped(request.RoundId, request.CycleId, request.SessionId, "NO_O4_AVAILABLE", DateTime.UtcNow));
            return;
        }

        IReadOnlyList<O4PlayerSnapshot> eligibleO4 = GetCurrentO4Snapshots();
        bool opened = voteService.TryOpenSession(request, eligibleO4, DateTime.UtcNow, out O4SelectionResult immediateResult);
        if (!opened)
        {
            string action = immediateResult.Outcome == O4SelectionOutcome.SKIPPED
                ? "O4_SELECTION_SKIPPED"
                : "O4_SELECTION_FALLBACK";
            LogInfo(action, $"RoundId={request.RoundId};SessionId={request.SessionId};Reason={immediateResult.Reason}");
            completed(immediateResult);
            return;
        }

        activeRequest = request;
        activeCompletion = completed;
        ScheduleSelectionTimeout();
        O4VoteSession session = voteService.ActiveSession!;
        LogInfo(
            "O4_SELECTION_REQUESTED",
            $"RoundId={request.RoundId};CycleId={request.CycleId};SessionId={request.SessionId};Candidates={request.Candidates.Count};EligibleO4Count={eligibleO4.Count};VoteDuration={config.VoteDurationSeconds}");
    }

    public void CancelAll(string reason)
    {
        KillSelectionTimeout();
        O4SelectionResult? result = voteService.CancelActive(reason, DateTime.UtcNow);
        Action<O4SelectionResult>? completed = activeCompletion;
        activeRequest = null;
        activeCompletion = null;
        if (result is not null)
        {
            LogInfo("O4_SELECTION_CANCELLED", $"RoundId={result.RoundId};CycleId={result.CycleId};SessionId={result.SessionId};Reason={reason}");
            completed?.Invoke(result);
        }
    }

    public void SuspendForRound(string reason)
    {
        if (!isRoundActive || isSuspended)
        {
            return;
        }

        CancelAll("LOW_POPULATION_SUSPENDED");
        isSuspended = true;
        StopRefresh();
        ShowToEligible(renderer.RenderSuspended());
        LogInfo("O4_PANEL_STOPPED", $"RoundId={roundId};Reason={reason};Suspended=true");
    }

    public void CleanupRound(string reason = "RoundEnd")
    {
        bool hadState = isRoundActive || isSuspended || currentModel is not null || voteService.ActiveSession is not null;
        CancelAll(reason);
        StopRefresh();
        isRoundActive = false;
        isSuspended = false;
        isUnavailable = false;
        roundId = 0L;
        currentModel = null;
        activeRequest = null;
        activeCompletion = null;
        roundLocalO4Ids.Clear();
        nextRoundLocalO4Number = 1;
        if (hadState)
        {
            LogInfo("O4_PANEL_STOPPED", $"Reason={reason};RoundStateCleared=true");
        }
    }

    private void ScheduleRefresh(float delay)
    {
        if (!isRoundActive || isSuspended || hasRefreshHandle)
        {
            return;
        }

        hasRefreshHandle = true;
        refreshHandle = Timing.CallDelayed(delay, RefreshAndSchedule);
    }

    private void RefreshAndSchedule()
    {
        hasRefreshHandle = false;
        if (!isRoundActive || isSuspended)
        {
            return;
        }

        IReadOnlyList<O4PlayerSnapshot> currentO4 = GetCurrentO4Snapshots();
        if (isUnavailable || currentModel is null)
        {
            ShowToEligible(renderer.RenderUnavailable());
        }
        else if (voteService.ActiveSession is not null && activeRequest is not null)
        {
            O4VoteSession session = voteService.ActiveSession;
            string panel = renderer.RenderSelection(
                currentModel,
                activeRequest.Candidates,
                TimeRemainingSeconds,
                session.VoteCount,
                currentO4.Count);
            ShowToEligible(panel, currentO4);
        }
        else if (config.ShowNormalPanel)
        {
            ShowToEligible(renderer.RenderNormal(currentModel, DateTime.UtcNow), currentO4);
        }

        ScheduleRefresh((float)config.RefreshIntervalSeconds);
    }

    private void ScheduleSelectionTimeout()
    {
        KillSelectionTimeout();
        hasSelectionTimeoutHandle = true;
        selectionTimeoutHandle = Timing.CallDelayed(config.VoteDurationSeconds, ResolveSelection);
    }

    private void ResolveSelection()
    {
        hasSelectionTimeoutHandle = false;
        O4SelectionRequest? request = activeRequest;
        Action<O4SelectionResult>? completed = activeCompletion;
        if (request is null || completed is null)
        {
            return;
        }

        O4SelectionResult? result = voteService.ResolveActive(DateTime.UtcNow, GetCurrentO4Snapshots());
        activeRequest = null;
        activeCompletion = null;
        if (result is null)
        {
            return;
        }

        LogInfo(
            "O4_SELECTION_RESOLVED",
            $"RoundId={result.RoundId};CycleId={result.CycleId};SessionId={result.SessionId};VoteCountA={GetResultCount(result, 0)};VoteCountB={GetResultCount(result, 1)};EligibleVotes={result.EligibleVotes};Winner={result.SelectedEventId};Reason={result.Reason}");
        if (result.Outcome == O4SelectionOutcome.FALLBACK)
        {
            LogInfo("O4_SELECTION_FALLBACK", $"RoundId={result.RoundId};SessionId={result.SessionId};Reason={result.Reason}");
        }

        completed(result);
    }

    private IReadOnlyList<O4PlayerSnapshot> GetCurrentO4Snapshots()
    {
        List<O4PlayerSnapshot> snapshots = new List<O4PlayerSnapshot>();
        foreach (Player player in Player.Enumerable)
        {
            if (!eligibilityProvider.IsEligible(player))
            {
                continue;
            }

            string? localId = GetOrAssignLocalId(player.Id);
            if (localId is not null)
            {
                snapshots.Add(new O4PlayerSnapshot(localId, true, ResolveRole(player)));
            }
        }

        return snapshots;
    }

    private string? GetOrAssignLocalId(int playerId)
    {
        if (roundLocalO4Ids.TryGetValue(playerId, out string? existing))
        {
            return existing;
        }

        if (nextRoundLocalO4Number > MaxRoundLocalO4Ids)
        {
            return null;
        }

        string localId = $"O4-{nextRoundLocalO4Number:00}";
        nextRoundLocalO4Number++;
        roundLocalO4Ids[playerId] = localId;
        return localId;
    }

    private static O4RoleState ResolveRole(Player player)
    {
        return player.Role.Type == PlayerRoles.RoleTypeId.Overwatch || player.IsOverwatchEnabled
            ? O4RoleState.Overwatch
            : O4RoleState.Spectator;
    }

    private void ShowToEligible(string text)
    {
        ShowToEligible(text, GetCurrentO4Snapshots());
    }

    private void ShowToEligible(string text, IReadOnlyList<O4PlayerSnapshot> currentO4)
    {
        HashSet<string> eligibleIds = new HashSet<string>(currentO4.Select(player => player.RoundLocalO4Id), StringComparer.Ordinal);
        foreach (Player player in Player.Enumerable)
        {
            if (!eligibilityProvider.IsEligible(player)
                || !roundLocalO4Ids.TryGetValue(player.Id, out string? localId)
                || !eligibleIds.Contains(localId))
            {
                continue;
            }

            try
            {
                player.ShowHint(text, (float)config.HintDurationSeconds);
            }
            catch (Exception exception)
            {
                LogWarn("O4_PANEL_HINT_FAILED", $"RoundId={roundId};Reason={exception.GetType().Name}");
            }
        }
    }

    private void StopRefresh()
    {
        if (!hasRefreshHandle)
        {
            return;
        }

        try
        {
            Timing.KillCoroutines(refreshHandle);
        }
        catch (Exception exception)
        {
            LogWarn("O4_PANEL_REFRESH_CANCEL_FAILED", $"RoundId={roundId};Reason={exception.GetType().Name}");
        }

        hasRefreshHandle = false;
    }

    private void KillSelectionTimeout()
    {
        if (!hasSelectionTimeoutHandle)
        {
            return;
        }

        try
        {
            Timing.KillCoroutines(selectionTimeoutHandle);
        }
        catch (Exception exception)
        {
            LogWarn("O4_SELECTION_TIMEOUT_CANCEL_FAILED", $"RoundId={roundId};Reason={exception.GetType().Name}");
        }

        hasSelectionTimeoutHandle = false;
    }

    private static int GetResultCount(O4SelectionResult result, int index)
    {
        return result.CandidateVoteCounts.Count > index ? result.CandidateVoteCounts[index] : 0;
    }

    private static void LogInfo(string action, string message)
    {
        Log.Info($"[EmergencyEvents][O4][{DateTime.UtcNow:O}][{action}] {message}");
    }

    private static void LogWarn(string action, string message)
    {
        Log.Warn($"[EmergencyEvents][O4][{DateTime.UtcNow:O}][{action}] {message}");
    }
}
