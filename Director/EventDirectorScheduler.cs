using System;

namespace EmergencyEvents.Director;

/// <summary>
/// 独立的第二槽位调度器，不触碰 Module 03 的 30 秒评估周期。
/// </summary>
public sealed class EventDirectorScheduler
{
    private readonly int delaySeconds;
    private long? scheduledCycleId;

    public EventDirectorScheduler(EventDirectorConfig config)
    {
        EventDirectorConfig normalized = (config ?? new EventDirectorConfig()).Normalize();
        delaySeconds = normalized.SecondSlotDelaySeconds;
    }

    public DateTime? SecondSlotDueAt { get; private set; }

    public bool ScheduleSecondSlot(long cycleId, DateTime actualFirstStart)
    {
        if (scheduledCycleId.HasValue)
        {
            return false;
        }

        scheduledCycleId = cycleId;
        SecondSlotDueAt = actualFirstStart.AddSeconds(delaySeconds);
        return true;
    }

    public bool TryConsumeDueSlot(DateTime now, long? expectedCycleId = null)
    {
        if (!SecondSlotDueAt.HasValue
            || now < SecondSlotDueAt.Value
            || expectedCycleId.HasValue && scheduledCycleId != expectedCycleId.Value)
        {
            return false;
        }

        SecondSlotDueAt = null;
        scheduledCycleId = null;
        return true;
    }

    public void Cleanup()
    {
        SecondSlotDueAt = null;
        scheduledCycleId = null;
    }
}
