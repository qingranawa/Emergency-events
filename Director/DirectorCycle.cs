using System;

namespace EmergencyEvents.Director;

/// <summary>
/// 一次 Director 周期的生命周期容器。
/// </summary>
public sealed class DirectorCycle
{
    public DirectorCycle(long cycleId, DateTime scheduledAt)
    {
        CycleId = cycleId;
        ScheduledAt = scheduledAt;
        State = EventLifecycleState.Scheduled;
    }

    public long CycleId { get; }

    public DateTime ScheduledAt { get; }

    public EventLifecycleState State { get; internal set; }

    public EventCandidate? SelectedSupport { get; internal set; }

    public EventCandidate? SelectedNonSupport { get; internal set; }

    public DateTime? ActualFirstSlotStartedAt { get; internal set; }

    public DateTime? SecondSlotDueAt { get; internal set; }
}
