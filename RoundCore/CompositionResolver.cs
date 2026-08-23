using System;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 将回合开始人口解析为锁定等级和开局编制。
/// </summary>
public static class CompositionResolver
{
    public const int MinimumSupportedPopulation = 16;

    public const int MaximumSupportedPopulation = 45;

    public static CompositionResolution GetComposition(int population)
    {
        PopulationTier tier = ResolveTier(population);
        bool wasClamped = population < MinimumSupportedPopulation || population > MaximumSupportedPopulation;

        if (wasClamped)
        {
            return new CompositionResolution(
                population,
                tier,
                isSupported: false,
                wasClamped: true,
                composition: null,
                unsupportedReason: "UnsupportedPopulation");
        }

        if (!CompositionTable.TryGet(population, out RoundComposition? composition) || composition is null)
        {
            throw new InvalidOperationException($"Round Core composition row is missing for population {population}.");
        }

        ValidateRow(composition);

        return new CompositionResolution(
            population,
            tier,
            isSupported: true,
            wasClamped: false,
            composition,
            unsupportedReason: null);
    }

    private static PopulationTier ResolveTier(int population)
    {
        if (population <= 19)
        {
            return PopulationTier.E;
        }

        if (population <= 25)
        {
            return PopulationTier.D;
        }

        if (population <= 31)
        {
            return PopulationTier.C;
        }

        if (population <= 37)
        {
            return PopulationTier.B;
        }

        return PopulationTier.A;
    }

    private static void ValidateRow(RoundComposition composition)
    {
        if (composition.Total != composition.Population)
            throw new InvalidOperationException($"Round Core composition row {composition.Population} totals {composition.Total}.");

        if (composition.SecurityCount != composition.ChaosInfiltratorCount)
            throw new InvalidOperationException($"Round Core composition row {composition.Population} breaks the Security/Chaos mirror.");
    }
}
