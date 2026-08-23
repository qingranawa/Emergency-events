using System;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 首波截止和固定窗口时间策略。
/// </summary>
public static class FirstWavePolicy
{
    public static bool ShouldSkip(
        bool isFirstWavePending,
        float elapsedSeconds,
        float deadlineSeconds,
        int eligibleObserverCount)
    {
        return isFirstWavePending
            && eligibleObserverCount <= 0
            && elapsedSeconds >= Math.Max(0f, deadlineSeconds);
    }

    public static float GetNextNormalWaveDueAfterSkip(
        float firstWindowSeconds,
        float normalIntervalSeconds)
    {
        return GetNextFixedWaveDue(firstWindowSeconds, normalIntervalSeconds);
    }

    public static float GetNextFixedWaveDue(
        float currentDueSeconds,
        float normalIntervalSeconds)
    {
        return Math.Max(0f, currentDueSeconds) + Math.Max(1f, normalIntervalSeconds);
    }
}
