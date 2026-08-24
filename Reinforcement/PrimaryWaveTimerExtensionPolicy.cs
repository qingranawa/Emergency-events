using System;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// Primary Wave Timer Extension 的纯逻辑策略。
/// </summary>
public static class PrimaryWaveTimerExtensionPolicy
{
    public const int DefaultSpawningFactionSeconds = 60;

    public const int DefaultOpposingFactionSeconds = 15;

    public const int MaxSeconds = 300;

    public const double VanillaResetTimePassedToleranceSeconds = 0.5d;

    public static int NormalizeConfiguredSeconds(int configuredSeconds, int fallbackSeconds)
    {
        int safeFallback = fallbackSeconds >= 0 && fallbackSeconds <= MaxSeconds
            ? fallbackSeconds
            : DefaultSpawningFactionSeconds;
        return configuredSeconds >= 0 && configuredSeconds <= MaxSeconds
            ? configuredSeconds
            : safeFallback;
    }

    public static bool IsPrimaryFaction(string? waveFaction)
    {
        return string.Equals(waveFaction, "NtfWave", StringComparison.Ordinal)
            || string.Equals(waveFaction, "ChaosWave", StringComparison.Ordinal);
    }

    public static bool IsActualSpawnedPlayer(bool isConnected, bool isAlive, bool matchesTargetTeam)
    {
        return isConnected && isAlive && matchesTargetTeam;
    }

    public static bool IsVanillaResetDetected(double timePassedSeconds)
    {
        return timePassedSeconds >= 0d
            && timePassedSeconds <= VanillaResetTimePassedToleranceSeconds;
    }

    public static bool TryGetExtensions(
        string? waveFaction,
        int spawningFactionSeconds,
        int opposingFactionSeconds,
        out int foundationSeconds,
        out int chaosSeconds)
    {
        foundationSeconds = 0;
        chaosSeconds = 0;
        if (string.Equals(waveFaction, "NtfWave", StringComparison.Ordinal))
        {
            foundationSeconds = spawningFactionSeconds;
            chaosSeconds = opposingFactionSeconds;
            return true;
        }

        if (string.Equals(waveFaction, "ChaosWave", StringComparison.Ordinal))
        {
            foundationSeconds = opposingFactionSeconds;
            chaosSeconds = spawningFactionSeconds;
            return true;
        }

        return false;
    }

    public static bool ShouldApply(
        string? waveFaction,
        bool isMiniWave,
        int actualSpawnedCount,
        bool completed,
        int spawningFactionSeconds,
        int opposingFactionSeconds,
        bool alreadyProcessed)
    {
        return IsPrimaryFaction(waveFaction)
            && !isMiniWave
            && actualSpawnedCount > 0
            && completed
            && (spawningFactionSeconds > 0 || opposingFactionSeconds > 0)
            && !alreadyProcessed;
    }

    public static double AddExtensionSeconds(double currentSeconds, int extensionSeconds)
    {
        return currentSeconds + Math.Max(0, extensionSeconds);
    }
}
