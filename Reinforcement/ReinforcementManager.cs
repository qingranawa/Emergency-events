using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RoundCore;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Waves;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using Respawning;
using Respawning.Waves;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 保留原版 Primary Wave，仅取消 Mini-Wave、截断人数并记录事实历史。
/// </summary>
public sealed class ReinforcementManager
{
    private const float SurvivalObservationDelaySeconds = 120f;
    private const float TimerExtensionRetryDelaySeconds = 0.1f;
    private const int MaxTimerExtensionRetryCount = 3;

    private readonly Config config;
    private readonly List<TimerExtensionWorkItem> pendingNativeWaveCompletions = new List<TimerExtensionWorkItem>();
    private ReinforcementState? state;
    private int spawningFactionTimerExtensionSeconds;
    private int opposingFactionTimerExtensionSeconds;

    private sealed class TimerExtensionWorkItem
    {
        public TimerExtensionWorkItem(
            long roundId,
            MajorWaveRecord record,
            TimedWave completedWave,
            double? foundationTimerBeforeWave,
            double? chaosTimerBeforeWave)
        {
            RoundId = roundId;
            Record = record;
            CompletedWave = completedWave;
            FoundationTimerBeforeWave = foundationTimerBeforeWave;
            ChaosTimerBeforeWave = chaosTimerBeforeWave;
        }

        public long RoundId { get; }

        public MajorWaveRecord Record { get; }

        public TimedWave CompletedWave { get; }

        public double? FoundationTimerBeforeWave { get; }

        public double? ChaosTimerBeforeWave { get; }

        public bool VanillaResetConfirmed { get; set; }

        public bool FoundationExtensionApplied { get; set; }

        public bool ChaosExtensionApplied { get; set; }

        public int RetryCount { get; set; }

        public double? FoundationTimerAfterVanillaReset { get; set; }

        public double? ChaosTimerAfterVanillaReset { get; set; }

        public double? FoundationTimePassedAfterVanillaReset { get; set; }

        public double? ChaosTimePassedAfterVanillaReset { get; set; }
    }

    public ReinforcementManager(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        WaveManager.OnWaveSpawned += HandleNativeWaveSpawned;
    }

    public event Action<MajorWaveCompletedEvent>? MajorWaveCompleted;

    public ReinforcementState? State => state;

    public bool IsRoundActive => IsActive();

    public IReadOnlyList<MajorWaveSnapshot> GetMajorWaveHistorySnapshot()
    {
        return state is null || !state.IsActive
            ? Array.Empty<MajorWaveSnapshot>()
            : state.MajorWaveHistory.GetSnapshots();
    }

    public IReadOnlyList<MajorWaveRecord> GetMajorWaveRecords()
    {
        return state?.MajorWaveHistory.Records ?? Array.Empty<MajorWaveRecord>();
    }

    public bool TryGetPrimaryTimerSeconds(out double? foundationSeconds, out double? chaosSeconds)
    {
        foundationSeconds = null;
        chaosSeconds = null;
        if (!TryGetPrimaryTimers(out TimedWave? foundationTimer, out TimedWave? chaosTimer))
        {
            return false;
        }

        foundationSeconds = foundationTimer!.Timer.TimeLeft.TotalSeconds;
        chaosSeconds = chaosTimer!.Timer.TimeLeft.TotalSeconds;
        return true;
    }

    public void ResetForWaitingForPlayers()
    {
        CleanupRound();
    }

    public void Dispose()
    {
        WaveManager.OnWaveSpawned -= HandleNativeWaveSpawned;
        CleanupRound();
    }

