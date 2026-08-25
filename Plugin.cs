using System;
using System.Linq;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Director;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using EmergencyEvents.Evaluation;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
using WarheadEvents = Exiled.Events.Handlers.Warhead;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;
using EmergencyEvents.Runtime;
using MEC;

namespace EmergencyEvents;

/// <summary>
/// emergency-events 主插件入口。
/// </summary>
public sealed partial class Plugin : Plugin<Config>
{
    private RoundCoreManager? roundCoreManager;
    private ReinforcementManager? reinforcementManager;
    private DlrcEvaluatorService? dlrcEvaluatorService;
    private CrisisManager? crisisManager;
    private FacilityDisorderRuntimeManager? facilityDisorderManager;
    private EventDirectorRuntimeManager? eventDirectorManager;
    private PluginRuntimeCoordinator? runtimeCoordinator;

    public override string Name => "EmergencyEvents";

    public override string Author => "Qingran";

    public override Version Version => new Version(0, 1, 0);

    public override Version RequiredExiledVersion => new Version(9, 14, 2);

    /// <summary>
    /// 当前已启用的插件实例，供 Remote Admin 命令转发请求。
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// 对后续模块提供当前回合最后一份有效 D-LRC 结果。
    /// </summary>
    public DlrcEvaluationResult? CurrentDlrcResult => dlrcEvaluatorService?.LastResult;

    /// <summary>
    /// 对后续 Module 05 提供当前回合最后一份有效危机评估。
    /// </summary>
    public CrisisAssessment? CurrentCrisisAssessment => crisisManager?.CurrentCrisisAssessment;

    public FacilityDisorderRuntimeManager? FacilityDisorder => facilityDisorderManager;

    public EventDirectorRuntimeManager? EventDirector => eventDirectorManager;

    public PluginRuntimeCoordinator? Runtime => runtimeCoordinator;

    /// <summary>
    /// 由 Remote Admin 请求一次立即 D-LRC 评估，不干预周期调度。
    /// </summary>
    public bool TryEvaluateDlrcImmediately(out DlrcEvaluationResult? result, out string response)
    {
        if (dlrcEvaluatorService is null)
        {
            result = null;
            response = "D-LRC 服务尚未启用。";
            return false;
        }

        return dlrcEvaluatorService.TryEvaluateImmediately(out result, out response);
    }

    /// <summary>
    /// 返回最近一次 D-LRC 评估的只读状态报告，不触发重新评估。
    /// </summary>
    public bool TryGetDlrcState(out string response)
    {
        if (dlrcEvaluatorService?.LastSnapshot is not RoundSnapshot snapshot
            || dlrcEvaluatorService.LastResult is not DlrcEvaluationResult result)
        {
            response = "D-LRC 尚未完成首次评估，当前没有可查询状态。";
            return false;
        }

        response = RemoteAdminCommands.DlrcStateReportFormatter.Format(
            snapshot,
            result,
            crisisManager?.CurrentCrisisAssessment);
        return true;
    }

