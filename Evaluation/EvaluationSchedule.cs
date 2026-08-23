using System;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 提供与游戏线程无关的评估调度计算。
/// </summary>
public static class EvaluationSchedule
{
    public static double GetInitialDelaySeconds(TimeSpan elapsed, int startTimeSeconds)
    {
        double normalizedElapsed = Math.Max(0d, elapsed.TotalSeconds);
        double normalizedStart = Math.Max(1, startTimeSeconds);
        return Math.Max(0d, normalizedStart - normalizedElapsed);
    }

    public static double GetIntervalSeconds(int intervalSeconds)
    {
        return Math.Max(1, intervalSeconds);
    }

    public static double GetNextDelaySeconds(
        TimeSpan elapsed,
        int startTimeSeconds,
        int intervalSeconds)
    {
        double normalizedElapsed = Math.Max(0d, elapsed.TotalSeconds);
        double normalizedStart = Math.Max(1, startTimeSeconds);
        double normalizedInterval = Math.Max(1, intervalSeconds);
        if (normalizedElapsed < normalizedStart)
        {
            return normalizedStart - normalizedElapsed;
        }

        double elapsedSinceStart = normalizedElapsed - normalizedStart;
        double completedIntervals = Math.Floor(elapsedSinceStart / normalizedInterval);
        double nextTarget = normalizedStart
            + (completedIntervals + 1d) * normalizedInterval;
        return Math.Max(0d, nextTarget - normalizedElapsed);
    }

    public static bool IsDue(TimeSpan elapsed, int startTimeSeconds)
    {
        return Math.Max(0d, elapsed.TotalSeconds) >= Math.Max(1, startTimeSeconds);
    }
}
