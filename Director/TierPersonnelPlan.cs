using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// 每个人口档位的事件目标人数与最低人数。
/// </summary>
public sealed class TierPersonnelPlan
{
    public TierPersonnelPlan(int e, int d, int c, int b, int a)
    {
        Values = new[] { Math.Max(0, e), Math.Max(0, d), Math.Max(0, c), Math.Max(0, b), Math.Max(0, a) };
    }

    private int[] Values { get; }

    public static TierPersonnelPlan Uniform(int value)
    {
        int normalizedValue = Math.Max(0, value);
        return new TierPersonnelPlan(
            normalizedValue,
            normalizedValue,
            normalizedValue,
            normalizedValue,
            normalizedValue);
    }

    public int Get(PopulationTier tier)
    {
        return Values[GetIndex(tier)];
    }

    private static int GetIndex(PopulationTier tier)
    {
        return tier switch
        {
            PopulationTier.E => 0,
            PopulationTier.D => 1,
            PopulationTier.C => 2,
            PopulationTier.B => 3,
            PopulationTier.A => 4,
            _ => 0,
        };
    }
}
