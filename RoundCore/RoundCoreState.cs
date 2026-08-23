using System;
using System.Collections.Generic;
using System.Linq;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 保存一局 Round Core 在回合开始时锁定的状态。
/// </summary>
public sealed class RoundCoreState
{
    internal RoundCoreState(
        long roundId,
        int startPopulation,
        CompositionResolution resolution,
        IEnumerable<int> roundStartPlayerIds,
        DateTime capturedAtUtc)
    {
        RoundId = roundId;
        StartPopulation = startPopulation;
        Resolution = resolution;
        RoundStartPlayerIds = roundStartPlayerIds.Distinct().ToArray();
        CapturedAtUtc = capturedAtUtc;
    }

    public long RoundId { get; }

    public int StartPopulation { get; }

    public CompositionResolution Resolution { get; }

    public IReadOnlyList<int> RoundStartPlayerIds { get; }

    public DateTime CapturedAtUtc { get; }

    public bool IsInitialized { get; internal set; }

    public bool IsSkipped { get; internal set; }

    public bool FoundationUsesElevatorA { get; internal set; }

    public int AssignedPlayerCount { get; internal set; }
}
