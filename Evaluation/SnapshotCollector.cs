using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using PlayerRoles;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 将一轮游戏状态物化为单次、只读的评估输入。
/// </summary>
public sealed class SnapshotCollector
{
    public RoundSnapshot Collect(
        RoundCoreState? roundCoreState,
        ReinforcementManager? reinforcementManager,
        BattlefieldMomentumSnapshot? momentum,
        int warheadCancellationCount,
        DateTime timestamp,
        TimeSpan elapsed)
    {
        List<Player> players = Player.Enumerable.ToList();
        List<int> activePlayerIds = new List<int>();
        List<ScpSnapshot> scpStates = new List<ScpSnapshot>();
        int currentOnlinePlayers = 0;
        int foundationCombatants = 0;
        int chaosCombatants = 0;
        int otherHostileCombatants = 0;
        int classDAlive = 0;
        int scientistsAlive = 0;
        int eligibleSpectators = 0;
        int overwatchCount = 0;
        int mainScpAlive = 0;
        int scp0492Count = 0;
        bool scp079Present = false;
        int scp079Tier = 0;
        bool scp079TierIsValid = true;
        int surfaceFoundationCombatants = 0;
        int surfaceChaosCombatants = 0;
        int surfaceMainScp = 0;
        int surfaceOtherHostiles = 0;

        foreach (Player player in players)
        {
            if (!player.IsConnected)
            {
                continue;
            }

            currentOnlinePlayers++;
            RoleTypeId role = player.Role.Type;
            bool isOverwatch = role == RoleTypeId.Overwatch || player.IsOverwatchEnabled;
            bool isSpectator = role == RoleTypeId.Spectator;
            bool isAlive = player.IsAlive;
            bool isSurface = player.Zone == ZoneType.Surface;

            if (isOverwatch)
            {
                overwatchCount++;
            }
            else if (isSpectator)
            {
                eligibleSpectators++;
            }

            if (!isAlive || isOverwatch || isSpectator)
            {
                continue;
            }

            activePlayerIds.Add(player.Id);
            if (IsFoundationRole(role))
            {
                foundationCombatants++;
                if (isSurface)
                {
                    surfaceFoundationCombatants++;
                }
            }
            else if (IsHostileRole(role))
            {
                chaosCombatants++;
                if (isSurface)
                {
                    surfaceChaosCombatants++;
                }
            }
            else if (role == RoleTypeId.ClassD)
            {
                classDAlive++;
                continue;
            }
            else if (role == RoleTypeId.Scientist)
            {
                scientistsAlive++;
                continue;
            }

            string roleName = role.ToString();
            if (role == RoleTypeId.Scp0492)
            {
                scp0492Count++;
                if (isSurface)
                {
                    surfaceOtherHostiles++;
                }
                continue;
            }

            if (role == RoleTypeId.Scp079)
            {
                mainScpAlive++;
                scp079Present = true;
                Scp079TierRead tierRead = ReadScp079Tier(player);
                scp079Tier = Math.Max(scp079Tier, tierRead.Value);
                scp079TierIsValid &= tierRead.IsValid;
                if (isSurface)
                {
                    surfaceMainScp++;
                }
                scpStates.Add(new ScpSnapshot(
                    roleName,
                    isAlive: true,
                    isScp079: true,
                    healthDataUnavailable: true));
                continue;
            }

            if (IsMainScpRole(roleName))
            {
                mainScpAlive++;
                if (isSurface)
                {
                    surfaceMainScp++;
                }
                scpStates.Add(ReadMainScpSnapshot(player, roleName));
                continue;
            }

            if (!IsFoundationRole(role) && !IsHostileRole(role))
            {
                otherHostileCombatants++;
                if (isSurface)
                {
                    surfaceOtherHostiles++;
                }
            }
        }

        RoundCoreState? core = roundCoreState;
        int roundStartPopulation = core?.StartPopulation ?? 0;
        int startingScpCount = core?.Resolution.Composition?.ScpCount ?? 0;
        PopulationTier populationTier = core?.Resolution.Tier ?? PopulationTier.E;
        long roundId = core?.RoundId ?? 0L;
        BattlefieldMomentumSnapshot actualMomentum = momentum
            ?? new BattlefieldMomentumSnapshot(0, 0, 0);

        return new RoundSnapshot(
            roundId,
            timestamp,
            elapsed,
            populationTier,
            roundStartPopulation,
            startingScpCount,
            currentOnlinePlayers,
            foundationCombatants,
            chaosCombatants,
            otherHostileCombatants,
            classDAlive,
            scientistsAlive,
            eligibleSpectators,
            overwatchCount,
            mainScpAlive,
            scpStates,
            scp0492Count,
            scp079Present,
            scp079Tier,
            !Warhead.IsLocked,
            Warhead.IsInProgress,
            Warhead.IsDetonated,
            Math.Max(0, warheadCancellationCount),
            reinforcementManager?.GetMajorWaveHistorySnapshot(),
            actualMomentum.FoundationDeaths,
            actualMomentum.HostileHumanDeaths,
            actualMomentum.MainScpDeaths,
            activePlayerIds,
            hostileThirdPartyActive: false,
            hostileThirdPartyCombatants: 0,
            surfaceFoundationCombatants: surfaceFoundationCombatants,
            surfaceChaosCombatants: surfaceChaosCombatants,
            surfaceMainScp: surfaceMainScp,
            surfaceOtherHostiles: surfaceOtherHostiles,
            scp079TierIsValid: scp079TierIsValid);
    }

