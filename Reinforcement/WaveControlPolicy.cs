using System;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 插件代管正常大波时使用的无游戏依赖策略。
/// </summary>
public static class WaveControlPolicy
{
    public static bool IsDue(float elapsedSeconds, float dueSeconds)
    {
        return elapsedSeconds >= dueSeconds;
    }

    public static bool ShouldAllowRespawn(bool pluginWaveInProgress, bool isMiniWave)
    {
        return pluginWaveInProgress && !isMiniWave;
    }

    public static bool ShouldAllowTriggeredRespawn(
        bool pluginWaveRequestPending,
        bool pluginWaveInProgress,
        bool isMiniWave)
    {
        return (pluginWaveRequestPending || pluginWaveInProgress) && !isMiniWave;
    }

    public static bool ShouldAllowManualNormalWave(bool nativeWavesPaused, bool isMiniWave)
    {
        return nativeWavesPaused && !isMiniWave;
    }

    public static bool IsEligibleObserver(
        bool isConnected,
        bool isOverwatch,
        bool isSpectator,
        bool isUninitialized)
    {
        return isConnected
            && !isOverwatch
            && (isSpectator || isUninitialized);
    }
}