    public void StartRound(long roundId, PopulationTier lockedPopulationTier)
    {
        CleanupRound();
        if (!config.ReinforcementEnabled)
        {
            LogInfo(roundId, "Disabled", "ReinforcementEnabled=false; 原版支援不受插件影响。");
            return;
        }

        spawningFactionTimerExtensionSeconds = PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(
            config.SpawningFactionTimerExtensionSeconds,
            PrimaryWaveTimerExtensionPolicy.DefaultSpawningFactionSeconds);
        opposingFactionTimerExtensionSeconds = PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(
            config.OpposingFactionTimerExtensionSeconds,
            PrimaryWaveTimerExtensionPolicy.DefaultOpposingFactionSeconds);
        if (config.SpawningFactionTimerExtensionSeconds != spawningFactionTimerExtensionSeconds)
        {
            LogWarn(
                roundId,
                "TimerExtensionConfig",
                $"Setting=SpawningFactionTimerExtensionSeconds; Configured={config.SpawningFactionTimerExtensionSeconds}; Applied={spawningFactionTimerExtensionSeconds}; Reason=InvalidConfiguration; Fallback={PrimaryWaveTimerExtensionPolicy.DefaultSpawningFactionSeconds}");
        }

        if (config.OpposingFactionTimerExtensionSeconds != opposingFactionTimerExtensionSeconds)
        {
            LogWarn(
                roundId,
                "TimerExtensionConfig",
                $"Setting=OpposingFactionTimerExtensionSeconds; Configured={config.OpposingFactionTimerExtensionSeconds}; Applied={opposingFactionTimerExtensionSeconds}; Reason=InvalidConfiguration; Fallback={PrimaryWaveTimerExtensionPolicy.DefaultOpposingFactionSeconds}");
        }

        state = new ReinforcementState(roundId, lockedPopulationTier);
        LogInfo(
            roundId,
            "RoundStarted",
            $"LockedPopulationTier={lockedPopulationTier}; PrimaryWave=VanillaPreserved; MiniWaveDisabled={config.DisableMiniWaves}; PrimaryWaveCap={GetCaps().GetCap(lockedPopulationTier)}; SpawningFactionTimerExtensionSeconds={spawningFactionTimerExtensionSeconds}; OpposingFactionTimerExtensionSeconds={opposingFactionTimerExtensionSeconds}; NativeTimers=Unchanged; NativeFactionAndTokens=Unchanged");
    }

    public void HandleSelectingRespawnTeam(SelectingRespawnTeamEventArgs ev)
    {
        // 选择阶段必须保留原版流程，否则原版会在同一个计时点反复重试选择事件。
    }

    public void HandleRespawningTeam(RespawningTeamEventArgs ev)
    {
        if (!IsActive())
        {
            return;
        }

        TimedWave wave = ev.Wave;
        if (PrimaryWavePolicy.ShouldCancelMiniWaveAtBoundary(
                wave.IsMiniWave,
                config.DisableMiniWaves,
                MiniWaveCancellationBoundary.RespawningTeam))
        {
            ev.IsAllowed = false;
            state!.ClearPendingPrimaryWave();
            LogInfo(
                state.RoundId,
                "MiniWave",
                $"Requested=true; Action=Cancelled; Reason=DisabledByEmergencyEvents; Wave={wave.Name}; Faction={wave.SpawnableFaction}; Boundary=RespawningTeam");
            return;
        }

        if (!PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction(wave.SpawnableFaction.ToString()))
        {
            state!.ClearPendingPrimaryWave();
            LogDetailed(
                state!.RoundId,
                "PrimaryWaveIgnored",
                $"Wave={wave.Name}; Faction={wave.SpawnableFaction}; Reason=NonPrimaryFaction");
            return;
        }

        int originalMaximum = ev.MaximumRespawnAmount;
        int cappedMaximum = PrimaryWavePolicy.GetCappedMaximumRespawnAmount(
            originalMaximum,
            state!.LockedPopulationTier,
            GetCaps());
        if (cappedMaximum < originalMaximum)
        {
            ev.MaximumRespawnAmount = cappedMaximum;
        }

        if (!ev.IsAllowed)
        {
            state.ClearPendingPrimaryWave();
            LogDetailed(
                state.RoundId,
                "PrimaryWaveIgnored",
                $"Wave={wave.Name}; Faction={wave.SpawnableFaction}; Reason=DeniedByEarlierHandler");
            return;
        }

        state.HasPendingPrimaryWave = true;
        state.PendingWaveName = wave.Name;
        state.PendingFaction = wave.SpawnableFaction.ToString();
        state.PendingStartedAt = DateTime.UtcNow;
        CapturePendingTimerSnapshot(state);
        LogDetailed(
            state.RoundId,
            "PrimaryWavePrepared",
            $"Wave={wave.Name}; Faction={wave.SpawnableFaction}; LockedPopulationTier={state.LockedPopulationTier}; VanillaMaximum={originalMaximum}; AppliedMaximum={cappedMaximum}; VanillaSelectedPlayersAfter={ev.Players.Count}; Selection=VanillaPreserved; Allowed={ev.IsAllowed}");
    }

