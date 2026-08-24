using System;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 仅保存 CON 与 END 所需的跨评估回合状态。
/// </summary>
public sealed class CrisisState
{
    public DateTime? SecondMajorWaveCompletedAt { get; private set; }

    public double? ContainmentBaselineEquivalent { get; private set; }

    public DateTime? NextContainmentCheckpointAt { get; private set; }

    public int ContainmentFailureStreak { get; private set; }

    public DateTime? WarheadDetonatedAt { get; private set; }

    public DateTime? SurfaceStalemateStartedAt { get; private set; }

    public void StartContainmentTracking(DateTime secondWaveCompletedAt, double baselineEquivalent, DateTime nextCheckpointAt)
    {
        SecondMajorWaveCompletedAt = secondWaveCompletedAt;
        ContainmentBaselineEquivalent = baselineEquivalent;
        NextContainmentCheckpointAt = nextCheckpointAt;
        ContainmentFailureStreak = 0;
    }

    public void RecordContainmentCheckpoint(double baselineEquivalent, DateTime nextCheckpointAt, bool wasContained)
    {
        ContainmentBaselineEquivalent = baselineEquivalent;
        NextContainmentCheckpointAt = nextCheckpointAt;
        ContainmentFailureStreak = wasContained ? 0 : ContainmentFailureStreak + 1;
    }

    public void ResetContainment()
    {
        SecondMajorWaveCompletedAt = null;
        ContainmentBaselineEquivalent = null;
        NextContainmentCheckpointAt = null;
        ContainmentFailureStreak = 0;
    }

    /// <summary>
    /// 仅供诊断层把已存在的检查点推进到当前快照时间。
    /// </summary>
    internal bool TryForceContainmentCheckpoint(DateTime checkpointAt)
    {
        if (!ContainmentBaselineEquivalent.HasValue || !NextContainmentCheckpointAt.HasValue)
        {
            return false;
        }

        NextContainmentCheckpointAt = checkpointAt;
        return true;
    }

    public void ObserveWarheadDetonation(DateTime timestamp)
    {
        WarheadDetonatedAt ??= timestamp;
    }

    public void StartSurfaceStalemate(DateTime timestamp)
    {
        SurfaceStalemateStartedAt ??= timestamp;
    }

    public void ResetEndgame()
    {
        WarheadDetonatedAt = null;
        SurfaceStalemateStartedAt = null;
    }

    public void ResetSurfaceStalemate()
    {
        SurfaceStalemateStartedAt = null;
    }

    public void Reset()
    {
        ResetContainment();
        ResetEndgame();
    }

    /// <summary>
    /// 为 RA 诊断创建独立副本，避免 Dry Run 改写正式回合状态。
    /// </summary>
    public CrisisState Clone()
    {
        return new CrisisState
        {
            SecondMajorWaveCompletedAt = SecondMajorWaveCompletedAt,
            ContainmentBaselineEquivalent = ContainmentBaselineEquivalent,
            NextContainmentCheckpointAt = NextContainmentCheckpointAt,
            ContainmentFailureStreak = ContainmentFailureStreak,
            WarheadDetonatedAt = WarheadDetonatedAt,
            SurfaceStalemateStartedAt = SurfaceStalemateStartedAt,
        };
    }
}
