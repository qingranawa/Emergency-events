using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis;
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
    private readonly Queue<MajorWaveCompletedEvent> queuedPostMajorWaveEvents = new Queue<MajorWaveCompletedEvent>();

    private EvaluationHistory evaluationHistory = new EvaluationHistory();
    private EvaluationOptions options = EvaluationOptions.Default;
    private RoundCoreState? roundCoreState;
    private ReinforcementManager? reinforcementManager;
    private DlrcEvaluationResult? lastResult;
    private RoundSnapshot? lastSnapshot;
    private CoroutineHandle scheduledHandle;
    private bool hasScheduledHandle;
    private bool isEvaluating;
    private bool queuedManualEvaluation;
    private bool isActive;
    private long roundId;
    private int warheadCancellationCount;
    private DateTime? warheadDetonatedAt;
    private long evaluationId;

    public DlrcEvaluatorService(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public DlrcEvaluationResult? LastResult => lastResult;

    public RoundSnapshot? LastSnapshot => lastSnapshot;

    public EvaluationHistory History => evaluationHistory;

    public bool IsActive => IsActiveRound();

    public bool IsEvaluating => isEvaluating;

    public bool HasScheduledEvaluation => hasScheduledHandle;

    public bool HasQueuedManualEvaluation => queuedManualEvaluation;

    public DlrcEvaluationTrigger? LastTrigger { get; private set; }

    /// <summary>
    /// 每次成功完成 D-LRC 评估后发布一次，供 Module 04 使用。
    /// </summary>
    public event Action<DlrcEvaluationCompletedEvent>? EvaluationCompleted;

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
        queuedPostMajorWaveEvents.Clear();
        warheadCancellationCount = 0;
        warheadDetonatedAt = null;
        evaluationId = 0L;
        lastResult = null;
        lastSnapshot = null;
        LastTrigger = null;
        isEvaluating = false;
        queuedManualEvaluation = false;
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

    /// <summary>
    /// 记录原版 Warhead.Detonated 事件时间，供后续快照消费客观事实。
    /// </summary>
    public void HandleWarheadDetonated(DateTime detonatedAt)
    {
        if (!IsActiveRound() || warheadDetonatedAt.HasValue)
        {
            return;
        }

        DateTime normalizedTimestamp = detonatedAt.Kind == DateTimeKind.Utc
            ? detonatedAt
            : detonatedAt.ToUniversalTime();
        if (normalizedTimestamp == default(DateTime))
        {
            LogWarn(roundId, "WarheadDetonatedFactUnavailable", "Reason=DefaultTimestamp");
            return;
        }

        warheadDetonatedAt = normalizedTimestamp;
        LogInfo(roundId, "WarheadDetonatedRecorded", $"DetonatedAt={normalizedTimestamp:O}; Source=Exiled.Warhead.Detonated");
    }

    /// <summary>
    /// 立即执行一次管理员请求的评估，不修改原有周期调度。
    /// </summary>
    public bool TryEvaluateImmediately(out DlrcEvaluationResult? result, out string response)
    {
        result = null;
        if (!IsActiveRound())
        {
            response = "当前没有正在进行、可评估的回合。";
            return false;
        }

        if (isEvaluating)
        {
            if (queuedManualEvaluation)
            {
                response = "D-LRC 当前正在评估，已有一个管理员补算请求排队。";
                return true;
            }

            queuedManualEvaluation = true;
            LogInfo(roundId, "MANUAL_RA_QUEUED", "RequestedBy=RA; Queued=true; PeriodicSchedule=Unchanged");
            response = "D-LRC 当前正在评估，已排队一次管理员补算。";
            return true;
        }

        isEvaluating = true;
        try
        {
            EvaluateOnce(DlrcEvaluationTrigger.MANUAL_RA);
            result = lastResult;
            if (result is null || !result.IsValid)
            {
                response = "D-LRC 未生成有效评估结果。";
                return false;
            }

            LogInfo(roundId, "MANUAL_RA_EVALUATED", $"RequestedBy=RA; Queued=false; Code={result.Code}; PeriodicSchedule=Unchanged");
            response = $"D-LRC 已立即评估：{result.Code}。原有周期计时未修改。";
            return true;
        }
        catch (Exception exception)
        {
            LogError(roundId, "MANUAL_EVALUATION_FAILED", exception);
            response = "D-LRC 立即评估失败，请查看服务端日志。";
            return false;
        }
        finally
        {
            isEvaluating = false;
            ProcessQueuedManualEvaluation(roundId);
        }
    }

    /// <summary>
    /// 主波实际生成后立即重算，原有 30 秒定时调度不重置。
    /// </summary>
    public void HandleMajorWaveCompleted(MajorWaveCompletedEvent ev)
    {
        if (!IsActiveRound() || ev is null || ev.RoundId != roundId)
        {
            return;
        }

        if (isEvaluating)
        {
            if (PostMajorWaveQueuePolicy.ShouldQueue(queuedPostMajorWaveEvents.Count))
            {
                queuedPostMajorWaveEvents.Enqueue(ev);
                LogInfo(roundId, "POST_MAJOR_WAVE_QUEUED", $"WaveId={ev.WaveId}; Reason=EvaluationInProgress; QueueLength=1; PeriodicSchedule=Unchanged");
            }
            else
            {
                LogDebug(roundId, "POST_MAJOR_WAVE_COALESCED", $"WaveId={ev.WaveId}; Reason=EvaluationInProgress; QueueLength=1; PeriodicSchedule=Unchanged");
            }

            return;
        }

        isEvaluating = true;
        try
        {
            EvaluateOnce(DlrcEvaluationTrigger.POST_MAJOR_WAVE);
            LogInfo(roundId, "POST_MAJOR_WAVE_EVALUATED", $"WaveId={ev.WaveId}; PeriodicSchedule=Unchanged");
        }
        catch (Exception exception)
        {
            LogError(roundId, "POST_MAJOR_WAVE_FAILED", exception);
        }
        finally
        {
            isEvaluating = false;
        }
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
            || warheadCancellationEventKeys.Count > 0
            || queuedPostMajorWaveEvents.Count > 0
            || warheadDetonatedAt.HasValue;
        long cleanupRoundId = roundId;
        bool handleCleanupSucceeded = StopScheduledEvaluation();

        isActive = false;
        isEvaluating = false;
        evaluationHistory.Clear();
        momentumTracker.Clear();
        warheadCancellationEventKeys.Clear();
        queuedPostMajorWaveEvents.Clear();
        queuedManualEvaluation = false;
        warheadCancellationCount = 0;
        warheadDetonatedAt = null;
        evaluationId = 0L;
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
                $"Reason={reason}; EvaluationHistoryCleared=true; MomentumCleared=true; SnapshotCleared=true; LastResultCleared=true; LastTriggerCleared=true; WarheadDedupCleared=true; WarheadDetonationFactCleared=true; PostMajorWaveQueueCleared=true; ScheduledHandleCleanup={handleCleanupSucceeded}; Cleanup={(handleCleanupSucceeded ? "SUCCESS" : "PARTIAL")}");
        }
    }

    public void SuspendRound(string reason)
    {
        if (!isActive && !hasScheduledHandle)
        {
            return;
        }

        bool scheduledHandleStopped = StopScheduledEvaluation();
        isActive = false;
        isEvaluating = false;
        queuedManualEvaluation = false;
        queuedPostMajorWaveEvents.Clear();
        LogInfo(
            roundId,
            "Suspended",
            $"Reason={reason}; ScheduledHandleStopped={scheduledHandleStopped}; LastResultRetained={lastResult is not null}; HistoryRetained={evaluationHistory.Count}");
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
            EvaluateOnce(DlrcEvaluationTrigger.PERIODIC);
        }
        catch (Exception exception)
        {
            LogError(currentRoundId, "EvaluationFailed", exception);
        }
        finally
        {
            isEvaluating = false;
            ScheduleNextEvaluation(currentRoundId);
            ProcessQueuedPostMajorWaveEvents(currentRoundId);
            ProcessQueuedManualEvaluation(currentRoundId);
        }
    }

    private void ProcessQueuedPostMajorWaveEvents(long currentRoundId)
    {
        while (IsActiveRound(currentRoundId) && queuedPostMajorWaveEvents.Count > 0)
        {
            MajorWaveCompletedEvent completedEvent = queuedPostMajorWaveEvents.Dequeue();
            isEvaluating = true;
            try
            {
                EvaluateOnce(DlrcEvaluationTrigger.POST_MAJOR_WAVE);
                LogInfo(roundId, "POST_MAJOR_WAVE_EVALUATED", $"WaveId={completedEvent.WaveId}; Source=Queued; PeriodicSchedule=Unchanged");
            }
            catch (Exception exception)
            {
                LogError(roundId, "POST_MAJOR_WAVE_FAILED", exception);
            }
            finally
            {
                isEvaluating = false;
            }
        }
    }

    private void ProcessQueuedManualEvaluation(long currentRoundId)
    {
        if (!IsActiveRound(currentRoundId) || !queuedManualEvaluation || isEvaluating)
        {
            return;
        }

        queuedManualEvaluation = false;
        isEvaluating = true;
        try
        {
            EvaluateOnce(DlrcEvaluationTrigger.MANUAL_RA);
            LogInfo(roundId, "MANUAL_RA_EVALUATED", "RequestedBy=RA; Queued=true; PeriodicSchedule=Unchanged");
        }
        catch (Exception exception)
        {
            LogError(roundId, "MANUAL_RA_EVALUATION_FAILED", exception);
        }
        finally
        {
            isEvaluating = false;
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

    private void EvaluateOnce(DlrcEvaluationTrigger trigger)
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
            elapsed,
            warheadDetonatedAt);
        DlrcEvaluationResult? previous = lastResult;
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(
            snapshot,
            evaluationHistory,
            options);

        lastSnapshot = snapshot;
        evaluationHistory.Add(result);
        lastResult = result;
        LastTrigger = trigger;
        LogDebug(roundId, "EvaluationDetail", EvaluationLogFormatter.FormatSnapshot(snapshot));
        LogDebug(roundId, "EvaluationDetail", EvaluationLogFormatter.FormatDetailed(result, roundId));
        if (previous is null
            || !string.Equals(previous.Code, result.Code, StringComparison.Ordinal)
            || previous.ControlState != result.ControlState)
        {
            LogInfo(roundId, "EvaluationChanged", EvaluationLogFormatter.FormatChange(previous, result));
        }

        PublishCompletedEvaluation(trigger, snapshot, result);
    }

    private void PublishCompletedEvaluation(
        DlrcEvaluationTrigger trigger,
        RoundSnapshot snapshot,
        DlrcEvaluationResult result)
    {
        if (!result.IsValid)
        {
            LogWarn(roundId, "CrisisEvaluationSkipped", "Reason=UpstreamEvaluationInvalid");
            return;
        }

        evaluationId++;
        try
        {
            EvaluationCompleted?.Invoke(new DlrcEvaluationCompletedEvent(
                evaluationId,
                trigger,
                snapshot,
                result));
        }
        catch (Exception exception)
        {
            LogError(roundId, "CrisisEvaluationPublishFailed", exception);
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
