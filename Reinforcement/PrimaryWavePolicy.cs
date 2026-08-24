using System;
using System.Collections.Generic;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Reinforcement;

public enum MiniWaveCancellationBoundary
{
    SelectingRespawnTeam,
    RespawningTeam,
}

/// <summary>
/// Primary Wave 的人数截断配置。
/// </summary>
public sealed class PrimaryWaveCaps
{
    public int E { get; set; } = 6;

    public int D { get; set; } = 6;

    public int C { get; set; } = 8;

    public int B { get; set; } = 14;

    public int A { get; set; } = 18;

    public int GetCap(PopulationTier tier)
    {
        int cap = tier switch
        {
            PopulationTier.E => E,
            PopulationTier.D => D,
            PopulationTier.C => C,
            PopulationTier.B => B,
            PopulationTier.A => A,
            _ => 0,
        };
        return Math.Max(0, cap);
    }
}

/// <summary>
/// 只对原版 Primary Wave 做人数截断，不接管原版选择或阵营决定。
/// </summary>
public static class PrimaryWavePolicy
{
    public static int GetCappedMaximumRespawnAmount(
        int vanillaMaximumRespawnAmount,
        PopulationTier lockedPopulationTier,
        PrimaryWaveCaps caps)
    {
        if (caps is null)
        {
            throw new ArgumentNullException(nameof(caps));
        }

        return Math.Min(Math.Max(0, vanillaMaximumRespawnAmount), caps.GetCap(lockedPopulationTier));
    }

    public static bool ShouldCancelMiniWave(bool isMiniWave, bool disableMiniWaves)
    {
        return isMiniWave && disableMiniWaves;
    }

    public static bool ShouldCancelMiniWaveAtBoundary(
        bool isMiniWave,
        bool disableMiniWaves,
        MiniWaveCancellationBoundary boundary)
    {
        return boundary == MiniWaveCancellationBoundary.RespawningTeam
            && ShouldCancelMiniWave(isMiniWave, disableMiniWaves);
    }

    public static IReadOnlyList<T> TruncateVanillaSelection<T>(
        IReadOnlyList<T>? vanillaSelection,
        int maximumRespawnAmount)
    {
        if (vanillaSelection is null || vanillaSelection.Count == 0 || maximumRespawnAmount <= 0)
        {
            return Array.Empty<T>();
        }

        int retainedCount = Math.Min(vanillaSelection.Count, maximumRespawnAmount);
        List<T> retained = new List<T>(retainedCount);
        for (int index = 0; index < retainedCount; index++)
        {
            retained.Add(vanillaSelection[index]);
        }

        return retained.AsReadOnly();
    }
}
