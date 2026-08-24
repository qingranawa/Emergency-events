using System;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 为 RA Dry Run 构造独立快照，不触及服务器或真实评估快照。
/// </summary>
public static class CrisisDiagnosticSnapshotFactory
{
    public static RoundSnapshot WithZombieCount(RoundSnapshot source, int zombieCount)
    {
        return Clone(source, scp0492Count: Math.Max(0, zombieCount));
    }

    public static RoundSnapshot WithScp079Tier(RoundSnapshot source, int tier)
    {
        return Clone(
            source,
            scp079Present: true,
            scp079Tier: Math.Min(Math.Max(tier, 0), 5),
            scp079TierIsValid: true);
    }

    public static RoundSnapshot WithSecurityFacts(
        RoundSnapshot source,
        int foundationCombatants,
        bool hostileThreatPresent)
    {
        return Clone(
            source,
            foundationCombatants: Math.Max(0, foundationCombatants),
            chaosCombatants: hostileThreatPresent ? Math.Max(1, source.ChaosCombatants) : 0,
            otherHostileCombatants: 0,
            mainScpAlive: hostileThreatPresent ? source.MainScpAlive : 0,
            hostileThirdPartyCombatants: 0);
    }

    public static RoundSnapshot WithWarheadState(RoundSnapshot source, string state)
    {
        string normalized = state?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalized switch
        {
            "locked" => Clone(source, warheadUnlocked: false, warheadActive: false, warheadDetonated: false),
            "unlocked" => Clone(source, warheadUnlocked: true, warheadActive: false, warheadDetonated: false),
            "active" => Clone(source, warheadUnlocked: true, warheadActive: true, warheadDetonated: false),
            "detonated" => Clone(
                source,
                warheadUnlocked: true,
                warheadActive: false,
                warheadDetonated: true,
                warheadDetonatedAt: source.Timestamp),
            _ => throw new ArgumentOutOfRangeException(nameof(state), "不支持的核弹测试状态。"),
        };
    }

    public static RoundSnapshot WithEndStalemate(RoundSnapshot source)
    {
        return Clone(
            source,
            warheadUnlocked: true,
            warheadActive: false,
            warheadDetonated: true,
            warheadDetonatedAt: source.Timestamp,
            surfaceFoundationCombatants: Math.Max(1, source.SurfaceFoundationCombatants),
            surfaceChaosCombatants: Math.Max(1, source.SurfaceChaosCombatants));
    }

    private static RoundSnapshot Clone(
        RoundSnapshot source,
        int? scp0492Count = null,
        bool? scp079Present = null,
        int? scp079Tier = null,
        bool? scp079TierIsValid = null,
        int? foundationCombatants = null,
        int? chaosCombatants = null,
        int? otherHostileCombatants = null,
        int? mainScpAlive = null,
        int? hostileThirdPartyCombatants = null,
        bool? warheadUnlocked = null,
        bool? warheadActive = null,
        bool? warheadDetonated = null,
        DateTime? warheadDetonatedAt = null,
        int? surfaceFoundationCombatants = null,
        int? surfaceChaosCombatants = null)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new RoundSnapshot(
            source.RoundId,
            source.Timestamp,
            source.RoundElapsedTime,
            source.PopulationTier,
            source.RoundStartPopulation,
            source.StartingScpCount,
            source.CurrentOnlinePlayers,
            foundationCombatants ?? source.FoundationCombatants,
            chaosCombatants ?? source.ChaosCombatants,
            otherHostileCombatants ?? source.OtherHostileCombatants,
            source.ClassDAlive,
            source.ScientistsAlive,
            source.EligibleSpectators,
            source.OverwatchCount,
            mainScpAlive ?? source.MainScpAlive,
            source.ScpStates,
            scp0492Count ?? source.Scp0492Count,
            scp079Present ?? source.Scp079Present,
            scp079Tier ?? source.Scp079Tier,
            warheadUnlocked ?? source.WarheadUnlocked,
            warheadActive ?? source.WarheadActive,
            warheadDetonated ?? source.WarheadDetonated,
            source.WarheadCancellationCount,
            source.MajorWaveHistory,
            source.RecentFoundationDeaths120s,
            source.RecentHostileDeaths120s,
            source.RecentMainScpDeaths120s,
            source.ActivePlayerIds,
            source.HostileThirdPartyActive,
            hostileThirdPartyCombatants ?? source.HostileThirdPartyCombatants,
            surfaceFoundationCombatants ?? source.SurfaceFoundationCombatants,
            surfaceChaosCombatants ?? source.SurfaceChaosCombatants,
            source.SurfaceMainScp,
            source.SurfaceOtherHostiles,
            scp079TierIsValid ?? source.Scp079TierIsValid,
            warheadDetonatedAt: warheadDetonatedAt ?? source.WarheadDetonatedAt);
    }
}
