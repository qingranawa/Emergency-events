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
}