    public void HandleRespawnedTeam(RespawnedTeamEventArgs ev)
    {
        if (!IsActive() || !state!.HasPendingPrimaryWave)
        {
            return;
        }

        string pendingWaveName = state.PendingWaveName;
        string pendingFaction = state.PendingFaction;
        DateTime pendingStartedAt = state.PendingStartedAt;
        double? foundationTimerBeforeWave = state.PendingFoundationTimerBeforeWave;
        double? chaosTimerBeforeWave = state.PendingChaosTimerBeforeWave;
        state.ClearPendingPrimaryWave();
        TimedWave? wave = FindTimedWave(ev.Wave);
        if (wave is not null && PrimaryWavePolicy.ShouldCancelMiniWave(wave.IsMiniWave, config.DisableMiniWaves))
        {
            LogWarn(
                state.RoundId,
                "MiniWave",
                $"Requested=true; Action=UnexpectedRespawnObserved; Reason=CancellationBypassed; Wave={wave.Name}; Faction={wave.SpawnableFaction}");
            return;
        }

        string observedFaction = wave?.SpawnableFaction.ToString() ?? pendingFaction;
        if (!PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction(observedFaction))
        {
            LogDetailed(
                state.RoundId,
                "PrimaryWaveIgnored",
                $"Wave={(wave?.Name ?? pendingWaveName)}; Faction={observedFaction}; Reason=NonPrimaryFaction");
            return;
        }

        List<Player> candidatePlayers = ev.Players.ToList();
        List<Player> spawnedPlayers = GetActuallySpawnedPlayers(candidatePlayers, wave);
        if (spawnedPlayers.Count == 0)
        {
            LogInfo(
                state.RoundId,
                "PrimaryWaveEmpty",
                $"Wave={(wave?.Name ?? pendingWaveName)}; Faction={(wave?.SpawnableFaction.ToString() ?? pendingFaction)}; CandidateCount={candidatePlayers.Count}; ActualSpawnedCount=0; Action=NotRecorded; Reason=NoSuccessfulRoleAssignment");
            LogTimerExtensionSkipped(
                state.RoundId,
                "Unavailable",
                wave?.SpawnableFaction.ToString() ?? pendingFaction,
                0,
                "ZeroSpawn");
            return;
        }

        string waveName = wave?.Name ?? pendingWaveName;
        string faction = observedFaction;
        DateTime completedAt = DateTime.UtcNow;
        string waveId = $"{state.RoundId}-MW-{++state.NextWaveSequence:000}";
        MajorWaveRecord record = state.MajorWaveHistory.Record(
            waveId,
            faction,
            state.LockedPopulationTier,
            spawnedPlayers.Count,
            spawnedPlayers.Select(player => player.Id),
            pendingStartedAt,
            completedAt);
        LogInfo(
            state.RoundId,
            "PrimaryWaveCompleted",
            $"WaveId={record.WaveId}; Wave={waveName}; Faction={record.Faction}; LockedPopulationTier={record.PopulationTier}; CandidateCount={candidatePlayers.Count}; StartedAt={record.StartedAt:O}; ActualSpawnedCount={record.ActualSpawnedCount}; CompletedAt={record.CompletedAt:O}; MemberCount={record.MemberIds.Count}");

        if (wave is null)
        {
            LogTimerExtensionSkipped(
                state.RoundId,
                record.WaveId,
                record.Faction,
                record.ActualSpawnedCount,
                "NativeWaveReferenceUnavailable");
            PublishMajorWaveCompleted(record);
            ScheduleSurvivalObservation(state.RoundId, record);
            return;
        }

        QueueTimerExtensionUntilNativeWaveCompletion(
            state.RoundId,
            record,
            wave,
            foundationTimerBeforeWave,
            chaosTimerBeforeWave);
        ScheduleSurvivalObservation(state.RoundId, record);
    }

    public void CleanupRound()
    {
        pendingNativeWaveCompletions.Clear();
        if (state is null)
        {
            return;
        }

        long roundId = state.RoundId;
        state.IsActive = false;
        int historyCount = state.MajorWaveHistory.Count;
        int scheduledHandleCount = state.ScheduledHandles.Count;
        foreach (CoroutineHandle handle in state.ScheduledHandles)
        {
            Timing.KillCoroutines(handle);
        }

        state.ScheduledHandles.Clear();
        state.MajorWaveHistory.Clear();
        state.ClearPendingPrimaryWave();
        LogInfo(
            roundId,
            "Cleanup",
            $"MajorWaveHistoryCleared={historyCount}; ScheduledHandlesCleared={scheduledHandleCount}; PendingPrimaryWaveCleared=true");
        state = null;
    }

