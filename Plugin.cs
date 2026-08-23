using System;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;
using Exiled.Events.EventArgs.Server;
using EmergencyEvents.Evaluation;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
using WarheadEvents = Exiled.Events.Handlers.Warhead;
using Scp914Events = Exiled.Events.Handlers.Scp914;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents;

/// <summary>
/// emergency-events 主插件入口。
/// </summary>
public sealed class Plugin : Plugin<Config>
{
    private RoundCoreManager? roundCoreManager;
    private ReinforcementManager? reinforcementManager;
    private DlrcEvaluatorService? dlrcEvaluatorService;

    public override string Name => "EmergencyEvents";

    public override string Author => "Qingran";

    public override Version Version => new Version(0, 1, 0);

    public override Version RequiredExiledVersion => new Version(9, 14, 2);

    /// <summary>
    /// 对后续模块提供当前回合最后一份有效 D-LRC 结果。
    /// </summary>
    public DlrcEvaluationResult? CurrentDlrcResult => dlrcEvaluatorService?.LastResult;

    public override void OnEnabled()
    {
        roundCoreManager = new RoundCoreManager(Config);
        reinforcementManager = new ReinforcementManager(Config);
        dlrcEvaluatorService = new DlrcEvaluatorService(Config);

        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RestartingRound += OnRestartingRound;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.AllPlayersSpawned += OnAllPlayersSpawned;
        ServerEvents.SelectingRespawnTeam += OnSelectingRespawnTeam;
        ServerEvents.RespawningTeam += OnRespawningTeam;
        ServerEvents.RespawnedTeam += OnRespawnedTeam;
        ServerEvents.RoundEnded += OnRoundEnded;
        PlayerEvents.Escaped += OnPlayerEscaped;
        PlayerEvents.Died += OnPlayerDied;
        PlayerEvents.Hurting += OnPlayerHurting;
        PlayerEvents.PickingUpItem += OnPlayerPickingUpItem;
        Scp914Events.UpgradedPickup += OnScp914UpgradedPickup;
        WarheadEvents.Stopping += OnWarheadStopping;

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
        PlayerEvents.Escaped -= OnPlayerEscaped;
        PlayerEvents.Died -= OnPlayerDied;
        PlayerEvents.Hurting -= OnPlayerHurting;
        PlayerEvents.PickingUpItem -= OnPlayerPickingUpItem;
        Scp914Events.UpgradedPickup -= OnScp914UpgradedPickup;
        WarheadEvents.Stopping -= OnWarheadStopping;

        dlrcEvaluatorService?.CleanupRound("OnDisabled");
        dlrcEvaluatorService = null;
        reinforcementManager?.CleanupRound();
        reinforcementManager = null;
        roundCoreManager?.CleanupRound();
        roundCoreManager = null;

        Log.Info("[EmergencyEvents] Plugin disabled; Round Core handlers unregistered.");
        base.OnDisabled();
    }

    private void OnWaitingForPlayers()
    {
        dlrcEvaluatorService?.ResetForWaitingForPlayers();
        reinforcementManager?.ResetForWaitingForPlayers();
        roundCoreManager?.ResetForWaitingForPlayers();
    }

    private void OnRestartingRound()
    {
        RoundRestartResetter.Reset(
            reason => dlrcEvaluatorService?.CleanupRound(reason),
            () => reinforcementManager?.CleanupRound(),
            () => roundCoreManager?.CleanupRound());
    }

    private void OnRoundStarted()
    {
        if (roundCoreManager?.State is null)
        {
            roundCoreManager?.CaptureRoundStart();
        }

        long roundId = roundCoreManager?.State?.RoundId ?? 0;
        reinforcementManager?.StartRound(roundId);
        dlrcEvaluatorService?.StartRound(roundCoreManager?.State, reinforcementManager);
    }

    private void OnAllPlayersSpawned()
    {
        if (roundCoreManager?.State is null)
        {
            roundCoreManager?.CaptureRoundStart();
        }

        roundCoreManager?.ApplyOpeningComposition();
    }

    private void OnRoundEnded(RoundEndedEventArgs _)
    {
        dlrcEvaluatorService?.CleanupRound("RoundEnded");
        reinforcementManager?.CleanupRound();
        roundCoreManager?.CleanupRound();
    }

    private void OnPlayerEscaped(EscapedEventArgs ev)
    {
        reinforcementManager?.HandleEscape(ev);
    }

    private void OnPlayerDied(DiedEventArgs ev)
    {
        roundCoreManager?.HandlePlayerDied(ev.Player);
        reinforcementManager?.HandleScpDeath(ev);
        reinforcementManager?.HandlePlayerDied(ev.Player.Id);
        dlrcEvaluatorService?.HandlePlayerDied(ev.Player.Id, ev.TargetOldRole);
    }

    private void OnPlayerHurting(HurtingEventArgs ev)
    {
        reinforcementManager?.HandleScpDamage(ev);
    }

    private void OnPlayerPickingUpItem(PickingUpItemEventArgs ev)
    {
        reinforcementManager?.HandleItemPickup(ev);
    }

    private void OnScp914UpgradedPickup(UpgradedPickupEventArgs ev)
    {
        reinforcementManager?.HandleScp914UpgradedPickup(ev);
    }

    private void OnWarheadStopping(Exiled.Events.EventArgs.Warhead.StoppingEventArgs ev)
    {
        dlrcEvaluatorService?.HandleWarheadStopping(ev.IsAllowed);
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
    }
}
