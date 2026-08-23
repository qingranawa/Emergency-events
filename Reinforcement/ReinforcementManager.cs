using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Waves;
using Exiled.API.Features.Pickups;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using EmergencyEvents.Evaluation;
using InventorySystem.Items.Pickups;
using MEC;
using PlayerRoles;
using Respawning.Waves;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 普通支援积分与原版支援波次的运行时边界。
/// </summary>
public sealed class ReinforcementManager
{
    private const float MajorWaveEvaluationDelaySeconds = 120f;

    private readonly Config config;
    private readonly Random random = new Random();
    private readonly SupportScoreLedger supportScoreLedger = new SupportScoreLedger();
    private readonly HashSet<ushort> scp914PickupSerials = new HashSet<ushort>();
    private ReinforcementState? state;
    private bool nativeWavesPaused;

    public ReinforcementManager(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public ReinforcementState? State => state;

    public void HandlePlayerDied(int playerId)
    {
        if (state is null || !state.IsActive)
        {
            return;
        }

        foreach (MajorWaveRecord record in state.MajorWaveHistory)
        {
            if (record.IsEvaluationComplete || !record.MemberIds.Remove(playerId))
            {
                continue;
            }

            if (record.MemberIds.Count == 0)
            {
                CompleteMajorWave(record, 0, "EarlyTotalWipe");
            }
        }
    }

    public IReadOnlyList<MajorWaveSnapshot> GetMajorWaveHistorySnapshot()
    {
        if (state is null || !state.IsActive)
        {
            return Array.Empty<MajorWaveSnapshot>();
        }

        List<MajorWaveSnapshot> snapshots = state.MajorWaveHistory
            .Select(record => record.ToSnapshot())
            .ToList();
        return snapshots.AsReadOnly();
    }

    public void ResetForWaitingForPlayers()
    {
        CleanupRound();
    }

    public void StartRound(long roundId)
    {
        CleanupRound();
        supportScoreLedger.Clear();
        scp914PickupSerials.Clear();

        if (!config.ReinforcementEnabled)
        {
            LogInfo(roundId, "Disabled", "ReinforcementEnabled=false，跳过普通支援调度。");
            return;
        }

        state = new ReinforcementState(roundId);
        PauseNativeWaves(roundId);
        float firstWindow = GetFirstWindowSeconds();
        float deadline = GetDeadlineSeconds(firstWindow);
        state.NextNormalWaveDueSeconds = firstWindow;

        LogInfo(
            roundId,
            "RoundStarted",
            $"FirstWaveWindow={FormatSeconds(firstWindow)}; Deadline={FormatSeconds(deadline)}; NormalWaveInterval={FormatSeconds(GetNormalIntervalSeconds())}; MiniWaves=Disabled; PluginManagedWaves=true; NativeWavesPaused={nativeWavesPaused}; ManualRaWaves=true; CarryoverRatio={GetCarryoverRatio():0.####}; ClassDValue={config.ClassDSupportScore}; ScientistValue={config.ScientistSupportScore}");

        ScheduleFirstWaveMonitor(roundId, 1f);
    }

    public void HandleEscape(EscapedEventArgs ev)
    {
        if (state is null || !state.IsActive)
        {
            return;
        }

        Player player = ev.Player;
        string oldRole = ev.OldRole is null ? "Unknown" : ev.OldRole.Type.ToString();

        if (!state.ScoredEscapePlayerIds.Add(player.Id))
        {
            LogWarn(
                state.RoundId,
                "DuplicateEscapeScoreAttempt",
                $"Player={player.Id}; OldRole={oldRole}; Scenario={ev.EscapeScenario}; NewScoreRejected=true; Reason=EscapeAlreadyScoredThisRound");
            return;
        }

        if (!TryResolveEscapeCredit(ev.EscapeScenario, out bool foundationCredit, out int scoreValue, out string reason))
        {
            LogDebug(
                state.RoundId,
                "EscapeIgnored",
                $"Player={player.Id}; OldRole={oldRole}; Scenario={ev.EscapeScenario}; SupportScoreChange=0; Reason=UnsupportedEscapeScenario");
            return;
        }

        int foundationBefore = state.FoundationSupportScore;
        int chaosBefore = state.ChaosSupportScore;

        if (foundationCredit)
        {
            state.FoundationSupportScore += scoreValue;
        }
        else
        {
            state.ChaosSupportScore += scoreValue;
        }

        LogInfo(
            state.RoundId,
            "EscapeScored",
            $"Player={player.Id}; OldRole={oldRole}; Scenario={ev.EscapeScenario}; FoundationBefore={foundationBefore}; FoundationAfter={state.FoundationSupportScore}; ChaosBefore={chaosBefore}; ChaosAfter={state.ChaosSupportScore}; Added={(foundationCredit ? "Foundation" : "Chaos")}:{scoreValue}; Reason={reason}");
    }

    public void HandleScpDeath(DiedEventArgs ev)
    {
        if (state is null || !state.IsActive || !IsMainScpRole(ev.TargetOldRole))
        {
            return;
        }

        string scpId = $"Player:{ev.Player.Id}";
        SupportFaction faction = ResolveSupportFaction(ev.Attacker);
        bool scored = supportScoreLedger.TryScoreScpDeath(scpId, faction, out int score);
        AddSupportScore(faction, score);
        LogInfo(
            state.RoundId,
            scored ? "ScpDeathScored" : "ScpDeathIgnored",
            $"ScpId={scpId}; Role={ev.TargetOldRole}; AwardedFaction={faction}; Score={score}; FoundationScore={state.FoundationSupportScore}; ChaosScore={state.ChaosSupportScore}; Reason={(scored ? "MainScpDeath" : "DuplicateOrUnsupportedFaction")}");
    }

    public void HandleScpDamage(HurtingEventArgs ev)
    {
        if (state is null || !state.IsActive || !IsMainScpRole(ev.Player.Role.Type))
        {
            return;
        }

        double maxHealth = ev.Player.MaxHealth;
        SupportFaction faction = ResolveSupportFaction(ev.Attacker);
        IReadOnlyList<SupportThresholdAward> awards = supportScoreLedger.RecordScpDamage(
            $"Player:{ev.Player.Id}",
            Math.Max(0d, ev.Amount),
            maxHealth,
            faction);

        foreach (SupportThresholdAward award in awards)
        {
            AddSupportScore(faction, award.Score);
            LogInfo(
                state.RoundId,
                "ScpDamageThresholdScored",
                $"ScpId=Player:{ev.Player.Id}; Threshold={award.ThresholdPercent}%; AwardedFaction={award.Faction}; Score={award.Score}; Damage={ev.Amount:0.####}; MaxHealth={maxHealth:0.####}");
        }
    }

    public void HandleItemPickup(PickingUpItemEventArgs ev)
    {
        if (state is null || !state.IsActive || ev.Pickup is null)
        {
            return;
        }

        SupportItemKind itemKind = ResolveSupportItemKind(ev.Pickup.Type);
        if (itemKind == SupportItemKind.None)
        {
            return;
        }

        ushort itemSerial = ev.Pickup.Serial;
        SupportFaction faction = ResolveSupportFaction(ev.Player);
        bool createdByScp914 = scp914PickupSerials.Contains(itemSerial);
        bool scored = supportScoreLedger.TryScoreItem(
            itemSerial,
            itemKind,
            faction,
            createdByScp914,
            out int score);
        AddSupportScore(faction, score);

        LogInfo(
            state.RoundId,
            scored ? "ScpItemPickupScored" : "ScpItemPickupIgnored",
            $"ItemSerial={itemSerial}; ItemType={ev.Pickup.Type}; ItemKind={itemKind}; AwardedFaction={faction}; Score={score}; CreatedByScp914={createdByScp914}; Reason={(scored ? "FirstEligiblePickup" : "DuplicateOrUnsupportedPickup")}");
    }

    public void HandleScp914UpgradedPickup(UpgradedPickupEventArgs ev)
    {
        if (state is null || !state.IsActive || ev.Result is null)
        {
            return;
        }

        foreach (ItemPickupBase result in ev.Result)
        {
            Pickup? pickup = Pickup.Get(result);
            if (pickup is not null)
            {
                scp914PickupSerials.Add(pickup.Serial);
                LogDebug(state.RoundId, "Scp914PickupMarked", $"ItemSerial={pickup.Serial}; ItemType={pickup.Type}; Reason=Scp914Output");
            }
        }
    }

    public void HandleSelectingRespawnTeam(SelectingRespawnTeamEventArgs ev)
    {
        if (state is null || !state.IsActive || ev.Wave is null)
        {
            return;
        }

        float elapsed = GetElapsedSeconds();
        RefreshFirstWaveWindowState(elapsed);
        bool isMiniWave = ev.Wave.IsMiniWave;

        if (isMiniWave)
        {
            ev.IsAllowed = false;
            state.PluginWaveRequestPending = false;
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            LogInfo(
                state.RoundId,
                "MiniWaveCancelled",
                $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; OriginalWave={ev.Wave.Name}; Reason=MiniWavesDisabled");
            return;
        }

        if (!state.PluginWaveRequestPending)
        {
            if (WaveControlPolicy.ShouldAllowManualNormalWave(nativeWavesPaused, false))
            {
                ev.IsAllowed = true;
                state.PluginWaveRequestPending = false;
                state.PluginWaveInProgress = true;
                state.ManualWaveInProgress = true;
                LogInfo(
                    state.RoundId,
                    "ManualWaveAllowed",
                    $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; OriginalWave={ev.Wave.Name}; IsMiniWave=false; NativeWavesPaused=true; Origin=RA");
                return;
            }

            ev.IsAllowed = false;
            LogDebug(
                state.RoundId,
                "NativeWaveSuppressed",
                $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; OriginalWave={ev.Wave.Name}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; Reason=PluginManagedWavesOnly");
            return;
        }

        state.PluginWaveRequestPending = false;
        state.PluginWaveInProgress = true;
        state.ManualWaveInProgress = false;
        bool isFirstWave = state.FirstWaveState == FirstWaveState.Requested
            && !state.FirstWaveSelectionHandled;

        SpawnableFaction selectedFaction = state.RequestedWaveFaction ?? SpawnableFaction.None;
        if (selectedFaction == SpawnableFaction.None)
        {
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            ev.IsAllowed = false;
            LogWarn(state.RoundId, "WaveOverrideCancelled", $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; Reason=PluginFactionMissing");
            return;
        }

        state.RequestedWaveFaction = null;
        string selectionReason = isFirstWave ? "FirstWaveDecision" : "SupportScoreDecision";

        TimedWave? selectedWave = FindTimedWave(selectedFaction, false);
        if (selectedWave is null)
        {
            ev.IsAllowed = false;
            if (isFirstWave)
            {
                state.FirstWaveState = FirstWaveState.WaitingForObservers;
                state.HasFirstWaveFaction = false;
                state.FirstWaveFaction = SpawnableFaction.None;
            }

            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;

            LogWarn(
                state.RoundId,
                "WaveOverrideCancelled",
                $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; RequestedFaction={selectedFaction}; IsMiniWave=false; Reason=NativeNormalWaveNotFound");
            return;
        }

        SpawnableFaction originalFaction = ev.Team;
        ev.Wave = selectedWave;
        if (isFirstWave)
        {
            state.FirstWaveSelectionHandled = true;
        }

        LogInfo(
            state.RoundId,
            isFirstWave ? "FirstWaveSelected" : "WaveSelected",
            $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={originalFaction}; SelectedFaction={selectedFaction}; Wave={selectedWave.Name}; IsMiniWave=false; FoundationScore={state.FoundationSupportScore}; ChaosScore={state.ChaosSupportScore}; Reason={(isFirstWave ? "FirstWaveDecision" : selectionReason)}");
    }

    public void HandleRespawningTeam(RespawningTeamEventArgs ev)
    {
        if (state is null || !state.IsActive)
        {
            return;
        }

        if (ev.Wave is null)
        {
            ClearPendingWaveState();
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            LogWarn(state.RoundId, "RespawningTeamMissingWave", "Pending wave state cleared because the respawn event had no wave.");
            return;
        }

        TimedWave? originalWave = ev.Wave;
        bool isMiniWave = originalWave?.IsMiniWave ?? false;

        if (isMiniWave)
        {
            ev.IsAllowed = false;
            state.PluginWaveRequestPending = false;
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            state.PendingWaveFaction = null;
            state.PendingWaveIsMini = false;
            state.PendingWavePlayerCount = 0;
            state.PendingWavePlayerIds.Clear();
            LogInfo(
                state.RoundId,
                "MiniWaveCancelled",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; OriginalFaction={originalWave?.SpawnableFaction}; OriginalWave={originalWave?.Name ?? ev.Wave.GetType().Name}; Reason=MiniWavesDisabled");
            return;
        }

        if (!WaveControlPolicy.ShouldAllowTriggeredRespawn(
                state.PluginWaveRequestPending,
                state.PluginWaveInProgress,
                false))
        {
            ev.IsAllowed = false;
            ClearPendingWaveState();
            LogDebug(
                state.RoundId,
                "NativeWaveSuppressed",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; OriginalFaction={originalWave?.SpawnableFaction}; OriginalWave={originalWave?.Name ?? ev.Wave.GetType().Name}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; Reason=PluginManagedWavesOnly");
            return;
        }

        state.PluginWaveRequestPending = false;
        state.PluginWaveInProgress = true;
        state.ManualWaveInProgress = false;
        state.RequestedWaveFaction = null;

        bool isFirstWave = state.FirstWaveState == FirstWaveState.Requested
            && !state.FirstWaveRespawnStarted
            && state.FirstWaveSelectionHandled;

        if (isFirstWave)
        {
            TimedWave? selectedWave = FindTimedWave(state.FirstWaveFaction, false);
            if (selectedWave is not null)
            {
                ev.Wave = selectedWave;
            }

            int beforeFilter = ev.Players.Count;
            List<int> excludedPlayers = ev.Players
                .Where(player => !IsEligibleObserver(player))
                .Select(player => player.Id)
                .ToList();
            List<Player> eligibleObservers = GetEligibleObservers();

            ev.Players.Clear();
            ev.Players.AddRange(eligibleObservers);
            ev.MaximumRespawnAmount = ev.Players.Count;

            LogInfo(
                state.RoundId,
                "FirstWaveRespawning",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={state.FirstWaveFaction}; EligibleObservers={ev.Players.Count}; BeforeFilter={beforeFilter}; ExcludedOverwatch={string.Join(",", excludedPlayers)}; RequestedSpawnCount={ev.Players.Count}; IsMiniWave=false");

            if (ev.Players.Count == 0)
            {
                ev.IsAllowed = false;
                state.FirstWaveRespawnStarted = false;
                state.FirstWaveSelectionHandled = false;
                state.HasFirstWaveFaction = false;
                state.FirstWaveFaction = SpawnableFaction.None;
                state.FirstWaveState = FirstWaveState.WaitingForObservers;
                state.PendingWaveFaction = null;
                state.PendingWaveIsMini = false;
                state.PendingWavePlayerCount = 0;
                state.PendingWavePlayerIds.Clear();
                state.PluginWaveInProgress = false;
                state.ManualWaveInProgress = false;
                LogWarn(state.RoundId, "FirstWaveCancelled", "Respawn event contained no eligible normal observers; will continue waiting until the deadline.");
                return;
            }

            state.FirstWaveRespawnStarted = true;
        }
        else
        {
            List<Player> eligibleObservers = GetEligibleObservers();
            ev.Players.Clear();
            ev.Players.AddRange(eligibleObservers);
            ev.MaximumRespawnAmount = ev.Players.Count;

            if (ev.Players.Count == 0)
            {
                ev.IsAllowed = false;
                state.PluginWaveInProgress = false;
                state.ManualWaveInProgress = false;
                ClearPendingWaveState();
                LogWarn(state.RoundId, "WaveCancelled", $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Reason=NoEligibleObservers");
                return;
            }
        }

        SpawnableFaction faction = ev.Wave.SpawnableFaction;
        if (faction == SpawnableFaction.None && state.FirstWaveState == FirstWaveState.Requested)
        {
            faction = state.FirstWaveFaction;
        }
        state.PendingWaveFaction = faction == SpawnableFaction.None ? null : faction;
        state.PendingWaveIsMini = isMiniWave;
        state.PendingWavePlayerCount = ev.Players.Count;
        state.PendingWavePlayerIds.Clear();
        foreach (Player player in ev.Players)
        {
            state.PendingWavePlayerIds.Add(player.Id);
        }

        LogDebug(
            state.RoundId,
            "RespawningTeam",
            $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(originalWave?.Name ?? ev.Wave.GetType().Name)}; IsMiniWave={isMiniWave}; Players={ev.Players.Count}; Maximum={ev.MaximumRespawnAmount}; Allowed={ev.IsAllowed}");
    }

    public void HandleRespawnedTeam(RespawnedTeamEventArgs ev)
    {
        if (state is null || !state.IsActive)
        {
            return;
        }

        if (ev.Wave is null)
        {
            ClearPendingWaveState();
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            LogWarn(state.RoundId, "RespawnedTeamMissingWave", "Pending wave state cleared because the respawned event had no wave.");
            return;
        }

        if (!state.PluginWaveInProgress)
        {
            LogDebug(
                state.RoundId,
                "NativeWaveSuppressed",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Wave={ev.Wave.GetType().Name}; Reason=PluginManagedWavesOnly");
            return;
        }

        TimedWave? actualWave = FindTimedWave(ev.Wave);
        SpawnableFaction faction = ResolveFaction(ev.Wave, state.PendingWaveFaction ?? SpawnableFaction.None);
        List<Player> spawnedPlayers = ev.Players.ToList();
        int playerCount = spawnedPlayers.Count;

        if (actualWave?.IsMiniWave == true || state.PendingWaveIsMini)
        {
            LogWarn(
                state.RoundId,
                "MiniWaveRejectedLate",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(actualWave?.Name ?? ev.Wave.GetType().Name)}; ActualSpawnCount={playerCount}; Reason=MiniWavesDisabled");
            state.PendingWaveFaction = null;
            state.PendingWaveIsMini = false;
            state.PendingWavePlayerCount = 0;
            state.PendingWavePlayerIds.Clear();
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            return;
        }

        if (playerCount <= 0)
        {
            LogWarn(
                state.RoundId,
                "RespawnedTeamEmpty",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(actualWave?.Name ?? ev.Wave.GetType().Name)}; SupportCycleCommitted=false; Reason=NoPlayersSpawned");
            state.PendingWaveFaction = null;
            state.PendingWaveIsMini = false;
            state.PendingWavePlayerCount = 0;
            state.PendingWavePlayerIds.Clear();
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            return;
        }

        bool hasNormalWaveEvidence = actualWave is not null
            || state.PendingWaveFaction.HasValue
            || state.PendingWavePlayerCount > 0;
        if (!hasNormalWaveEvidence)
        {
            LogWarn(
                state.RoundId,
                "RespawnedTeamUnknownWave",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; ActualSpawnCount={playerCount}; SupportCycleCommitted=false; Reason=NormalWaveIdentityUnavailable");
            ClearPendingWaveState();
            state.PluginWaveInProgress = false;
            state.ManualWaveInProgress = false;
            return;
        }

        state.SupportCycleCount++;
        bool wasManualWave = state.ManualWaveInProgress;
        if (state.FirstWaveState == FirstWaveState.Requested && !state.PendingWaveIsMini)
        {
            state.FirstWaveState = FirstWaveState.Completed;
        }

        DateTime waveStartedAtUtc = DateTime.UtcNow;
        state.LastWaveStartedAtUtc = waveStartedAtUtc;
        state.LastWaveName = actualWave?.Name ?? ev.Wave.GetType().Name;
        float completedElapsed = GetElapsedSeconds();
        state.NextNormalWaveDueSeconds = FirstWavePolicy.GetNextFixedWaveDue(
            state.NextNormalWaveDueSeconds,
            GetNormalIntervalSeconds());

        LogInfo(
            state.RoundId,
            "SupportCycleCompleted",
            $"Elapsed={FormatSeconds(completedElapsed)}; Cycle={state.SupportCycleCount}; Faction={faction}; Wave={state.LastWaveName}; IsMiniWave=false; Origin={(wasManualWave ? "RA" : "Plugin")}; ActualSpawnCount={playerCount}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; FirstWaveState={state.FirstWaveState}");

        MajorWaveRecord record = new MajorWaveRecord(
            state.LastWaveName,
            playerCount,
            spawnedPlayers.Select(player => player.Id),
            waveStartedAtUtc);
        state.MajorWaveHistory.Add(record);
        LogInfo(
            state.RoundId,
            "MajorWaveRecorded",
            $"Wave={record.Name}; StartingCount={record.StartingCount}; StartedAt={record.StartedAt:O}; EvaluationDelay={MajorWaveEvaluationDelaySeconds:0}s");
        ScheduleMajorWaveEvaluation(state.RoundId, record);

        ApplyScoreCarryover();
        state.PluginWaveInProgress = false;
        state.ManualWaveInProgress = false;
        state.PendingWaveFaction = null;
        state.PendingWaveIsMini = false;
        state.PendingWavePlayerCount = 0;
        state.PendingWavePlayerIds.Clear();
    }

    public void CleanupRound()
    {
        if (state is null)
        {
            ResumeNativeWaves(0);
            supportScoreLedger.Clear();
            scp914PickupSerials.Clear();
            return;
        }

        long roundId = state.RoundId;
        state.IsActive = false;
        int majorWaveHistoryCount = state.MajorWaveHistory.Count;
        int scheduledHandleCount = state.ScheduledHandles.Count;
        LogInfo(
            roundId,
            "Cleanup",
            $"FirstWaveState={state.FirstWaveState}; SupportCycles={state.SupportCycleCount}; FoundationScore={state.FoundationSupportScore}; ChaosScore={state.ChaosSupportScore}; ScoredEscapes={state.ScoredEscapePlayerIds.Count}; MajorWaveHistory={majorWaveHistoryCount}; ScheduledHandles={scheduledHandleCount}");
        foreach (CoroutineHandle handle in state.ScheduledHandles)
        {
            Timing.KillCoroutines(handle);
        }

        state.ScheduledHandles.Clear();
        state.MajorWaveHistory.Clear();
        state.PendingWavePlayerIds.Clear();
        LogInfo(
            roundId,
            "CleanupHandlesCleared",
            $"MajorWaveHistoryCleared={majorWaveHistoryCount}; ScheduledHandlesCleared={scheduledHandleCount}; PendingWavePlayerIdsCleared=true");
        state = null;
        ResumeNativeWaves(roundId);
        supportScoreLedger.Clear();
        scp914PickupSerials.Clear();
    }

    private void ScheduleFirstWaveMonitor(long roundId, float delay)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId)
        {
            return;
        }

        CoroutineHandle handle = default(CoroutineHandle);
        handle = Timing.CallDelayed(
            Math.Max(0.1f, delay),
            () => RunFirstWaveMonitor(roundId, handle));
        state.ScheduledHandles.Add(handle);
    }

    private void ScheduleMajorWaveEvaluation(long roundId, MajorWaveRecord record)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId || record.IsEvaluationComplete)
        {
            return;
        }

        CoroutineHandle handle = default(CoroutineHandle);
        handle = Timing.CallDelayed(
            MajorWaveEvaluationDelaySeconds,
            () => RunMajorWaveEvaluation(roundId, record, handle));
        state.ScheduledHandles.Add(handle);
    }

    private void RunFirstWaveMonitor(long roundId, CoroutineHandle handle)
    {
        try
        {
            MonitorFirstWave(roundId);
        }
        finally
        {
            RemoveScheduledHandle(roundId, handle);
        }
    }

    private void RunMajorWaveEvaluation(long roundId, MajorWaveRecord record, CoroutineHandle handle)
    {
        try
        {
            EvaluateMajorWave(roundId, record);
        }
        finally
        {
            RemoveScheduledHandle(roundId, handle);
        }
    }

    private void RemoveScheduledHandle(long roundId, CoroutineHandle handle)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId)
        {
            return;
        }

        state.ScheduledHandles.Remove(handle);
    }

    private void ClearPendingWaveState()
    {
        if (state is null)
        {
            return;
        }

        state.PendingWaveFaction = null;
        state.PendingWaveIsMini = false;
        state.PendingWavePlayerCount = 0;
        state.PendingWavePlayerIds.Clear();
    }

    private void EvaluateMajorWave(long roundId, MajorWaveRecord record)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId || !state.MajorWaveHistory.Contains(record) || record.IsEvaluationComplete)
        {
            return;
        }

        int survivingCount = record.MemberIds
            .Select(playerId => Player.Get(playerId))
            .Count(IsEligibleMajorWaveSurvivor);
        CompleteMajorWave(record, survivingCount, "Timed120Seconds");
    }

    private void CompleteMajorWave(MajorWaveRecord record, int survivingCount, string reason)
    {
        if (state is null || !state.IsActive || record.IsEvaluationComplete)
        {
            return;
        }

        int normalizedSurvivingCount = Math.Max(0, Math.Min(record.StartingCount, survivingCount));
        record.SurvivingCountAtEvaluation = normalizedSurvivingCount;
        record.BaseFailureScore = GetMajorWaveFailureScore(record.StartingCount, normalizedSurvivingCount);
        record.IsCatastrophic = normalizedSurvivingCount == 0;
        record.IsEvaluationComplete = true;
        record.EvaluatedAt = DateTime.UtcNow;
        record.EvaluationReason = reason;

        string action = reason == "EarlyTotalWipe" ? "MajorWaveEarlyWipe" : "MajorWaveEvaluation";
        LogInfo(
            state.RoundId,
            action,
            $"Wave={record.Name}; StartingCount={record.StartingCount}; SurvivingCount={record.SurvivingCountAtEvaluation}; BaseFailureScore={record.BaseFailureScore:0.####}; IsCatastrophic={record.IsCatastrophic}; Reason={record.EvaluationReason}");
    }

    private static bool IsEligibleMajorWaveSurvivor(Player? player)
    {
        return player is not null
            && player.IsConnected
            && player.IsAlive
            && player.Role.Type != RoleTypeId.Spectator
            && !player.IsOverwatchEnabled;
    }

    private static double GetMajorWaveFailureScore(int startingCount, int survivingCount)
    {
        if (survivingCount <= 0)
        {
            return 15d;
        }

        if (startingCount <= 0)
        {
            return 0d;
        }

        double survivalRatio = survivingCount / (double)startingCount;
        if (survivalRatio > 0.75d)
        {
            return 0d;
        }

        if (survivalRatio > 0.50d)
        {
            return 4d;
        }

        if (survivalRatio > 0.25d)
        {
            return 8d;
        }

        return 12d;
    }

    private void MonitorFirstWave(long roundId)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId)
        {
            return;
        }

        float elapsed = GetElapsedSeconds();
        RefreshFirstWaveWindowState(elapsed);

        if (!state.PluginWaveRequestPending
            && !state.PluginWaveInProgress
            && WaveControlPolicy.IsDue(elapsed, state.NextNormalWaveDueSeconds)
            && !Respawn.IsSpawning)
        {
            List<Player> eligibleObservers = GetEligibleObservers();
            if (eligibleObservers.Count > 0)
            {
                RequestDueWave(elapsed, eligibleObservers.Count);
            }
            else if (state.FirstWaveState is FirstWaveState.Completed or FirstWaveState.Skipped)
            {
                SkipElapsedWaveWindows(elapsed);
            }
        }

        ScheduleFirstWaveMonitor(roundId, 1f);
    }

    private void RequestDueWave(float elapsed, int eligibleObserverCount)
    {
        if (state is null)
        {
            return;
        }

        bool isFirstWave = state.FirstWaveState is FirstWaveState.NotReady or FirstWaveState.WaitingForObservers;
        string selectionReason;
        SpawnableFaction selectedFaction = SelectFactionBySupportRatio(out selectionReason);

        state.RequestedWaveFaction = selectedFaction;
        state.PluginWaveRequestPending = true;
        if (isFirstWave)
        {
            state.FirstWaveFaction = selectedFaction;
            state.HasFirstWaveFaction = true;
            state.FirstWaveState = FirstWaveState.Requested;
        }

        LogInfo(
            state.RoundId,
            isFirstWave ? "FirstWaveTimerTriggered" : "WaveTimerTriggered",
            $"Elapsed={FormatSeconds(elapsed)}; Due={FormatSeconds(state.NextNormalWaveDueSeconds)}; EligibleObservers={eligibleObserverCount}; SelectedFaction={selectedFaction}; Reason={selectionReason}; TicketsPreserved=true");

        try
        {
            Respawn.ForceWave(selectedFaction);
        }
        catch (Exception exception)
        {
            state.PluginWaveRequestPending = false;
            state.RequestedWaveFaction = null;
            state.FirstWaveState = isFirstWave ? FirstWaveState.WaitingForObservers : state.FirstWaveState;
            state.HasFirstWaveFaction = false;
            state.FirstWaveFaction = SpawnableFaction.None;
            LogError(state.RoundId, "WaveForceFailed", exception, $"Elapsed={FormatSeconds(elapsed)}; Faction={selectedFaction}");
        }
    }

    private void RefreshFirstWaveWindowState(float elapsed)
    {
        if (state is null)
        {
            return;
        }

        float firstWindow = GetFirstWindowSeconds();
        float deadline = GetDeadlineSeconds(firstWindow);

        if (state.FirstWaveState == FirstWaveState.NotReady && elapsed >= firstWindow)
        {
            state.FirstWaveState = FirstWaveState.WaitingForObservers;
            int observerCount = GetEligibleObservers().Count;

            LogInfo(
                state.RoundId,
                "FirstWaveWindowOpened",
                $"RoundTime={FormatSeconds(elapsed)}; Spectators={observerCount}; Deadline={FormatSeconds(deadline)}; Decision=WAIT_FOR_NATIVE_NORMAL_WAVE; ForceWave=false");
        }

        bool isFirstWavePending = state.FirstWaveState is FirstWaveState.NotReady
            or FirstWaveState.WaitingForObservers
            or FirstWaveState.Requested;
        int observerCountAtDeadline = GetEligibleObservers().Count;
        if (!state.PluginWaveRequestPending
            && !state.PluginWaveInProgress
            && FirstWavePolicy.ShouldSkip(
                isFirstWavePending,
                elapsed,
                deadline,
                observerCountAtDeadline))
        {
            state.FirstWaveState = FirstWaveState.Skipped;
            state.FirstWaveRespawnStarted = false;
            state.FirstWaveSelectionHandled = false;
            state.HasFirstWaveFaction = false;
            state.FirstWaveFaction = SpawnableFaction.None;
            state.NextNormalWaveDueSeconds = FirstWavePolicy.GetNextNormalWaveDueAfterSkip(
                firstWindow,
                GetNormalIntervalSeconds());
            ClearPendingWaveState();
            LogWarn(
                state.RoundId,
                "FirstWaveDeadlineReached",
                $"RoundTime={FormatSeconds(elapsed)}; Spectators=0; Decision=SKIP_FIRST_WAVE; Reason=NoEligibleSpectators; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}");
        }
    }

    private void SkipElapsedWaveWindows(float elapsed)
    {
        if (state is null)
        {
            return;
        }

        float previousDue = state.NextNormalWaveDueSeconds;
        int skippedWindows = 0;
        do
        {
            state.NextNormalWaveDueSeconds = FirstWavePolicy.GetNextFixedWaveDue(
                state.NextNormalWaveDueSeconds,
                GetNormalIntervalSeconds());
            skippedWindows++;
        }
        while (WaveControlPolicy.IsDue(elapsed, state.NextNormalWaveDueSeconds));

        LogInfo(
            state.RoundId,
            "WaveWindowSkipped",
            $"Elapsed={FormatSeconds(elapsed)}; PreviousDue={FormatSeconds(previousDue)}; SkippedWindows={skippedWindows}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; Reason=NoEligibleObservers; TicketsPreserved=true");
    }

    private void AddSupportScore(SupportFaction faction, int score)
    {
        if (state is null || score <= 0)
        {
            return;
        }

        if (faction == SupportFaction.Foundation)
        {
            state.FoundationSupportScore += score;
        }
        else if (faction == SupportFaction.Chaos)
        {
            state.ChaosSupportScore += score;
        }
    }

    private void PauseNativeWaves(long roundId)
    {
        try
        {
            Respawn.PauseWaves();
            nativeWavesPaused = true;
            LogInfo(roundId, "NativeWavesPaused", "原版正常/mini wave 计时器已暂停；RA 正常大波保留，mini wave 仍禁止。");
        }
        catch (Exception exception)
        {
            nativeWavesPaused = false;
            LogError(roundId, "NativeWavesPauseFailed", exception);
        }
    }

    private void ResumeNativeWaves(long roundId)
    {
        if (!nativeWavesPaused)
        {
            return;
        }

        try
        {
            Respawn.ResumeWaves();
            LogInfo(roundId, "NativeWavesResumed", "插件回合状态清理，原版 Respawn Waves 计时器已恢复。");
        }
        catch (Exception exception)
        {
            LogError(roundId, "NativeWavesResumeFailed", exception);
        }
        finally
        {
            nativeWavesPaused = false;
        }
    }

    private static SupportFaction ResolveSupportFaction(Player? player)
    {
        if (player is null)
        {
            return SupportFaction.None;
        }

        if (player.IsFoundationForces)
        {
            return SupportFaction.Foundation;
        }

        if (player.IsCHI)
        {
            return SupportFaction.Chaos;
        }

        return SupportFaction.None;
    }

    private static bool IsMainScpRole(RoleTypeId role)
    {
        return role == RoleTypeId.Scp049
            || role == RoleTypeId.Scp079
            || role == RoleTypeId.Scp096
            || role == RoleTypeId.Scp106
            || role == RoleTypeId.Scp173
            || role == RoleTypeId.Scp3114
            || role == RoleTypeId.Scp939;
    }

    private static SupportItemKind ResolveSupportItemKind(ItemType itemType)
    {
        string itemName = itemType.ToString();
        if (!itemName.StartsWith("SCP", StringComparison.OrdinalIgnoreCase))
        {
            return SupportItemKind.None;
        }

        return itemType == ItemType.SCP207
            || itemType == ItemType.SCP330
            || itemType == ItemType.SCP500
            ? SupportItemKind.ConsumableScp
            : SupportItemKind.UniqueScp;
    }

    private SpawnableFaction SelectFactionBySupportRatio(out string reason)
    {
        if (state is null)
        {
            reason = "NoState";
            return SpawnableFaction.NtfWave;
        }

        int foundationScore = Math.Max(0, state.FoundationSupportScore);
        int chaosScore = Math.Max(0, state.ChaosSupportScore);
        int total = foundationScore + chaosScore;

        if (total == 0)
        {
            double roll = random.NextDouble();
            SpawnableFaction selected = roll < 0.5d ? SpawnableFaction.NtfWave : SpawnableFaction.ChaosWave;
            reason = $"SupportRatio; FoundationProbability=0.5000; ChaosProbability=0.5000; RandomRoll={roll:0.0000}; SelectedFaction={selected}";
            return selected;
        }

        double foundationProbability = foundationScore / (double)total;
        double chaosProbability = chaosScore / (double)total;
        double ratioRoll = random.NextDouble();
        SpawnableFaction faction = ratioRoll < foundationProbability ? SpawnableFaction.NtfWave : SpawnableFaction.ChaosWave;
        reason = $"SupportRatio; FoundationProbability={foundationProbability:0.0000}; ChaosProbability={chaosProbability:0.0000}; RandomRoll={ratioRoll:0.0000}; SelectedFaction={faction}";
        return faction;
    }

    private void ApplyScoreCarryover()
    {
        if (state is null)
        {
            return;
        }

        double ratio = GetCarryoverRatio();
        int foundationBefore = state.FoundationSupportScore;
        int chaosBefore = state.ChaosSupportScore;
        int foundationAfter = RoundScore(foundationBefore * ratio);
        int chaosAfter = RoundScore(chaosBefore * ratio);

        state.FoundationSupportScore = foundationAfter;
        state.ChaosSupportScore = chaosAfter;

        LogInfo(
            state.RoundId,
            "DecaySupportScores",
            $"Reason=SupportCycleCompleted; CarryoverRatio={ratio:0.####}; FoundationBefore={foundationBefore}; FoundationCalculation={foundationBefore}*{ratio:0.####}={foundationBefore * ratio:0.####}; FoundationAfter={foundationAfter}; ChaosBefore={chaosBefore}; ChaosCalculation={chaosBefore}*{ratio:0.####}={chaosBefore * ratio:0.####}; ChaosAfter={chaosAfter}");
    }

    private double GetCarryoverRatio()
    {
        return Math.Max(0d, Math.Min(1d, config.SupportScoreCarryoverRatio));
    }

    private float GetFirstWindowSeconds()
    {
        return Math.Max(0f, config.FirstReinforcementTimeSeconds);
    }

    private float GetDeadlineSeconds(float firstWindow)
    {
        return Math.Max(firstWindow, config.FirstReinforcementDeadlineSeconds);
    }

    private float GetNormalIntervalSeconds()
    {
        return Math.Max(1f, config.NormalReinforcementIntervalSeconds);
    }

    private float GetElapsedSeconds()
    {
        return (float)Round.ElapsedTime.TotalSeconds;
    }

    private static List<Player> GetEligibleObservers()
    {
        return Player.Enumerable
            .Where(IsEligibleObserver)
            .ToList();
    }

    private static bool IsEligibleObserver(Player player)
    {
        RoleTypeId role = player.Role.Type;
        return WaveControlPolicy.IsEligibleObserver(
            player.IsConnected,
            player.IsOverwatchEnabled,
            role == RoleTypeId.Spectator,
            role == RoleTypeId.None);
    }

    private bool TryResolveEscapeCredit(
        EscapeScenario scenario,
        out bool foundationCredit,
        out int scoreValue,
        out string reason)
    {
        foundationCredit = false;
        scoreValue = 0;
        reason = string.Empty;

        switch (scenario)
        {
            case EscapeScenario.ClassD:
                foundationCredit = false;
                scoreValue = Math.Max(0, config.ClassDSupportScore);
                reason = "ClassDNormalEscape";
                return true;
            case EscapeScenario.CuffedClassD:
                foundationCredit = true;
                scoreValue = Math.Max(0, config.ClassDSupportScore);
                reason = "ClassDCuffedEscape";
                return true;
            case EscapeScenario.Scientist:
                foundationCredit = true;
                scoreValue = Math.Max(0, config.ScientistSupportScore);
                reason = "ScientistNormalEscape";
                return true;
            case EscapeScenario.CuffedScientist:
                foundationCredit = false;
                scoreValue = Math.Max(0, config.ScientistSupportScore);
                reason = "ScientistCuffedEscape";
                return true;
            default:
                return false;
        }
    }

    private static TimedWave? FindTimedWave(SpawnableFaction faction, bool isMiniWave)
    {
        return TimedWave.GetTimedWaves()
            .FirstOrDefault(wave => wave.SpawnableFaction == faction && wave.IsMiniWave == isMiniWave);
    }

    private static TimedWave? FindTimedWave(SpawnableWaveBase wave)
    {
        return TimedWave.GetTimedWaves()
            .FirstOrDefault(candidate => ReferenceEquals(candidate.Base, wave));
    }

    private static SpawnableFaction ResolveFaction(SpawnableWaveBase wave, SpawnableFaction fallback)
    {
        TimedWave? timedWave = FindTimedWave(wave);
        return timedWave?.SpawnableFaction ?? fallback;
    }

    private static int RoundScore(double score)
    {
        return (int)Math.Round(score, MidpointRounding.AwayFromZero);
    }

    private static string FormatSeconds(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0f, seconds));
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }

    private static void LogInfo(long roundId, string action, string message)
    {
        Log.Info($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
    }

    private static void LogWarn(long roundId, string action, string message)
    {
        Log.Warn($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
    }

    private void LogDebug(long roundId, string action, string message)
    {
        if (!config.Debug)
        {
            return;
        }

        Log.Debug($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message}");
    }

    private static void LogError(long roundId, string action, Exception exception, string? message = null)
    {
        Log.Error($"[EmergencyEvents][Reinforcement][{DateTime.UtcNow:O}][RoundId={roundId}][{action}] {message ?? string.Empty} {exception}");
    }
}