    public void SuspendRound(string reason)
    {
        pendingNativeWaveCompletions.Clear();
        if (state is null || !state.IsActive)
        {
            return;
        }

        int scheduledHandleCount = state.ScheduledHandles.Count;
        foreach (CoroutineHandle handle in state.ScheduledHandles)
        {
            Timing.KillCoroutines(handle);
        }

        state.ScheduledHandles.Clear();
        state.ClearPendingPrimaryWave();
        state.IsActive = false;
        LogInfo(
            state.RoundId,
            "Suspended",
            $"Reason={reason}; MajorWaveHistoryRetained={state.MajorWaveHistory.Count}; ScheduledHandlesStopped={scheduledHandleCount}; PendingPrimaryWaveCleared=true");
    }

    private void PublishMajorWaveCompleted(MajorWaveRecord record)
    {
        if (state is null || !state.MajorWaveHistory.TryMarkPostMajorWavePublished(record))
        {
            return;
        }

        MajorWaveCompletedEvent completedEvent = new MajorWaveCompletedEvent(state.RoundId, record);
        LogInfo(
            state.RoundId,
            "POST_MAJOR_WAVE",
            $"WaveId={completedEvent.WaveId}; Faction={completedEvent.Faction}; PopulationTier={completedEvent.PopulationTier}; ActualSpawnedCount={completedEvent.ActualSpawnedCount}; CompletedAt={completedEvent.CompletedAt:O}");
        try
        {
            MajorWaveCompleted?.Invoke(completedEvent);
        }
        catch (Exception exception)
        {
            LogError(state.RoundId, "POST_MAJOR_WAVE_FAILED", exception);
        }
    }

    private void QueueTimerExtensionUntilNativeWaveCompletion(
        long roundId,
        MajorWaveRecord record,
        TimedWave completedWave,
        double? foundationTimerBeforeWave,
        double? chaosTimerBeforeWave)
    {
        if (!IsActiveRound(roundId))
        {
            return;
        }

        pendingNativeWaveCompletions.Add(
            new TimerExtensionWorkItem(
                roundId,
                record,
                completedWave,
                foundationTimerBeforeWave,
                chaosTimerBeforeWave));
    }

    private void HandleNativeWaveSpawned(SpawnableWaveBase nativeWave, List<ReferenceHub> _)
    {
        if (!IsActive() || pendingNativeWaveCompletions.Count == 0)
        {
            return;
        }

        int workIndex = pendingNativeWaveCompletions.FindIndex(
            workItem => ReferenceEquals(workItem.CompletedWave.Base, nativeWave));
        if (workIndex < 0)
        {
            return;
        }

        TimerExtensionWorkItem workItem = pendingNativeWaveCompletions[workIndex];
        string waveFaction = workItem.CompletedWave.SpawnableFaction.ToString();
        bool shouldConfirmVanillaReset = ShouldConfirmVanillaReset(
            waveFaction,
            workItem.CompletedWave.IsMiniWave,
            workItem.Record.ActualSpawnedCount);
        if (shouldConfirmVanillaReset
            && !workItem.VanillaResetConfirmed
            && (!TryGetPrimaryTimers(out TimedWave? foundationTimer, out TimedWave? chaosTimer)
                || !IsVanillaResetDetected(waveFaction, foundationTimer!, chaosTimer!)))
        {
            pendingNativeWaveCompletions.RemoveAt(workIndex);
            LogTimerExtensionSkipped(
                workItem.RoundId,
                workItem.Record.WaveId,
                waveFaction,
                workItem.Record.ActualSpawnedCount,
                "VanillaResetNotDetected");
            PublishMajorWaveCompleted(workItem.Record);
            return;
        }

        workItem.VanillaResetConfirmed |= shouldConfirmVanillaReset;
        ProcessTimerExtensionWorkItem(workItem);
    }

