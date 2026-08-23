using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Waves;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using Respawning.Waves;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 普通支援积分与原版支援波次的运行时边界。
/// </summary>
public sealed class ReinforcementManager
{
    private readonly Config config;
    private readonly Random random = new Random();
    private ReinforcementState? state;

    public ReinforcementManager(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public ReinforcementState? State => state;

    public void ResetForWaitingForPlayers()
    {
        CleanupRound();
    }

    public void StartRound(long roundId)
    {
        CleanupRound();

        if (!config.ReinforcementEnabled)
        {
            LogInfo(roundId, "Disabled", "ReinforcementEnabled=false，跳过普通支援调度。");
            return;
        }

        state = new ReinforcementState(roundId);
        float firstWindow = GetFirstWindowSeconds();
        float deadline = GetDeadlineSeconds(firstWindow);
        state.NextNormalWaveDueSeconds = firstWindow;

        LogInfo(
            roundId,
            "RoundStarted",
            $"FirstWaveWindow={FormatSeconds(firstWindow)}; Deadline={FormatSeconds(deadline)}; NormalWaveInterval={FormatSeconds(GetNormalIntervalSeconds())}; MiniWaves=Disabled; ForceWave=false; CarryoverRatio={GetCarryoverRatio():0.####}; ClassDValue={config.ClassDSupportScore}; ScientistValue={config.ScientistSupportScore}");

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
            LogInfo(
                state.RoundId,
                "MiniWaveCancelled",
                $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; OriginalWave={ev.Wave.Name}; Reason=MiniWavesDisabled");
            return;
        }

        if (elapsed < state.NextNormalWaveDueSeconds)
        {
            ev.IsAllowed = false;
            LogDebug(
                state.RoundId,
                "NormalWaveHeld",
                $"Elapsed={FormatSeconds(elapsed)}; OriginalFaction={ev.Team}; OriginalWave={ev.Wave.Name}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; Reason=FiveMinuteIntervalNotReached");
            return;
        }

        bool isFirstWave = state.FirstWaveState is FirstWaveState.NotReady or FirstWaveState.WaitingForObservers;

        SpawnableFaction selectedFaction;
        string selectionReason;
        if (isFirstWave)
        {
            selectedFaction = SelectFirstWaveFaction(out selectionReason);
            state.FirstWaveFaction = selectedFaction;
            state.HasFirstWaveFaction = true;
            state.FirstWaveState = FirstWaveState.Requested;
        }
        else
        {
            selectedFaction = SelectFactionBySupportRatio(out selectionReason);
        }

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
        if (state is null || !state.IsActive || ev.Wave is null)
        {
            return;
        }

        TimedWave? originalWave = ev.Wave;
        bool isMiniWave = originalWave?.IsMiniWave ?? false;

        if (isMiniWave)
        {
            ev.IsAllowed = false;
            state.PendingWaveFaction = null;
            state.PendingWaveIsMini = false;
            state.PendingWavePlayerCount = 0;
            LogInfo(
                state.RoundId,
                "MiniWaveCancelled",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; OriginalFaction={originalWave?.SpawnableFaction}; OriginalWave={originalWave?.Name ?? ev.Wave.GetType().Name}; Reason=MiniWavesDisabled");
            return;
        }

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

            ev.Players.RemoveAll(player => !IsEligibleObserver(player));
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
                LogWarn(state.RoundId, "FirstWaveCancelled", "Respawn event contained no eligible normal observers; will continue waiting until the deadline.");
                return;
            }

            state.FirstWaveRespawnStarted = true;
        }

        SpawnableFaction faction = ev.Wave.SpawnableFaction;
        if (faction == SpawnableFaction.None && state.FirstWaveState == FirstWaveState.Requested)
        {
            faction = state.FirstWaveFaction;
        }
        state.PendingWaveFaction = faction == SpawnableFaction.None ? null : faction;
        state.PendingWaveIsMini = isMiniWave;
        state.PendingWavePlayerCount = ev.Players.Count;

        LogDebug(
            state.RoundId,
            "RespawningTeam",
            $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(originalWave?.Name ?? ev.Wave.GetType().Name)}; IsMiniWave={isMiniWave}; Players={ev.Players.Count}; Maximum={ev.MaximumRespawnAmount}; Allowed={ev.IsAllowed}");
    }