    private static ScpSnapshot ReadMainScpSnapshot(Player player, string roleName)
    {
        try
        {
            double currentHealth = Convert.ToDouble(player.Health);
            double maxHealth = Convert.ToDouble(player.MaxHealth);
            double currentHume = Convert.ToDouble(player.HumeShield);
            double maxHume = Convert.ToDouble(player.MaxHumeShield);
            bool isHealthDataUnavailable = !IsValidHealthValue(currentHealth)
                || !IsValidHealthValue(maxHealth)
                || !IsValidHealthValue(currentHume)
                || !IsValidHealthValue(maxHume)
                || maxHealth + maxHume <= 0d;

            if (isHealthDataUnavailable)
            {
                Log.Warn($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][SnapshotHealthUnavailable] Role={roleName}; Reason=InvalidMaximumOrCurrentValue");
            }

            return new ScpSnapshot(
                roleName,
                isAlive: true,
                currentHealth,
                maxHealth,
                currentHume,
                maxHume,
                isScp079: false,
                healthDataUnavailable: isHealthDataUnavailable);
        }
        catch (Exception exception)
        {
            Log.Warn($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][SnapshotHealthUnavailable] Role={roleName}; Reason={exception.GetType().Name}");
            return new ScpSnapshot(
                roleName,
                isAlive: true,
                healthDataUnavailable: true);
        }
    }

    private static Scp079TierRead ReadScp079Tier(Player player)
    {
        try
        {
            if (player.Role is Scp079Role scp079Role)
            {
                int value = Convert.ToInt32(scp079Role.Level);
                if (value < 0 || value > 5)
                {
                    Log.Warn($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][Scp079LevelUnavailable] Reason=OutOfRange; Value={value}");
                    return new Scp079TierRead(value, false);
                }

                return new Scp079TierRead(value, true);
            }
        }
        catch (Exception exception)
        {
            Log.Warn($"[EmergencyEvents][DLRC][{DateTime.UtcNow:O}][Scp079LevelUnavailable] Reason={exception.GetType().Name}");
        }

        return new Scp079TierRead(0, false);
    }

    private static bool IsFoundationRole(RoleTypeId role)
    {
        return role == RoleTypeId.FacilityGuard
            || role == RoleTypeId.NtfPrivate
            || role == RoleTypeId.NtfSergeant
            || role == RoleTypeId.NtfCaptain
            || role == RoleTypeId.NtfSpecialist;
    }

    private static bool IsHostileRole(RoleTypeId role)
    {
        return role == RoleTypeId.ChaosConscript
            || role == RoleTypeId.ChaosRifleman
            || role == RoleTypeId.ChaosMarauder
            || role == RoleTypeId.ChaosRepressor;
    }

    private static bool IsMainScpRole(string roleName)
    {
        return roleName.StartsWith("Scp", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(roleName, nameof(RoleTypeId.Scp0492), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(roleName, nameof(RoleTypeId.Scp079), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidHealthValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }

    private readonly struct Scp079TierRead
    {
        public Scp079TierRead(int value, bool isValid)
        {
            Value = value;
            IsValid = isValid;
        }

        public int Value { get; }

        public bool IsValid { get; }
    }
}
