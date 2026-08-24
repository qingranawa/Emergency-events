using System;
using System.Collections.Generic;
using EmergencyEvents.RoundCore;
using MEC;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 一局 Primary Wave 截断与事实历史所需的最小状态。
/// </summary>
public sealed class ReinforcementState
{
    public ReinforcementState(long roundId, PopulationTier lockedPopulationTier)
    {
        RoundId = roundId;
        LockedPopulationTier = lockedPopulationTier;
        StartedAtUtc = DateTime.UtcNow;
    }

    public long RoundId { get; }

    public PopulationTier LockedPopulationTier { get; }

    public DateTime StartedAtUtc { get; }

    public bool IsActive { get; set; } = true;

    public int NextWaveSequence { get; set; }

    public bool HasPendingPrimaryWave { get; set; }

    public string PendingWaveName { get; set; } = string.Empty;

    public string PendingFaction { get; set; } = string.Empty;

    public DateTime PendingStartedAt { get; set; }

    public double? PendingFoundationTimerBeforeWave { get; set; }

    public double? PendingChaosTimerBeforeWave { get; set; }

    public MajorWaveHistory MajorWaveHistory { get; } = new MajorWaveHistory();

    public List<CoroutineHandle> ScheduledHandles { get; } = new List<CoroutineHandle>();

    public void ClearPendingPrimaryWave()
    {
        HasPendingPrimaryWave = false;
        PendingWaveName = string.Empty;
        PendingFaction = string.Empty;
        PendingStartedAt = default(DateTime);
        PendingFoundationTimerBeforeWave = null;
        PendingChaosTimerBeforeWave = null;
    }
}
