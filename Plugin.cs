using System;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
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

    public override string Name => "EmergencyEvents";

    public override string Author => "Qingran";

    public override Version Version => new Version(0, 1, 0);

    public override Version RequiredExiledVersion => new Version(9, 14, 2);

    public override void OnEnabled()
    {
        roundCoreManager = new RoundCoreManager(Config);
        reinforcementManager = new ReinforcementManager(Config);

        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.AllPlayersSpawned += OnAllPlayersSpawned;
        ServerEvents.SelectingRespawnTeam += OnSelectingRespawnTeam;
        ServerEvents.RespawningTeam += OnRespawningTeam;
        ServerEvents.RespawnedTeam += OnRespawnedTeam;
        ServerEvents.RoundEnded += OnRoundEnded;
        PlayerEvents.Escaped += OnPlayerEscaped;

        base.OnEnabled();
        Log.Info("[EmergencyEvents] Plugin enabled; Round Core handlers registered.");
    }

    public override void OnDisabled()
    {
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.AllPlayersSpawned -= OnAllPlayersSpawned;
        ServerEvents.SelectingRespawnTeam -= OnSelectingRespawnTeam;
        ServerEvents.RespawningTeam -= OnRespawningTeam;
        ServerEvents.RespawnedTeam -= OnRespawnedTeam;
        ServerEvents.RoundEnded -= OnRoundEnded;
        PlayerEvents.Escaped -= OnPlayerEscaped;

        reinforcementManager?.CleanupRound();
        reinforcementManager = null;
        roundCoreManager?.CleanupRound();
        roundCoreManager = null;

        Log.Info("[EmergencyEvents] Plugin disabled; Round Core handlers unregistered.");
        base.OnDisabled();
    }

    private void OnWaitingForPlayers()
    {
        reinforcementManager?.ResetForWaitingForPlayers();
        roundCoreManager?.ResetForWaitingForPlayers();
    }

    private void OnRoundStarted()
    {
        if (roundCoreManager?.State is null)
        {
            roundCoreManager?.CaptureRoundStart();
        }

        long roundId = roundCoreManager?.State?.RoundId ?? 0;
        reinforcementManager?.StartRound(roundId);
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
        reinforcementManager?.CleanupRound();
        roundCoreManager?.CleanupRound();
    }

    private void OnPlayerEscaped(EscapedEventArgs ev)
    {
        reinforcementManager?.HandleEscape(ev);
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
