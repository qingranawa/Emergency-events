using System;
using System.Collections.Generic;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;
using Exiled.API.Features;
using MEC;
using PlayerRoles;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// D-LRC 运行时服务，只读取游戏状态并发布评估日志。
/// </summary>
public sealed class DlrcEvaluatorService
{
    private readonly Config config;
    private readonly SnapshotCollector snapshotCollector = new SnapshotCollector();
    private readonly BattlefieldMomentumTracker momentumTracker = new BattlefieldMomentumTracker();
    private readonly HashSet<long> warheadCancellationEventKeys = new HashSet<long>();

    private EvaluationHistory evaluationHistory = new EvaluationHistory();
    private EvaluationOptions options = EvaluationOptions.Default;
    private RoundCoreState? roundCoreState;
    private ReinforcementManager? reinforcementManager;
    private DlrcEvaluationResult? lastResult;
    private RoundSnapshot? lastSnapshot;
    private CoroutineHandle scheduledHandle;
    private bool hasScheduledHandle;
    private bool isEvaluating;
    private bool isActive;
    private long roundId;
    private int warheadCancellationCount;

    public DlrcEvaluatorService(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public DlrcEvaluationResult? LastResult => lastResult;

    public RoundSnapshot? LastSnapshot => lastSnapshot;

    public EvaluationHistory History => evaluationHistory;

    public void StartRound(
        RoundCoreState? currentRoundCoreState,
        ReinforcementManager? currentReinforcementManager)
    {
        CleanupRound("Restart");
        if (!config.DlrcEvaluatorEnabled)
        {
            LogInfo(0L, "Disabled", "DlrcEvaluatorEnabled=false，跳过 D-LRC 调度。");
            return;
        }

        if (currentRoundCoreState is null)
        {
            LogWarn(0L, "StartSkipped", "没有已锁定的 Round Core 状态，跳过 D-LRC 调度。");
            return;
        }

        roundCoreState = currentRoundCoreState;
        reinforcementManager = currentReinforcementManager;
        roundId = currentRoundCoreState.RoundId;
        options = BuildOptions();
        evaluationHistory = new EvaluationHistory(options.HistoryCapacity);
        momentumTracker.Clear();
        warheadCancellationEventKeys.Clear();
        warheadCancellationCount = 0;
        lastResult = null;
        lastSnapshot = null;
        isEvaluating = false;
        isActive = true;

        double delaySeconds = EvaluationSchedule.GetInitialDelaySeconds(
            Round.ElapsedTime,
            options.EvaluationStartTimeSeconds);
        string activationMessage = EvaluationLogFormatter.FormatActivation(
            options.EvaluationStartTimeSeconds,
            options.EvaluationIntervalSeconds,
            delaySeconds);
        LogInfo(
            roundId,
            "Activated",
            $"{activationMessage}; LockedTier={currentRoundCoreState.Resolution.Tier}; LockedPopulation={currentRoundCoreState.StartPopulation}; StartingScpCount={currentRoundCoreState.Resolution.Composition?.ScpCount ?? 0}");
        ScheduleEvaluation(roundId, delaySeconds);
    }

    public void HandlePlayerDied(int playerId, RoleTypeId oldRole)
    {
        if (!IsActiveRound())
        {
            return;
        }

        if (!TryResolveDeathCategory(oldRole, out BattlefieldDeathCategory category))
        {
            return;
        }

        momentumTracker.RecordDeath(DateTime.UtcNow, category);
        LogDebug(roundId, "DeathRecorded", $"Player={playerId}; Category={category}; WindowSeconds={options.MomentumWindowSeconds}");
    }

    public void HandleWarheadStopping(bool isAllowed)
    {
        if (!IsActiveRound() || !isAllowed || Warhead.IsDetonated || !Warhead.IsInProgress)
        {
            return;
        }

        long eventKey = GetWarheadEventKey();
        if (!warheadCancellationEventKeys.Add(eventKey))
        {
            LogDebug(roundId, "WarheadCancellationDuplicate", $"EventKey={eventKey}; Count={warheadCancellationCount}");
            return;
        }

        warheadCancellationCount++;
        LogInfo(
            roundId,
            "WarheadCancellationRecorded",
            $"Count={warheadCancellationCount}; EventKey={eventKey}; ScorePerCancellation={options.WarheadCancelScore:0.####}; MaxScore={options.WarheadCancelMaxScore:0.####}");
    }

    public void ResetForWaitingForPlayers()
    {
        CleanupRound("WaitingForPlayers");
    }

    public void CleanupRound(string reason = "RoundEnded")
    {
        bool hadState = isActive
            || hasScheduledHandle
            || lastResult is not null
            || lastSnapshot is not null
            || evaluationHistory.Count > 0
            || warheadCancellationEventKeys.Count > 0;
        long cleanupRoundId = roundId;
        bool handleCleanupSucceeded = StopScheduledEvaluation();

        isActive = false;
        isEvaluating = false;
        evaluationHistory.Clear();
        momentumTracker.Clear();
        warheadCancellationEventKeys.Clear();
        warheadCancellationCount = 0;
        lastSnapshot = null;
        lastResult = null;
        roundCoreState = null;
        reinforcementManager = null;
        roundId = 0L;

        if (hadState)
        {
            LogInfo(
                cleanupRoundId,
                "Cleanup",
                $"Reason={reason}; EvaluationHistoryCleared=true; MomentumCleared=true; SnapshotCleared=true; LastResultCleared=true; WarheadDedupCleared=true; ScheduledHandleCleanup={handleCleanupSucceeded}; Cleanup={(handleCleanupSucceeded ? "SUCCESS" : "PARTIAL")}");
        }
    }

    private void ScheduleEvaluation(long currentRoundId, double delaySeconds)
    {
        if (!IsActiveRound(currentRoundId))
        {
            return;
        }

        float delay = (float)Math.Max(0.01d, delaySeconds);
        hasScheduledHandle = true;
        scheduledHandle = Timing.CallDelayed(
            delay,
            () => RunScheduledEvaluation(currentRoundId));
    }

    private void RunScheduledEvaluation(long currentRoundId)
    {
        if (!IsActiveRound(currentRoundId))
        {
            return;
        }

        hasScheduledHandle = false;

        if (!EvaluationSchedule.IsDue(Round.ElapsedTime, options.EvaluationStartTimeSeconds))
        {
            double remainingSeconds = EvaluationSchedule.GetInitialDelaySeconds(
                Round.ElapsedTime,
                options.EvaluationStartTimeSeconds);
            ScheduleEvaluation(currentRoundId, remainingSeconds);
            return;
        }

        if (isEvaluating)
        {
            LogWarn(currentRoundId, "EvaluationSkipped", "Reason=PreviousEvaluationStillRunning");
            ScheduleNextEvaluation(currentRoundId);
            return;
        }

        isEvaluating = true;
        try
        {
            EvaluateOnce();
        }
        catch (Exception exception)
        {
            LogError(currentRoundId, "EvaluationFailed", exception);
        }
        finally
        {
            isEvaluating = false;
            ScheduleNextEvaluation(currentRoundId);
        }
    }

    private void ScheduleNextEvaluation(long currentRoundId)
    {
        if (IsActiveRound(currentRoundId))
        {
            double delaySeconds = EvaluationSchedule.GetNextDelaySeconds(
                Round.ElapsedTime,
                options.EvaluationStartTimeSeconds,
                options.EvaluationIntervalSeconds);
            ScheduleEvaluation(currentRoundId, delaySeconds);
        }
    }

    private void EvaluateOnce()
    {
        DateTime timestamp = DateTime.UtcNow;
        TimeSpan elapsed = Round.ElapsedTime;
        BattlefieldMomentumSnapshot momentum = momentumTracker.GetSnapshot(
            timestamp,
            options.MomentumWindowSeconds);
        RoundSnapshot snapshot = snapshotCollector.Collect(
            roundCoreState,
            reinforcementManager,
            momentum,
            warheadCancellationCount,
            timestamp,
            elapsed);
        DlrcEvaluationResult? previous = lastResult;
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(
            snapshot,
            evaluationHistory,
            options);

        lastSnapshot = snapshot;
        evaluationHistory.Add(result);
        lastResult = result;
        LogDebug(roundId, "EvaluationDetail", EvaluationLogFormatter.FormatSnapshot(snapshot));
        LogDebug(roundId, "EvaluationDetail", EvaluationLogFormatter.FormatDetailed(result, roundId));
        if (previous is null
            || !string.Equals(previous.Code, result.Code, StringComparison.Ordinal)
            || previous.ControlState != result.ControlState)
        {
            LogInfo(roundId, "EvaluationChanged", EvaluationLogFormatter.FormatChange(previous, result));
        }
    }

    private EvaluationOptions BuildOptions()
    {
        return new EvaluationOptions(
            config.DlrcZombieFullPressureCount,
            config.DlrcThreatTrendWindowSeconds,
            config.DlrcMomentumWindowSeconds,
            config.DlrcWarheadCancelScore,
            config.DlrcWarheadCancelMaxScore,
            config.DlrcEvaluatorStartTimeSeconds,
            config.DlrcEvaluatorIntervalSeconds,
            config.DlrcEvaluationHistoryCapacity,
            config.DlrcResponseThresholdsE,
            config.DlrcResponseThresholdsD,
            config.DlrcResponseThresholdsC,
            config.DlrcResponseThresholdsB,
            config.DlrcResponseThresholdsA);
    }

    private bool StopScheduledEvaluation()
    {
        if (!hasScheduledHandle)
        {
            return true;
        }

        try
        {
            Timing.KillCoroutines(scheduledHandle);
            hasScheduledHandle = false;
            return true;
        }
        catch (Exception exception)
        {
            LogError(roundId, "CleanupHandleFailed", exception);
            hasScheduledHandle = false;
            return false;
        }
    }

    private bool IsActiveRound()
    {
        return isActive && roundId > 0L;
    }

    private bool IsActiveRound(long currentRoundId)
    {
        return IsActiveRound() && roundId == currentRoundId;
    }

    private long GetWarheadEventKey()
    {
        return (long)Math.Round(Round.ElapsedTime.TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    private static bool TryResolveDeathCategory(
        RoleTypeId role,
        out BattlefieldDeathCategory category)
    {
        if (role == RoleTypeId.FacilityGuard
            || role == RoleTypeId.NtfPrivate
            || role == RoleTypeId.NtfSergeant
            || role == RoleTypeId.NtfCaptain
            || role == RoleTypeId.NtfSpecialist)
        {
            category = BattlefieldDeathCategory.Foundation;
            return true;
        }

        if (role == RoleTypeId.ChaosConscript
            || role == RoleTypeId.ChaosRifleman
            || role == RoleTypeId.ChaosMarauder
            || role == RoleTypeId.ChaosRepressor)
        {
            category = BattlefieldDeathCategory.HostileHuman;
            return true;
        }

        string roleName = role.ToString();
        if (roleName.StartsWith("Scp", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(roleName, nameof(RoleTypeId.Scp0492), StringComparison.OrdinalIgnoreCase))
        {
            category = BattlefieldDeathCategory.MainScp;
            return true;
        }

        category = default(BattlefieldDeathCategory);
        return false;
    }

    private static void LogInfo(long currentRoundId, string action, string message)
    {
        Log.Info($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }

    private static void LogWarn(long currentRoundId, string action, string message)
    {
        Log.Warn($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }

    private static void LogError(long currentRoundId, string action, Exception exception)
    {
        Log.Error($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {exception}");
    }

    private void LogDebug(long currentRoundId, string action, string message)
    {
        if (!config.Debug)
        {
            return;
        }

        Log.Debug($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }
}
