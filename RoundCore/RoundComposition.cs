using System;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 一局回合的开局编制。
/// </summary>
public sealed class RoundComposition
{
    internal RoundComposition(
        int population,
        PopulationTier tier,
        int scpCount,
        int securityCount,
        int chaosInfiltratorCount,
        int classDCount,
        int scientistCount)
    {
        if (population < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(population));
        }

        Population = population;
        Tier = tier;
        ScpCount = scpCount;
        SecurityCount = securityCount;
        ChaosInfiltratorCount = chaosInfiltratorCount;
        ClassDCount = classDCount;
        ScientistCount = scientistCount;
    }

    public int Population { get; }

    public PopulationTier Tier { get; }

    public int ScpCount { get; }

    public int SecurityCount { get; }

    public int ChaosInfiltratorCount { get; }

    public int ClassDCount { get; }

    public int ScientistCount { get; }

    public int Total =>
        ScpCount + SecurityCount + ChaosInfiltratorCount + ClassDCount + ScientistCount;

    public override string ToString()
    {
        return $"{Population}|{Tier}|{ScpCount}/{SecurityCount}/{ChaosInfiltratorCount}/{ClassDCount}/{ScientistCount}";
    }
}

/// <summary>
/// 人口编制解析结果。
/// </summary>
public sealed class CompositionResolution
{
    internal CompositionResolution(
        int population,
        PopulationTier tier,
        bool isSupported,
        bool wasClamped,
        RoundComposition? composition,
        string? unsupportedReason)
    {
        Population = population;
        Tier = tier;
        IsSupported = isSupported;
        WasClamped = wasClamped;
        Composition = composition;
        UnsupportedReason = unsupportedReason;
    }

    public int Population { get; }

    public PopulationTier Tier { get; }

    public bool IsSupported { get; }

    public bool WasClamped { get; }

    public RoundComposition? Composition { get; }

    public string? UnsupportedReason { get; }
}