    public override void OnEnabled()
    {
        Instance = this;
        runtimeCoordinator = new PluginRuntimeCoordinator(
            Config.EmergencyEventsEnabled,
            Config.MinimumPlayers);
        roundCoreManager = new RoundCoreManager(Config);
        reinforcementManager = new ReinforcementManager(Config, SnapshotCollector.CaptureScpCombatEquivalent);
        dlrcEvaluatorService = new DlrcEvaluatorService(Config);
        if (Config.CrisisSystemEnabled)
        {
            crisisManager = new CrisisManager(BuildCrisisOptions());
            crisisManager.CrisisAssessmentUpdated += OnCrisisAssessmentUpdated;
            crisisManager.CrisisChanged += OnCrisisChanged;
        }
        facilityDisorderManager = new FacilityDisorderRuntimeManager(Config.FacilityDisorder);
        eventDirectorManager = new EventDirectorRuntimeManager(
            new EventDirector(Array.Empty<EventDefinition>(), Config.EventDirector),
            Config.MinimumPlayers);
        reinforcementManager.MajorWaveCompleted += OnMajorWaveCompleted;
        dlrcEvaluatorService.EvaluationCompleted += OnDlrcEvaluationCompleted;

        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RestartingRound += OnRestartingRound;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.AllPlayersSpawned += OnAllPlayersSpawned;
        ServerEvents.SelectingRespawnTeam += OnSelectingRespawnTeam;
        ServerEvents.RespawningTeam += OnRespawningTeam;
        ServerEvents.RespawnedTeam += OnRespawnedTeam;
        ServerEvents.RoundEnded += OnRoundEnded;
        PlayerEvents.Joined += OnPlayerJoined;
        PlayerEvents.Left += OnPlayerLeft;
        PlayerEvents.Died += OnPlayerDied;
        PlayerEvents.ChangingRole += OnChangingRole;
        WarheadEvents.Stopping += OnWarheadStopping;
        WarheadEvents.Detonated += OnWarheadDetonated;

        base.OnEnabled();
        Log.Info("[EmergencyEvents] Plugin enabled; Round Core handlers registered.");
    }

    public override void OnDisabled()
    {
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RestartingRound -= OnRestartingRound;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.AllPlayersSpawned -= OnAllPlayersSpawned;
        ServerEvents.SelectingRespawnTeam -= OnSelectingRespawnTeam;
        ServerEvents.RespawningTeam -= OnRespawningTeam;
        ServerEvents.RespawnedTeam -= OnRespawnedTeam;
        ServerEvents.RoundEnded -= OnRoundEnded;
        PlayerEvents.Joined -= OnPlayerJoined;
        PlayerEvents.Left -= OnPlayerLeft;
        PlayerEvents.Died -= OnPlayerDied;
        PlayerEvents.ChangingRole -= OnChangingRole;
        WarheadEvents.Stopping -= OnWarheadStopping;
        WarheadEvents.Detonated -= OnWarheadDetonated;

        dlrcEvaluatorService?.CleanupRound("OnDisabled");
        facilityDisorderManager?.CleanupRound();
        eventDirectorManager?.CleanupRound();
        if (dlrcEvaluatorService is not null)
        {
            dlrcEvaluatorService.EvaluationCompleted -= OnDlrcEvaluationCompleted;
        }
        dlrcEvaluatorService = null;
        if (crisisManager is not null)
        {
            crisisManager.CrisisAssessmentUpdated -= OnCrisisAssessmentUpdated;
            crisisManager.CrisisChanged -= OnCrisisChanged;
            crisisManager.CleanupRound();
        }
        crisisManager = null;
        if (reinforcementManager is not null)
        {
            reinforcementManager.MajorWaveCompleted -= OnMajorWaveCompleted;
        }
        reinforcementManager?.Dispose();
        reinforcementManager = null;
        roundCoreManager?.CleanupRound();
        roundCoreManager = null;
        eventDirectorManager = null;
        runtimeCoordinator = null;
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }

