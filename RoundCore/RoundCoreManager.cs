using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using EmergencyEvents.RoundCore;
using MEC;
using PlayerRoles;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// Round Core 的 EXILED 运行时边界。
/// </summary>
public sealed class RoundCoreManager
{
    private static readonly RoleTypeId[] ScpRolePool =
    {
        RoleTypeId.Scp049,
        RoleTypeId.Scp079,
        RoleTypeId.Scp096,
        RoleTypeId.Scp106,
        RoleTypeId.Scp173,
        RoleTypeId.Scp3114,
        RoleTypeId.Scp939,
    };

    private readonly Config config;
    private readonly Random random = new Random();
    private readonly Dictionary<int, string?> originalBadgeNames = new Dictionary<int, string?>();
    private RoundCoreState? state;
    private long roundId;
    private bool isApplying;

    public RoundCoreManager(Config config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public RoundCoreState? State => state;

    public void ResetForWaitingForPlayers()
    {
        RestoreBadges();
        state = null;
        isApplying = false;
        LogInfo(0, "Reset", "等待下一局，Round Core 状态已清理。");
    }

    public void CaptureRoundStart()
    {
        RestoreBadges();
        isApplying = false;
        roundId++;

        List<Player> players = GetOpeningRoster();
        CompositionResolution resolution = CompositionResolver.GetComposition(players.Count);
        state = new RoundCoreState(
            roundId,
            players.Count,
            resolution,
            players.Select(player => player.Id),
            DateTime.UtcNow);

        LogInfo(
            roundId,
            "CaptureRoundStart",
            $"Population={players.Count}; Tier={resolution.Tier}; Supported={resolution.IsSupported}; Roster={string.Join(",", players.Select(player => player.Id))}");

        if (!resolution.IsSupported)
        {
            LogWarn(
                roundId,
                "UnsupportedPopulation",
                $"Population={players.Count}; fallbackTier={resolution.Tier}; reason={resolution.UnsupportedReason}; 保留原版开局，不强行套用组成表。");
        }
    }

    public void ApplyOpeningComposition()
    {
        if (!config.RoundCoreEnabled)
        {
            LogInfo(roundId, "Disabled", "RoundCoreEnabled=false，跳过开局接管。");
            return;
        }

        if (state is null)
        {
            LogWarn(roundId, "ApplySkipped", "AllPlayersSpawned 时没有已锁定的 Round Core 状态。");
            return;
        }

        if (state.IsInitialized || state.IsSkipped)
        {
            LogDebug(state.RoundId, "ApplySkipped", "本局已经处理过开局编制。");
            return;
        }

        if (!state.Resolution.IsSupported || state.Resolution.Composition is null)
        {
            state.IsSkipped = true;
            LogWarn(state.RoundId, "ApplySkipped", "当前人口没有可用的精确组成表，保留原版开局。");
            return;
        }

        if (isApplying)
        {
            LogDebug(state.RoundId, "ApplySkipped", "开局编制正在执行，忽略重复回调。");
            return;
        }

        isApplying = true;

        try
        {
            RoundComposition composition = state.Resolution.Composition;
            List<Player> players = state.RoundStartPlayerIds
                .Select(id => GetPlayerById(id))
                .Where(player => player is not null && player.IsConnected)
                .Cast<Player>()
                .ToList();

            if (players.Count != state.StartPopulation)
            {
                LogWarn(
                    state.RoundId,
                    "RosterChanged",
                    $"LockedPopulation={state.StartPopulation}; ConnectedLockedRoster={players.Count}; 仍按锁定编制执行，缺失玩家不会被晚加入玩家替代。");
            }

            Shuffle(players);
            List<RoleAssignment> assignments = BuildAssignments(players, composition);
            List<Player> securityPlayers = new List<Player>();
            List<Player> chaosPlayers = new List<Player>();
            List<Player> assignedPlayers = new List<Player>();

            foreach (RoleAssignment assignment in assignments)
            {
                if (!TrySetRole(assignment.Player, assignment.Role))
                {
                    continue;
                }

                assignedPlayers.Add(assignment.Player);

                if (assignment.Role == RoleTypeId.FacilityGuard)
                {
                    securityPlayers.Add(assignment.Player);
                }
                else if (assignment.Role == RoleTypeId.ChaosConscript)
                {
                    chaosPlayers.Add(assignment.Player);
                }
            }

            ApplyTeamBadges(securityPlayers, config.SecurityTitle);
            ApplyTeamBadges(chaosPlayers, config.ChaosTitle);
            ApplyMirroredEquipment(securityPlayers, chaosPlayers);
            ApplyAdditionalEquipment(securityPlayers, config.SecurityExtraStartingItems, "SecurityExtraEquipmentApplied");

            bool foundationUsesElevatorA = random.Next(0, 2) == 0;
            state.FoundationUsesElevatorA = foundationUsesElevatorA;

            if (config.TeleportOpeningTeamsToHczElevators)
            {
                TeleportOpeningTeams(securityPlayers, chaosPlayers, foundationUsesElevatorA);
            }

            state.AssignedPlayerCount = assignedPlayers.Count;
            state.IsInitialized = true;

            ScheduleTeamBadgeRefresh(securityPlayers, config.SecurityTitle, state.RoundId);
            ScheduleTeamBadgeRefresh(chaosPlayers, config.ChaosTitle, state.RoundId);

            LogInfo(
                state.RoundId,
                "ApplyOpeningComposition",
                $"Expected={composition}; Assigned={assignedPlayers.Count}; Security={securityPlayers.Count}; Chaos={chaosPlayers.Count}; FoundationElevator={(foundationUsesElevatorA ? "A" : "B")}; ChaosElevator={(foundationUsesElevatorA ? "B" : "A")}");

            ValidateAppliedComposition(state, assignedPlayers, securityPlayers, chaosPlayers, composition);
        }
        catch (Exception exception)
        {
            LogError(state.RoundId, "ApplyFailed", exception);
        }
        finally
        {
            isApplying = false;
        }
    }

    public void CleanupRound()
    {
        RestoreBadges();

        if (state is not null)
        {
            LogInfo(state.RoundId, "Cleanup", $"Assigned={state.AssignedPlayerCount}; Initialized={state.IsInitialized}; Skipped={state.IsSkipped}");
        }

        state = null;
        isApplying = false;
    }

    private List<Player> GetOpeningRoster()
    {
        return Player.Enumerable
            .Where(IsEligibleOpeningPlayer)
            .ToList();
    }

    private static bool IsEligibleOpeningPlayer(Player player)
    {
        if (!player.IsConnected)
        {
            return false;
        }

        RoleTypeId role = player.Role.Type;
        return role != RoleTypeId.Spectator && role != RoleTypeId.Overwatch;
    }

    private static Player? GetPlayerById(int id)
    {
        return Player.Enumerable.FirstOrDefault(player => player.Id == id);
    }

    private List<RoleAssignment> BuildAssignments(IReadOnlyList<Player> players, RoundComposition composition)
    {
        List<RoleAssignment> assignments = new List<RoleAssignment>(composition.Total);
        List<RoleTypeId> shuffledScpRoles = BuildScpRoles(composition.ScpCount);

        LogDebug(
            state?.RoundId ?? roundId,
            "ScpRolesResolved",
            $"Requested={composition.ScpCount}; Roles={string.Join(",", shuffledScpRoles)}; Scp939Count={shuffledScpRoles.Count(role => role == RoleTypeId.Scp939)}; Scp3114InPool={ScpRolePool.Contains(RoleTypeId.Scp3114)}");

        int playerIndex = 0;

        for (int index = 0; index < composition.ScpCount && playerIndex < players.Count; index++)
        {
            assignments.Add(new RoleAssignment(players[playerIndex++], shuffledScpRoles[index % shuffledScpRoles.Count]));
        }

        AddAssignments(assignments, players, ref playerIndex, composition.SecurityCount, RoleTypeId.FacilityGuard);
        AddAssignments(assignments, players, ref playerIndex, composition.ChaosInfiltratorCount, RoleTypeId.ChaosConscript);
        AddAssignments(assignments, players, ref playerIndex, composition.ClassDCount, RoleTypeId.ClassD);
        AddAssignments(assignments, players, ref playerIndex, composition.ScientistCount, RoleTypeId.Scientist);

        if (assignments.Count != composition.Total)
        {
            LogError(
                state?.RoundId ?? roundId,
                "AssignmentCountMismatch",
                new InvalidOperationException($"Expected {composition.Total} assignments, built {assignments.Count} from {players.Count} connected locked players."));
        }

        return assignments;
    }

    private List<RoleTypeId> BuildScpRoles(int count)
    {
        if (count <= 0)
        {
            return new List<RoleTypeId>();
        }

        List<RoleTypeId> roles = new List<RoleTypeId>(count);
        int guaranteed939Count = Math.Min(count, 2);

        for (int index = 0; index < guaranteed939Count; index++)
        {
            roles.Add(RoleTypeId.Scp939);
        }

        List<RoleTypeId> remainingPool = ScpRolePool
            .Where(role => role != RoleTypeId.Scp939)
            .ToList();
        Shuffle(remainingPool);

        for (int index = roles.Count; index < count; index++)
        {
            int remainingIndex = (index - guaranteed939Count) % remainingPool.Count;
            roles.Add(remainingPool[remainingIndex]);
        }

        Shuffle(roles);
        return roles;
    }

    private static void AddAssignments(
        ICollection<RoleAssignment> assignments,
        IReadOnlyList<Player> players,
        ref int playerIndex,
        int count,
        RoleTypeId role)
    {
        for (int index = 0; index < count && playerIndex < players.Count; index++)
        {
            assignments.Add(new RoleAssignment(players[playerIndex++], role));
        }
    }

    private bool TrySetRole(Player player, RoleTypeId role)
    {
        try
        {
            player.Role.Set(role, SpawnReason.RoundStart);
            LogDebug(state?.RoundId ?? roundId, "RoleAssigned", $"Player={player.Id}; Role={role}");
            return true;
        }
        catch (Exception exception)
        {
            LogError(state?.RoundId ?? roundId, "RoleAssignmentFailed", exception, $"Player={player.Id}; Role={role}");
            return false;
        }
    }

    private void ApplyTeamBadges(IEnumerable<Player> players, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            LogWarn(state?.RoundId ?? roundId, "BadgeSkipped", "称号为空，跳过 Badge 追加。");
            return;
        }

        foreach (Player player in players)
        {
            try
            {
                if (!originalBadgeNames.ContainsKey(player.Id))
                {
                    originalBadgeNames[player.Id] = player.RankName;
                }

                string originalBadge = originalBadgeNames[player.Id] ?? string.Empty;
                string expectedBadge = BuildBadgeWithTitle(originalBadge, title);
                player.RankName = expectedBadge;
                string actualBadge = player.RankName ?? string.Empty;
                bool matches = string.Equals(actualBadge, expectedBadge, StringComparison.Ordinal);

                LogDebug(
                    state?.RoundId ?? roundId,
                    "BadgeApplied",
                    $"Player={player.Id}; OriginalBadge={originalBadge}; Expected={expectedBadge}; Actual={actualBadge}; Match={matches}");
            }
            catch (Exception exception)
            {
                LogError(state?.RoundId ?? roundId, "BadgeFailed", exception, $"Player={player.Id}; Title={title}");
            }
        }
    }