    private void ProcessTimerExtensionWorkItem(TimerExtensionWorkItem workItem)
    {
        if (!IsActiveRound(workItem.RoundId)
            || !pendingNativeWaveCompletions.Contains(workItem))
        {
            return;
        }

        try
        {
            if (!ApplyPrimaryWaveTimerExtension(workItem))
            {
                return;
            }

            pendingNativeWaveCompletions.Remove(workItem);
            PublishMajorWaveCompleted(workItem.Record);
        }
        catch (Exception exception)
        {
            LogError(workItem.RoundId, "TIMER_EXTENSION_FAILED", exception);
            if (workItem.RetryCount < MaxTimerExtensionRetryCount)
            {
                ScheduleTimerExtensionRetry(workItem);
                return;
            }

            pendingNativeWaveCompletions.Remove(workItem);
            workItem.Record.TryMarkTimerExtensionProcessed();
            LogTimerExtensionSkipped(
                workItem.RoundId,
                workItem.Record.WaveId,
                workItem.CompletedWave.SpawnableFaction.ToString(),
                workItem.Record.ActualSpawnedCount,
                "ExceptionAfterRetries",
                workItem.VanillaResetConfirmed);
            PublishMajorWaveCompleted(workItem.Record);
        }
    }

    private void ScheduleTimerExtensionRetry(TimerExtensionWorkItem workItem)
    {
        workItem.RetryCount++;
        LogWarn(
            workItem.RoundId,
            "TimerExtensionRetry",
            $"WaveId={workItem.Record.WaveId}; Retry={workItem.RetryCount}; MaxRetries={MaxTimerExtensionRetryCount}; DelaySeconds={TimerExtensionRetryDelaySeconds.ToString("0.###", CultureInfo.InvariantCulture)}");

        try
        {
            CoroutineHandle handle = default(CoroutineHandle);
            handle = Timing.CallDelayed(
                TimerExtensionRetryDelaySeconds,
                () =>
                {
                    try
                    {
                        ProcessTimerExtensionWorkItem(workItem);
                    }
                    finally
                    {
                        RemoveScheduledHandle(workItem.RoundId, handle);
                    }
                });
            state!.ScheduledHandles.Add(handle);
        }
        catch (Exception exception)
        {
            LogError(workItem.RoundId, "TIMER_EXTENSION_RETRY_SCHEDULE_FAILED", exception);
            pendingNativeWaveCompletions.Remove(workItem);
            workItem.Record.TryMarkTimerExtensionProcessed();
            LogTimerExtensionSkipped(
                workItem.RoundId,
                workItem.Record.WaveId,
                workItem.CompletedWave.SpawnableFaction.ToString(),
                workItem.Record.ActualSpawnedCount,
                "RetrySchedulingFailed",
                workItem.VanillaResetConfirmed);
            PublishMajorWaveCompleted(workItem.Record);
        }
    }

    private bool ApplyPrimaryWaveTimerExtension(TimerExtensionWorkItem workItem)
    {
        string waveFaction = workItem.CompletedWave.SpawnableFaction.ToString();
        bool isMiniWave = workItem.CompletedWave.IsMiniWave;
        MajorWaveRecord record = workItem.Record;
        bool alreadyProcessed = record.IsTimerExtensionProcessed;
        if (!PrimaryWaveTimerExtensionPolicy.ShouldApply(
                waveFaction,
                isMiniWave,
                record.ActualSpawnedCount,
                true,
                spawningFactionTimerExtensionSeconds,
                opposingFactionTimerExtensionSeconds,
                alreadyProcessed))
        {
            string reason = GetTimerExtensionSkipReason(
                waveFaction,
                isMiniWave,
                record.ActualSpawnedCount,
                alreadyProcessed);
            LogTimerExtensionSkipped(
                workItem.RoundId,
                record.WaveId,
                waveFaction,
                record.ActualSpawnedCount,
                reason);
            record.TryMarkTimerExtensionProcessed();
            return true;
        }

        if (!PrimaryWaveTimerExtensionPolicy.TryGetExtensions(
                waveFaction,
                spawningFactionTimerExtensionSeconds,
                opposingFactionTimerExtensionSeconds,
                out int foundationExtension,
                out int chaosExtension)
            || !TryGetPrimaryTimers(out TimedWave? foundationTimer, out TimedWave? chaosTimer))
        {
            throw new InvalidOperationException("Primary wave timers are unavailable after the native wave event.");
        }

        TimedWave foundationTimerValue = foundationTimer
            ?? throw new InvalidOperationException("Foundation primary wave timer is unavailable.");
        TimedWave chaosTimerValue = chaosTimer
            ?? throw new InvalidOperationException("Chaos primary wave timer is unavailable.");

        if (record.IsTimerExtensionProcessed)
        {
            LogTimerExtensionSkipped(
                workItem.RoundId,
                record.WaveId,
                waveFaction,
                record.ActualSpawnedCount,
                "Duplicate");
            return true;
        }

        if (!workItem.FoundationTimerAfterVanillaReset.HasValue)
        {
            workItem.FoundationTimerAfterVanillaReset = foundationTimerValue.Timer.TimeLeft.TotalSeconds;
            workItem.ChaosTimerAfterVanillaReset = chaosTimerValue.Timer.TimeLeft.TotalSeconds;
            workItem.FoundationTimePassedAfterVanillaReset = foundationTimerValue.Timer.Base.TimePassed;
            workItem.ChaosTimePassedAfterVanillaReset = chaosTimerValue.Timer.Base.TimePassed;
        }

        double foundationBeforeExtension = foundationTimerValue.Timer.TimeLeft.TotalSeconds;
        double chaosBeforeExtension = chaosTimerValue.Timer.TimeLeft.TotalSeconds;
        if (foundationExtension <= 0)
        {
            workItem.FoundationExtensionApplied = true;
        }

        if (chaosExtension <= 0)
        {
            workItem.ChaosExtensionApplied = true;
        }

        if (foundationExtension > 0 && !workItem.FoundationExtensionApplied)
        {
            AddTimerExtension(foundationTimerValue, foundationExtension);
            workItem.FoundationExtensionApplied = true;
            TrySendTimerUpdate(workItem.RoundId, foundationTimerValue);
        }

        if (chaosExtension > 0 && !workItem.ChaosExtensionApplied)
        {
            AddTimerExtension(chaosTimerValue, chaosExtension);
            workItem.ChaosExtensionApplied = true;
            TrySendTimerUpdate(workItem.RoundId, chaosTimerValue);
        }

        if (!record.TryMarkTimerExtensionProcessed())
        {
            LogTimerExtensionSkipped(
                workItem.RoundId,
                record.WaveId,
                waveFaction,
                record.ActualSpawnedCount,
                "Duplicate");
            return true;
        }

        DateTime appliedAt = DateTime.UtcNow;
        double foundationAfterExtension = foundationTimerValue.Timer.TimeLeft.TotalSeconds;
        double chaosAfterExtension = chaosTimerValue.Timer.TimeLeft.TotalSeconds;
        double delayAfterWaveCompletionMs = Math.Max(0, (appliedAt - record.CompletedAt).TotalMilliseconds);
        LogInfo(
            workItem.RoundId,
            "TimerExtension",
            $"WaveId={record.WaveId}; WaveFaction={waveFaction}; ActualSpawnedCount={record.ActualSpawnedCount}; VanillaResetDetected={workItem.VanillaResetConfirmed}; VanillaResetDetection=WaveManager.OnWaveSpawnedAfterTimeBasedWave; FoundationTimerBeforeWave={FormatOptionalTimerSeconds(workItem.FoundationTimerBeforeWave)}; ChaosTimerBeforeWave={FormatOptionalTimerSeconds(workItem.ChaosTimerBeforeWave)}; FoundationTimerAfterVanillaReset={FormatOptionalTimerSeconds(workItem.FoundationTimerAfterVanillaReset)}; ChaosTimerAfterVanillaReset={FormatOptionalTimerSeconds(workItem.ChaosTimerAfterVanillaReset)}; FoundationTimerBeforeExtension={FormatTimerSeconds(foundationBeforeExtension)}; ChaosTimerBeforeExtension={FormatTimerSeconds(chaosBeforeExtension)}; FoundationTimePassedAfterVanillaReset={FormatOptionalTimerSeconds(workItem.FoundationTimePassedAfterVanillaReset)}; ChaosTimePassedAfterVanillaReset={FormatOptionalTimerSeconds(workItem.ChaosTimePassedAfterVanillaReset)}; FoundationExtension={foundationExtension}; ChaosExtension={chaosExtension}; FoundationTimerAfterExtension={FormatTimerSeconds(foundationAfterExtension)}; ChaosTimerAfterExtension={FormatTimerSeconds(chaosAfterExtension)}; AppliedAt={appliedAt:O}; WaveCompletedAt={record.CompletedAt:O}; DelayAfterWaveCompletionMs={delayAfterWaveCompletionMs.ToString("0.###", CultureInfo.InvariantCulture)}; RetryCount={workItem.RetryCount}; Applied=true; Reason=PrimaryWaveCompleted");
        return true;
    }

    private static bool TryGetPrimaryTimers(out TimedWave? foundationTimer, out TimedWave? chaosTimer)
    {
        foundationTimer = null;
        chaosTimer = null;
        foreach (TimedWave timedWave in TimedWave.GetTimedWaves())
        {
            if (timedWave.IsMiniWave)
            {
                continue;
            }

            if (timedWave.SpawnableFaction == SpawnableFaction.NtfWave)
            {
                foundationTimer = timedWave;
            }
            else if (timedWave.SpawnableFaction == SpawnableFaction.ChaosWave)
            {
                chaosTimer = timedWave;
            }
        }

        return foundationTimer is not null && chaosTimer is not null;
    }

    private static void CapturePendingTimerSnapshot(ReinforcementState reinforcementState)
    {
        if (!TryGetPrimaryTimers(out TimedWave? foundationTimer, out TimedWave? chaosTimer))
        {
            reinforcementState.PendingFoundationTimerBeforeWave = null;
            reinforcementState.PendingChaosTimerBeforeWave = null;
            return;
        }

        reinforcementState.PendingFoundationTimerBeforeWave = foundationTimer!.Timer.TimeLeft.TotalSeconds;
        reinforcementState.PendingChaosTimerBeforeWave = chaosTimer!.Timer.TimeLeft.TotalSeconds;
    }

    private bool ShouldConfirmVanillaReset(string waveFaction, bool isMiniWave, int actualSpawnedCount)
    {
        return PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction(waveFaction)
            && !isMiniWave
            && actualSpawnedCount > 0
            && (spawningFactionTimerExtensionSeconds > 0 || opposingFactionTimerExtensionSeconds > 0);
    }

    private static bool IsVanillaResetDetected(
        string waveFaction,
        TimedWave foundationTimer,
        TimedWave chaosTimer)
    {
        TimedWave spawningTimer = string.Equals(waveFaction, "NtfWave", StringComparison.Ordinal)
            ? foundationTimer
            : chaosTimer;
        return PrimaryWaveTimerExtensionPolicy.IsVanillaResetDetected(
            spawningTimer.Timer.Base.TimePassed);
    }

    private static void AddTimerExtension(TimedWave timedWave, int extensionSeconds)
    {
        Respawning.Waves.WaveTimer timer = timedWave.Timer.Base;
        timer.SpawnIntervalSeconds += extensionSeconds;
    }

    private void TrySendTimerUpdate(long roundId, TimedWave timedWave)
    {
        try
        {
            WaveUpdateMessage.ServerSendUpdate(timedWave.Base, UpdateMessageFlags.Timer);
        }
        catch (Exception exception)
        {
            LogWarn(
                roundId,
                "TimerExtensionUpdateFailed",
                $"Faction={timedWave.SpawnableFaction}; Reason=TimerValueAppliedButClientUpdateFailed; Exception={exception.GetType().Name}");
        }
    }

    private string GetTimerExtensionSkipReason(
        string waveFaction,
        bool isMiniWave,
        int actualSpawnedCount,
        bool alreadyProcessed)
    {
        if (alreadyProcessed)
        {
            return "Duplicate";
        }

        if (spawningFactionTimerExtensionSeconds == 0 && opposingFactionTimerExtensionSeconds == 0)
        {
            return "Disabled";
        }

        if (isMiniWave)
        {
            return "MiniWave";
        }

        if (actualSpawnedCount <= 0)
        {
            return "ZeroSpawn";
        }

        if (!PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction(waveFaction))
        {
            return "NotPrimaryWave";
        }

        return "Incomplete";
    }

    private void LogTimerExtensionSkipped(
        long roundId,
        string waveId,
        string waveFaction,
        int actualSpawnedCount,
        string reason,
        bool vanillaResetDetected = false)
    {
        LogInfo(
            roundId,
            "TimerExtension",
            $"WaveId={waveId}; WaveFaction={waveFaction}; ActualSpawnedCount={actualSpawnedCount}; VanillaResetDetected={vanillaResetDetected}; SpawningFactionExtensionSeconds={spawningFactionTimerExtensionSeconds}; OpposingFactionExtensionSeconds={opposingFactionTimerExtensionSeconds}; FoundationTimerBeforeWave=Unavailable; ChaosTimerBeforeWave=Unavailable; FoundationTimerAfterVanillaReset=Unavailable; ChaosTimerAfterVanillaReset=Unavailable; FoundationTimerBeforeExtension=Unavailable; ChaosTimerBeforeExtension=Unavailable; FoundationExtension=Unavailable; ChaosExtension=Unavailable; FoundationTimerAfterExtension=Unavailable; ChaosTimerAfterExtension=Unavailable; Applied=false; Reason={reason}");
    }

    private static string FormatTimerSeconds(double seconds)
    {
        return seconds.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatOptionalTimerSeconds(double? seconds)
    {
        return seconds.HasValue
            ? FormatTimerSeconds(seconds.Value)
            : "Unavailable";
    }

    private void ScheduleSurvivalObservation(long roundId, MajorWaveRecord record)
    {
        if (!IsActiveRound(roundId))
        {
            return;
        }

        CoroutineHandle handle = default(CoroutineHandle);
        handle = Timing.CallDelayed(
            SurvivalObservationDelaySeconds,
            () => RunSurvivalObservation(roundId, record, handle));
        state!.ScheduledHandles.Add(handle);
    }

    private void RunSurvivalObservation(long roundId, MajorWaveRecord record, CoroutineHandle handle)
    {
        try
        {
            if (!IsActiveRound(roundId))
            {
                return;
            }

            int survivingCount = record.MemberIds
                .Select(Player.Get)
                .Count(IsAlivePrimaryWaveMember);
            if (!record.TryCompleteSurvivalObservation(survivingCount, DateTime.UtcNow))
            {
                return;
            }

            LogInfo(
                roundId,
                "PrimaryWaveSurvivalObserved",
                $"WaveId={record.WaveId}; ActualSpawnedCount={record.ActualSpawnedCount}; SurvivingCount={record.SurvivingCountAtObservation}; ObservedAt={record.SurvivalObservedAt:O}; DlrcJudgment=DeferredToModule03");
        }
        finally
        {
            RemoveScheduledHandle(roundId, handle);
        }
    }

    private void RemoveScheduledHandle(long roundId, CoroutineHandle handle)
    {
        if (IsActiveRound(roundId))
        {
            state!.ScheduledHandles.Remove(handle);
        }
    }

    private PrimaryWaveCaps GetCaps()
    {
        return config.PrimaryWaveCaps ?? new PrimaryWaveCaps();
    }

    private bool IsActive()
    {
        return state is not null && state.IsActive;
    }

    private bool IsActiveRound(long roundId)
    {
        return IsActive() && state!.RoundId == roundId;
    }

    private static TimedWave? FindTimedWave(SpawnableWaveBase wave)
    {
        return TimedWave.GetTimedWaves()
            .FirstOrDefault(candidate => ReferenceEquals(candidate.Base, wave));
    }

    private static List<Player> GetActuallySpawnedPlayers(
        IReadOnlyList<Player> candidates,
        TimedWave? wave)
    {
        List<Player> spawnedPlayers = new List<Player>();
        foreach (Player player in candidates)
        {
            bool matchesTargetTeam = wave is null || player.Role.Team == wave.Team;
            if (PrimaryWaveTimerExtensionPolicy.IsActualSpawnedPlayer(
                    player.IsConnected,
                    player.IsAlive,
                    matchesTargetTeam))
            {
                spawnedPlayers.Add(player);
            }
        }

        return spawnedPlayers;
    }

    private static bool IsAlivePrimaryWaveMember(Player? player)
    {
        return player is not null
            && player.IsConnected
            && player.IsAlive
            && player.Role.Type != RoleTypeId.Spectator
            && !player.IsOverwatchEnabled;
    }

    private void LogDetailed(long roundId, string action, string message)
    {
        if (config.Debug)
        {
            Log.Debug($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
        }
    }

    private static void LogInfo(long roundId, string action, string message)
    {
        Log.Info($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
    }

    private static void LogWarn(long roundId, string action, string message)
    {
        Log.Warn($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
    }

    private static void LogError(long roundId, string action, Exception exception)
    {
        Log.Error($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {exception}");
    }
}