    public void HandleRespawnedTeam(RespawnedTeamEventArgs ev)
    {
        if (state is null || !state.IsActive || ev.Wave is null)
        {
            return;
        }

        TimedWave? actualWave = FindTimedWave(ev.Wave);
        SpawnableFaction faction = ResolveFaction(ev.Wave, state.PendingWaveFaction ?? SpawnableFaction.None);
        int playerCount = ev.Players.Count();

        if (actualWave?.IsMiniWave == true || state.PendingWaveIsMini)
        {
            LogWarn(
                state.RoundId,
                "MiniWaveRejectedLate",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(actualWave?.Name ?? ev.Wave.GetType().Name)}; ActualSpawnCount={playerCount}; Reason=MiniWavesDisabled");
            state.PendingWaveFaction = null;
            state.PendingWaveIsMini = false;
            state.PendingWavePlayerCount = 0;
            return;
        }

        if (playerCount <= 0)
        {
            LogWarn(
                state.RoundId,
                "RespawnedTeamEmpty",
                $"Elapsed={FormatSeconds(GetElapsedSeconds())}; Faction={faction}; Wave={(actualWave?.Name ?? ev.Wave.GetType().Name)}; SupportCycleCommitted=false; Reason=NoPlayersSpawned");
            state.PendingWaveFaction = null;
            return;
        }

        state.SupportCycleCount++;
        if (state.FirstWaveState == FirstWaveState.Requested && !state.PendingWaveIsMini)
        {
            state.FirstWaveState = FirstWaveState.Completed;
        }

        state.LastWaveStartedAtUtc = DateTime.UtcNow;
        state.LastWaveName = actualWave?.Name ?? ev.Wave.GetType().Name;
        float completedElapsed = GetElapsedSeconds();
        state.NextNormalWaveDueSeconds = completedElapsed + GetNormalIntervalSeconds();

        LogInfo(
            state.RoundId,
            "SupportCycleCompleted",
            $"Elapsed={FormatSeconds(completedElapsed)}; Cycle={state.SupportCycleCount}; Faction={faction}; Wave={state.LastWaveName}; IsMiniWave=false; ActualSpawnCount={playerCount}; NextNormalWaveDue={FormatSeconds(state.NextNormalWaveDueSeconds)}; FirstWaveState={state.FirstWaveState}");

        ApplyScoreCarryover();
        state.PendingWaveFaction = null;
        state.PendingWavePlayerCount = 0;
    }

    public void CleanupRound()
    {
        if (state is null)
        {
            return;
        }

        long roundId = state.RoundId;
        state.IsActive = false;
        LogInfo(
            roundId,
            "Cleanup",
            $"FirstWaveState={state.FirstWaveState}; SupportCycles={state.SupportCycleCount}; FoundationScore={state.FoundationSupportScore}; ChaosScore={state.ChaosSupportScore}; ScoredEscapes={state.ScoredEscapePlayerIds.Count}");
        state = null;
    }

    private void ScheduleFirstWaveMonitor(long roundId, float delay)
    {
        Timing.CallDelayed(Math.Max(0.1f, delay), () => MonitorFirstWave(roundId));
    }

    private void MonitorFirstWave(long roundId)
    {
        if (state is null || !state.IsActive || state.RoundId != roundId)
        {
            return;
        }

        float elapsed = GetElapsedSeconds();
        RefreshFirstWaveWindowState(elapsed);

        if (state.FirstWaveState == FirstWaveState.NotReady || state.FirstWaveState == FirstWaveState.WaitingForObservers)
        {
            ScheduleFirstWaveMonitor(roundId, 1f);
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

        if (state.FirstWaveState == FirstWaveState.WaitingForObservers && elapsed >= deadline)
        {
            int observerCount = GetEligibleObservers().Count;
            if (observerCount == 0)
            {
                state.FirstWaveState = FirstWaveState.Skipped;
                LogWarn(
                    state.RoundId,
                    "FirstWaveDeadlineReached",
                    $"RoundTime={FormatSeconds(elapsed)}; Spectators=0; Decision=SKIP_FIRST_WAVE; Reason=NoEligibleSpectators");
            }
        }
    }

    private SpawnableFaction SelectFirstWaveFaction(out string reason)
    {
        if (state is null)
        {
            reason = "NoState";
            return SpawnableFaction.NtfWave;
        }

        if (state.FoundationSupportScore > state.ChaosSupportScore)
        {
            reason = "FoundationGreater";
            return SpawnableFaction.NtfWave;
        }

        if (state.ChaosSupportScore > state.FoundationSupportScore)
        {
            reason = "ChaosGreater";
            return SpawnableFaction.ChaosWave;
        }

        double roll = random.NextDouble();
        SpawnableFaction selected = roll < 0.5d ? SpawnableFaction.NtfWave : SpawnableFaction.ChaosWave;
        reason = $"TieRandom; Roll={roll:0.0000}; Threshold=0.5000";
        return selected;
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
            reason = $"BothZeroRandom; Roll={roll:0.0000}; Threshold=0.5000";
            return selected;
        }

        double ratioRoll = random.NextDouble() * total;
        double foundationThreshold = foundationScore;
        SpawnableFaction faction = ratioRoll < foundationThreshold ? SpawnableFaction.NtfWave : SpawnableFaction.ChaosWave;
        reason = $"SupportRatio; Foundation={foundationScore}/{total}; Chaos={chaosScore}/{total}; Roll={ratioRoll:0.0000}; FoundationThreshold={foundationThreshold:0.0000}";
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
        return player.IsConnected
            && player.Role.Type == RoleTypeId.Spectator
            && !player.IsOverwatchEnabled;
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
