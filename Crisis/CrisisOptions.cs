using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Crisis;

/// <summary>
/// Crisis System 的已校验纯逻辑配置。
/// </summary>
public sealed class CrisisOptions
{
    public static CrisisOptions Default { get; } = new CrisisOptions(
        new CrisisTierThresholds(3, 5, 7),
        new CrisisTierThresholds(3, 6, 8),
        new CrisisTierThresholds(4, 7, 10),
        new CrisisTierThresholds(4, 8, 12),
        new CrisisTierThresholds(5, 9, 14),
        new CrisisTierThresholds(1, 1, 0),
        new CrisisTierThresholds(2, 1, 0),
        new CrisisTierThresholds(2, 1, 0),
        new CrisisTierThresholds(4, 2, 0),
        new CrisisTierThresholds(5, 2, 0));

    public CrisisOptions(
        CrisisTierThresholds? bioE = null,
        CrisisTierThresholds? bioD = null,
        CrisisTierThresholds? bioC = null,
        CrisisTierThresholds? bioB = null,
        CrisisTierThresholds? bioA = null,
        CrisisTierThresholds? secE = null,
        CrisisTierThresholds? secD = null,
        CrisisTierThresholds? secC = null,
        CrisisTierThresholds? secB = null,
        CrisisTierThresholds? secA = null,
        int containmentCheckpointSeconds = 300,
        double containmentEquivalentReduction = 1d,
        int endLevel3Seconds = 300,
        int endLevel4Seconds = 480,
        int endLevel5Seconds = 720)
    {
        BioE = NormalizeBio(bioE, new CrisisTierThresholds(3, 5, 7));
        BioD = NormalizeBio(bioD, new CrisisTierThresholds(3, 6, 8));
        BioC = NormalizeBio(bioC, new CrisisTierThresholds(4, 7, 10));
        BioB = NormalizeBio(bioB, new CrisisTierThresholds(4, 8, 12));
        BioA = NormalizeBio(bioA, new CrisisTierThresholds(5, 9, 14));
        SecE = NormalizeSecurity(secE, new CrisisTierThresholds(1, 1, 0));
        SecD = NormalizeSecurity(secD, new CrisisTierThresholds(2, 1, 0));
        SecC = NormalizeSecurity(secC, new CrisisTierThresholds(2, 1, 0));
        SecB = NormalizeSecurity(secB, new CrisisTierThresholds(4, 2, 0));
        SecA = NormalizeSecurity(secA, new CrisisTierThresholds(5, 2, 0));
        ContainmentCheckpointSeconds = containmentCheckpointSeconds > 0 ? containmentCheckpointSeconds : 300;
        ContainmentEquivalentReduction = containmentEquivalentReduction > 0d && !double.IsNaN(containmentEquivalentReduction) && !double.IsInfinity(containmentEquivalentReduction)
            ? containmentEquivalentReduction
            : 1d;
        bool hasValidEndDurations = endLevel3Seconds > 0
            && endLevel4Seconds >= endLevel3Seconds
            && endLevel5Seconds >= endLevel4Seconds;
        EndLevel3Seconds = hasValidEndDurations ? endLevel3Seconds : 300;
        EndLevel4Seconds = hasValidEndDurations ? endLevel4Seconds : 480;
        EndLevel5Seconds = hasValidEndDurations ? endLevel5Seconds : 720;
    }

    public CrisisTierThresholds BioE { get; }

    public CrisisTierThresholds BioD { get; }

    public CrisisTierThresholds BioC { get; }

    public CrisisTierThresholds BioB { get; }

    public CrisisTierThresholds BioA { get; }

    public CrisisTierThresholds SecE { get; }

    public CrisisTierThresholds SecD { get; }

    public CrisisTierThresholds SecC { get; }

    public CrisisTierThresholds SecB { get; }

    public CrisisTierThresholds SecA { get; }

    public int ContainmentCheckpointSeconds { get; }

    public double ContainmentEquivalentReduction { get; }

    public int EndLevel3Seconds { get; }

    public int EndLevel4Seconds { get; }

    public int EndLevel5Seconds { get; }

    public CrisisTierThresholds GetBioThresholds(PopulationTier tier)
    {
        return tier switch
        {
            PopulationTier.A => BioA,
            PopulationTier.B => BioB,
            PopulationTier.C => BioC,
            PopulationTier.D => BioD,
            _ => BioE,
        };
    }

    public CrisisTierThresholds GetSecurityThresholds(PopulationTier tier)
    {
        return tier switch
        {
            PopulationTier.A => SecA,
            PopulationTier.B => SecB,
            PopulationTier.C => SecC,
            PopulationTier.D => SecD,
            _ => SecE,
        };
    }

    private static CrisisTierThresholds NormalizeBio(CrisisTierThresholds? configured, CrisisTierThresholds fallback)
    {
        return configured is not null
            && configured.Level3 > 0
            && configured.Level4 >= configured.Level3
            && configured.Level5 >= configured.Level4
            ? configured
            : fallback;
    }

    private static CrisisTierThresholds NormalizeSecurity(CrisisTierThresholds? configured, CrisisTierThresholds fallback)
    {
        return configured is not null
            && configured.Level3 >= configured.Level4
            && configured.Level4 >= configured.Level5
            && configured.Level5 >= 0
            ? configured
            : fallback;
    }
}

/// <summary>
/// 单个 A–E 档位的 L3、L4、L5 数量阈值。
/// </summary>
public sealed class CrisisTierThresholds
{
    public CrisisTierThresholds()
    {
    }

    public CrisisTierThresholds(int level3, int level4, int level5)
    {
        Level3 = Math.Max(0, level3);
        Level4 = Math.Max(0, level4);
        Level5 = Math.Max(0, level5);
    }

    public int Level3 { get; set; }

    public int Level4 { get; set; }

    public int Level5 { get; set; }
}