    private void ScheduleTeamBadgeRefresh(IEnumerable<Player> players, string title, long currentRoundId)
    {
        int[] playerIds = players.Select(player => player.Id).ToArray();

        LogDebug(
            currentRoundId,
            "BadgeRefreshScheduled",
            $"Players={string.Join(",", playerIds)}; Title={title}; DelaySeconds=0.50; Reason=WaitForRoleAndRoundInitialization");

        Timing.CallDelayed(0.5f, () => ReapplyTeamBadges(playerIds, title, currentRoundId));
    }

    private void ReapplyTeamBadges(IEnumerable<int> playerIds, string title, long currentRoundId)
    {
        if (state is null || state.RoundId != currentRoundId || !state.IsInitialized)
        {
            LogDebug(currentRoundId, "BadgeRefreshSkipped", "Reason=RoundCoreStateNoLongerActive");
            return;
        }

        foreach (int playerId in playerIds)
        {
            Player? player = GetPlayerById(playerId);
            if (player is null || !player.IsConnected)
            {
                LogDebug(currentRoundId, "BadgeRefreshSkipped", $"Player={playerId}; Reason=PlayerUnavailable");
                continue;
            }

            try
            {
                string originalBadge = originalBadgeNames.TryGetValue(player.Id, out string? savedBadge)
                    ? savedBadge ?? string.Empty
                    : player.RankName ?? string.Empty;
                string expectedBadge = BuildBadgeWithTitle(originalBadge, title);
                player.RankName = expectedBadge;
                string actualBadge = player.RankName ?? string.Empty;
                bool matches = string.Equals(actualBadge, expectedBadge, StringComparison.Ordinal);
                LogDebug(
                    currentRoundId,
                    "BadgeReapplied",
                    $"Player={player.Id}; OriginalBadge={originalBadge}; Expected={expectedBadge}; Actual={actualBadge}; Match={matches}");
            }
            catch (Exception exception)
            {
                LogError(currentRoundId, "BadgeRefreshFailed", exception, $"Player={player.Id}; Title={title}");
            }
        }
    }