        Log.Info("[EmergencyEvents] Plugin disabled; Round Core handlers unregistered.");
        base.OnDisabled();
    }

    private void OnWaitingForPlayers()
    {
        dlrcEvaluatorService?.ResetForWaitingForPlayers();
        facilityDisorderManager?.CleanupRound();
        eventDirectorManager?.CleanupRound();
        crisisManager?.CleanupRound();
        reinforcementManager?.ResetForWaitingForPlayers();
        roundCoreManager?.ResetForWaitingForPlayers();
        runtimeCoordinator?.EnterWaitingForPlayers();
    }

    private void OnRestartingRound()
    {
        RoundRestartResetter.Reset(
            reason => dlrcEvaluatorService?.CleanupRound(reason),
            () => reinforcementManager?.CleanupRound(),
            () => roundCoreManager?.CleanupRound());
        facilityDisorderManager?.CleanupRound();
        eventDirectorManager?.CleanupRound();
        crisisManager?.CleanupRound();
        runtimeCoordinator?.EndRound();
    }

    private void OnRoundStarted()
    {
        int openingPopulation = roundCoreManager?.GetCurrentOpeningPopulation() ?? 0;
        runtimeCoordinator?.BeginRound(openingPopulation);
        if (!IsEmergencyEventsActiveForRound())
        {
            LogActivationDecision(openingPopulation, "InsufficientPopulationOrDisabled");
            return;
        }

        roundCoreManager?.CaptureRoundStart();
        long roundId = roundCoreManager?.State?.RoundId ?? 0;
        reinforcementManager?.StartRound(
            roundId,
            roundCoreManager?.State?.Resolution.Tier ?? PopulationTier.E);
        dlrcEvaluatorService?.StartRound(roundCoreManager?.State, reinforcementManager);
        facilityDisorderManager?.StartRound(DateTime.UtcNow, openingPopulation, roundId);
        eventDirectorManager?.StartRound(
            roundId,
            roundCoreManager?.State?.Resolution.Tier ?? PopulationTier.E);
    }

    private void OnAllPlayersSpawned()
    {
        EvaluateCurrentPopulation();
        if (!IsEmergencyEventsActiveForRound())
        {
            return;
        }

        if (roundCoreManager?.State is null)
        {
            roundCoreManager?.CaptureRoundStart();
        }

        roundCoreManager?.ApplyOpeningComposition();
        facilityDisorderManager?.ScheduleOpeningForceBaseline();
    }

    private void OnRoundEnded(RoundEndedEventArgs _)
    {
        dlrcEvaluatorService?.CleanupRound("RoundEnded");
        facilityDisorderManager?.CleanupRound();
        eventDirectorManager?.CleanupRound();
        crisisManager?.CleanupRound();
        reinforcementManager?.CleanupRound();
        roundCoreManager?.CleanupRound();
        runtimeCoordinator?.EndRound();
    }

    private void OnPlayerJoined(JoinedEventArgs _)
    {
        SchedulePopulationEvaluation();
        facilityDisorderManager?.HandlePlayerJoined();
    }

    private void OnPlayerLeft(LeftEventArgs _)
    {
        SchedulePopulationEvaluation();
        facilityDisorderManager?.HandlePlayerLeft();
    }

    private void OnPlayerDied(DiedEventArgs ev)
    {
        roundCoreManager?.HandlePlayerDied(ev.Player);
        dlrcEvaluatorService?.HandlePlayerDied(ev.Player.Id, ev.TargetOldRole);
        facilityDisorderManager?.HandlePlayerDied(ev);
    }

    private void OnChangingRole(ChangingRoleEventArgs ev)
    {
        facilityDisorderManager?.HandleChangingRole(ev);
    }

    private void OnWarheadStopping(Exiled.Events.EventArgs.Warhead.StoppingEventArgs ev)
    {
        dlrcEvaluatorService?.HandleWarheadStopping(ev.IsAllowed);
    }

    private void OnWarheadDetonated()
    {
        dlrcEvaluatorService?.HandleWarheadDetonated(DateTime.UtcNow);
    }

    private void OnSelectingRespawnTeam(SelectingRespawnTeamEventArgs ev)
    {
        reinforcementManager?.HandleSelectingRespawnTeam(ev);
    }

    private void OnRespawningTeam(RespawningTeamEventArgs ev)
    {
        reinforcementManager?.HandleRespawningTeam(ev);
    }

    private void OnRespawnedTeam(RespawnedTeamEventArgs ev)
    {
        reinforcementManager?.HandleRespawnedTeam(ev);
        facilityDisorderManager?.HandleRespawnedTeam(ev);
    }

    private void OnMajorWaveCompleted(MajorWaveCompletedEvent ev)
    {
        dlrcEvaluatorService?.HandleMajorWaveCompleted(ev);
    }

    private bool IsEmergencyEventsActiveForRound()
    {
        return runtimeCoordinator?.IsEmergencyEventsActiveForRound == true;
    }

    private void SchedulePopulationEvaluation()
    {
        Timing.CallDelayed(0.1f, EvaluateCurrentPopulation);
    }

    private void EvaluateCurrentPopulation()
    {
        if (runtimeCoordinator?.ObservePopulation(GetCurrentOnlinePopulation()) != true)
        {
            return;
        }

        SuspendEmergencyEventsForRound("InsufficientPopulation");
    }

    private void SuspendEmergencyEventsForRound(string reason)
    {
        reinforcementManager?.SuspendRound(reason);
        dlrcEvaluatorService?.SuspendRound(reason);
        facilityDisorderManager?.ObservePopulation(runtimeCoordinator?.CurrentPopulation ?? 0);
        eventDirectorManager?.SuspendForRound(reason);
        Log.Info(
            $"[EmergencyEvents][Activation] RuntimeState={runtimeCoordinator?.State}; CurrentPlayers={runtimeCoordinator?.CurrentPopulation ?? 0}; MinimumPlayers={runtimeCoordinator?.MinimumPlayers ?? 0}; Decision={runtimeCoordinator?.State}; Reason={reason}");
    }

    private void LogActivationDecision(int currentPlayers, string reason)
    {
        Log.Info(
            $"[EmergencyEvents][Activation] CurrentPlayers={currentPlayers}; MinimumPlayers={runtimeCoordinator?.MinimumPlayers ?? 0}; RuntimeState={runtimeCoordinator?.State}; Decision=VANILLA_ROUND; EmergencyEventsActive=false; Reason={reason}");
    }

    private static int GetCurrentOnlinePopulation()
    {
        return Player.Enumerable.Count(player => player.IsConnected);
    }

    private static DirectorPersonnelFacts CreateDirectorPersonnelFacts(RoundSnapshot snapshot)
    {
        return new DirectorPersonnelFacts(
            snapshot.FoundationCombatants,
            snapshot.ChaosCombatants,
            snapshot.HostileThirdPartyCombatants,
            snapshot.EligibleSpectators,
            snapshot.OverwatchCount,
            snapshot.CurrentOnlinePlayers);
    }

    private void OnDlrcEvaluationCompleted(DlrcEvaluationCompletedEvent completedEvent)
    {
        CrisisAssessment? assessment = crisisManager?.Evaluate(completedEvent);
        facilityDisorderManager?.HandleEvaluation(completedEvent, assessment);
        eventDirectorManager?.HandleEvaluation(
            completedEvent,
            assessment,
            facilityDisorderManager?.State,
            reinforcementManager?.GetMajorWaveRecords() ?? Array.Empty<MajorWaveRecord>(),
            CreateDirectorPersonnelFacts(completedEvent.Snapshot),
            FacilityState.Normal,
            hasO4Selector: false);
    }

    private void OnCrisisAssessmentUpdated(CrisisAssessment assessment)
    {
        Log.Debug($"[EmergencyEvents]{CrisisLogFormatter.FormatDetailed(assessment)}");
    }

    private void OnCrisisChanged(CrisisAssessment? previous, CrisisAssessment current)
    {
        Log.Info($"[EmergencyEvents]{CrisisLogFormatter.FormatChange(previous, current)}");
    }

    private CrisisOptions BuildCrisisOptions()
    {
        return new CrisisOptions(
            Config.CrisisBioThresholdsE,
            Config.CrisisBioThresholdsD,
            Config.CrisisBioThresholdsC,
            Config.CrisisBioThresholdsB,
            Config.CrisisBioThresholdsA,
            Config.CrisisSecurityThresholdsE,
            Config.CrisisSecurityThresholdsD,
            Config.CrisisSecurityThresholdsC,
            Config.CrisisSecurityThresholdsB,
            Config.CrisisSecurityThresholdsA,
            Config.CrisisContainmentCheckpointSeconds,
            Config.CrisisContainmentEquivalentReduction,
            Config.CrisisEndLevel3Seconds,
            Config.CrisisEndLevel4Seconds,
            Config.CrisisEndLevel5Seconds);
    }
}
