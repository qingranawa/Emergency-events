using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Crisis;

/// <summary>
/// Crisis System 的已校验纯逻辑配置。
/// </summary>
public sealed class CrisisOptions
{
    public static CrisisOptions Default { get; } = new CrisisOptions(
        new CrisisTierThresholds(3),
        new CrisisTierThresholds(3),
        new CrisisTierThresholds(4),
        new CrisisTierThresholds(4),
        new CrisisTierThresholds(5),
        new CrisisTierThresholds(1),
        new CrisisTierThresholds(2),
        new CrisisTierThresholds(2),
        new CrisisTierThresholds(4),
        new CrisisTierThresholds(5));

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
        int endActivationSeconds = 300)
    {
        BioE = NormalizeBio(bioE, new CrisisTierThresholds(3));
        BioD = NormalizeBio(bioD, new CrisisTierThresholds(3));
        BioC = NormalizeBio(bioC, new CrisisTierThresholds(4));
        BioB = NormalizeBio(bioB, new CrisisTierThresholds(4));
        BioA = NormalizeBio(bioA, new CrisisTierThresholds(5));
        SecE = NormalizeSecurity(secE, new CrisisTierThresholds(1));
        SecD = NormalizeSecurity(secD, new CrisisTierThresholds(2));
        SecC = NormalizeSecurity(secC, new CrisisTierThresholds(2));
        SecB = NormalizeSecurity(secB, new CrisisTierThresholds(4));
        SecA = NormalizeSecurity(secA, new CrisisTierThresholds(5));
        ContainmentCheckpointSeconds = containmentCheckpointSeconds > 0 ? containmentCheckpointSeconds : 300;
        ContainmentEquivalentReduction = containmentEquivalentReduction > 0d && !double.IsNaN(containmentEquivalentReduction) && !double.IsInfinity(containmentEquivalentReduction)
            ? containmentEquivalentReduction
            : 1d;
        EndActivationSeconds = endActivationSeconds > 0 ? endActivationSeconds : 300;
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

    public int EndActivationSeconds { get; }

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
            && configured.ActivationThreshold > 0
            ? configured
            : fallback;
    }

    private static CrisisTierThresholds NormalizeSecurity(CrisisTierThresholds? configured, CrisisTierThresholds fallback)
    {
        return configured is not null
            && configured.ActivationThreshold >= 0
            ? configured
            : fallback;
    }
}

/// <summary>
/// 单个 A–E 档位的危机激活阈值。
/// </summary>
public sealed class CrisisTierThresholds
{
    public CrisisTierThresholds()
    {
    }

    public CrisisTierThresholds(int activationThreshold)
    {
        ActivationThreshold = Math.Max(0, activationThreshold);
    }

    public int ActivationThreshold { get; set; }
}