    private static string BuildBadgeWithTitle(string originalBadge, string title)
    {
        string normalizedBadge = originalBadge?.Trim() ?? string.Empty;
        string normalizedTitle = title.Trim();
        string suffix = $" ({normalizedTitle})";

        if (normalizedBadge.EndsWith(suffix, StringComparison.Ordinal))
        {
            return normalizedBadge;
        }

        return string.IsNullOrEmpty(normalizedBadge)
            ? normalizedTitle
            : normalizedBadge + suffix;
    }

    private void ApplyMirroredEquipment(IEnumerable<Player> securityPlayers, IEnumerable<Player> chaosPlayers)
    {
        IEnumerable<string> configuredItems = config.MirroredStartingItems ?? Enumerable.Empty<string>();
        string[] itemNames = configuredItems.Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();

        foreach (Player player in securityPlayers.Concat(chaosPlayers))
        {
            try
            {
                player.ClearInventory(true);

                foreach (string itemName in itemNames)
                {
                    if (!Enum.TryParse(itemName, ignoreCase: true, out ItemType itemType))
                    {
                        LogWarn(state?.RoundId ?? roundId, "InvalidMirrorItem", $"Player={player.Id}; ItemType={itemName}");
                        continue;
                    }

                    player.AddItem(itemType);
                }

                LogDebug(state?.RoundId ?? roundId, "MirrorEquipmentApplied", $"Player={player.Id}; Items={string.Join(",", itemNames)}");
            }
            catch (Exception exception)
            {
                LogError(state?.RoundId ?? roundId, "MirrorEquipmentFailed", exception, $"Player={player.Id}");
            }
        }
    }

