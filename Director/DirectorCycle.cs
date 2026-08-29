using System;
using System.Collections.Generic;

namespace EmergencyEvents.Director;

/// <summary>
/// 一次 Director 周期的生命周期容器。
/// </summary>
public sealed class DirectorCycle
{
    public DirectorCycle(long cycleId, DateTime scheduledAt, long roundId = 0L)
    {
        CycleId = cycleId;
        RoundId = roundId;
        ScheduledAt = scheduledAt;
        State = EventLifecycleState.Scheduled;
    }

    public long CycleId { get; }

    public long RoundId { get; }

    public DateTime ScheduledAt { get; }

    public EventLifecycleState State { get; internal set; }

    public EventCandidate? SelectedSupport { get; internal set; }

    public EventCandidate? SelectedNonSupport { get; internal set; }

    public IReadOnlyList<EventCandidate> O4SelectionCandidates { get; internal set; } = Array.Empty<EventCandidate>();

    public string? PendingO4SelectionSessionId { get; internal set; }

    public bool IsAwaitingO4Selection => !string.IsNullOrWhiteSpace(PendingO4SelectionSessionId);

    public DateTime? ActualFirstSlotStartedAt { get; internal set; }

    public DateTime? SecondSlotDueAt { get; internal set; }
}