    private void ApplyAdditionalEquipment(IEnumerable<Player> players, IEnumerable<string>? configuredItems, string action)
    {
        string[] itemNames = (configuredItems ?? Enumerable.Empty<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        foreach (Player player in players)
        {
            try
            {
                foreach (string itemName in itemNames)
                {
                    if (!Enum.TryParse(itemName, ignoreCase: true, out ItemType itemType))
                    {
                        LogWarn(state?.RoundId ?? roundId, "InvalidExtraItem", $"Player={player.Id}; ItemType={itemName}");
                        continue;
                    }

                    player.AddItem(itemType);
                }

                LogDebug(state?.RoundId ?? roundId, action, $"Player={player.Id}; Items={string.Join(",", itemNames)}");
            }
            catch (Exception exception)
            {
                LogError(state?.RoundId ?? roundId, "ExtraEquipmentFailed", exception, $"Player={player.Id}");
            }
        }
    }

    private void TeleportOpeningTeams(
        IReadOnlyList<Player> securityPlayers,
        IReadOnlyList<Player> chaosPlayers,
        bool foundationUsesElevatorA)
    {
        Room? elevatorA = Room.Get(RoomType.HczElevatorA);
        Room? elevatorB = Room.Get(RoomType.HczElevatorB);

        if (elevatorA is null || elevatorB is null)
        {
            LogWarn(state?.RoundId ?? roundId, "ElevatorRoomMissing", $"ElevatorA={(elevatorA is not null)}; ElevatorB={(elevatorB is not null)}; 保留原版出生位置。");
            return;
        }

        TeleportGroup(securityPlayers, foundationUsesElevatorA ? elevatorA : elevatorB);
        TeleportGroup(chaosPlayers, foundationUsesElevatorA ? elevatorB : elevatorA);
    }

    private void TeleportGroup(IEnumerable<Player> players, Room room)
    {
        int index = 0;

        foreach (Player player in players)
        {
            try
            {
                player.Teleport(room);
                LogDebug(state?.RoundId ?? roundId, "PlayerTeleported", $"Player={player.Id}; Room={room.Type}; Index={index}");
                index++;
            }
            catch (Exception exception)
            {
                LogError(state?.RoundId ?? roundId, "TeleportFailed", exception, $"Player={player.Id}; Room={room.Type}");
            }
        }
    }

    private void ValidateAppliedComposition(
        RoundCoreState currentState,
        IEnumerable<Player> assignedPlayers,
        IReadOnlyCollection<Player> securityPlayers,
        IReadOnlyCollection<Player> chaosPlayers,
        RoundComposition expected)
    {
        List<Player> assigned = assignedPlayers.ToList();
        int actualScp = assigned.Count(player => ScpRolePool.Contains(player.Role.Type));
        int actualSecurity = securityPlayers.Count(player => player.Role.Type == RoleTypeId.FacilityGuard);
        int actualChaos = chaosPlayers.Count(player => player.Role.Type == RoleTypeId.ChaosConscript);
        int actualClassD = assigned.Count(player => player.Role.Type == RoleTypeId.ClassD);
        int actualScientist = assigned.Count(player => player.Role.Type == RoleTypeId.Scientist);

        bool valid = assigned.Count == expected.Total
            && actualScp == expected.ScpCount
            && actualSecurity == expected.SecurityCount
            && actualChaos == expected.ChaosInfiltratorCount
            && actualClassD == expected.ClassDCount
            && actualScientist == expected.ScientistCount;

        string actual = $"{actualScp}/{actualSecurity}/{actualChaos}/{actualClassD}/{actualScientist}";
        if (!valid)
        {
            LogError(
                currentState.RoundId,
                "RuntimeValidationFailed",
                new InvalidOperationException($"Expected={expected}; Actual={assigned.Count}|{actual}"));
            return;
        }

        LogInfo(currentState.RoundId, "RuntimeValidationPassed", $"Actual={assigned.Count}|{actual}");
    }

    private void RestoreBadges()
    {
        foreach (KeyValuePair<int, string?> original in originalBadgeNames)
        {
            Player? player = GetPlayerById(original.Key);
            if (player is null || !player.IsConnected)
            {
                continue;
            }

            try
            {
                player.RankName = original.Value ?? string.Empty;
                LogDebug(roundId, "BadgeRestored", $"Player={player.Id}; Badge={original.Value ?? "<empty>"}");
            }
            catch (Exception exception)
            {
                LogError(roundId, "BadgeRestoreFailed", exception, $"Player={original.Key}");
            }
        }

        originalBadgeNames.Clear();
    }

    private void Shuffle<T>(IList<T> values)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static void LogInfo(long currentRoundId, string action, string message)
    {
        Log.Info($"[EmergencyEvents][RoundCore][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }

    private void LogDebug(long currentRoundId, string action, string message)
    {
        if (!config.Debug)
        {
            return;
        }

        Log.Debug($"[EmergencyEvents][RoundCore][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }

    private static void LogWarn(long currentRoundId, string action, string message)
    {
        Log.Warn($"[EmergencyEvents][RoundCore][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message}");
    }

    private static void LogError(long currentRoundId, string action, Exception exception, string? message = null)
    {
        Log.Error($"[EmergencyEvents][RoundCore][{DateTime.UtcNow:O}][RoundId={currentRoundId}][{action}] {message ?? ""} {exception}");
    }

    private readonly struct RoleAssignment
    {
        public RoleAssignment(Player player, RoleTypeId role)
        {
            Player = player;
            Role = role;
        }

        public Player Player { get; }

        public RoleTypeId Role { get; }
    }
}
