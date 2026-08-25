using System;

namespace EmergencyEvents.Director;

/// <summary>
/// Director 诊断日志，不包含游戏生成副作用。
/// </summary>
public sealed class DirectorLogEntry
{
    public DirectorLogEntry(
        DateTime timestamp,
        long? cycleId,
        string eventId,
        EventLifecycleState state,
        bool isLegal,
        string reason)
    {
        Timestamp = timestamp;
        CycleId = cycleId;
        EventId = eventId ?? string.Empty;
        State = state;
        IsLegal = isLegal;
        Reason = reason ?? string.Empty;
    }

    public DateTime Timestamp { get; }

    public long? CycleId { get; }

    public string EventId { get; }

    public EventLifecycleState State { get; }

    public bool IsLegal { get; }

    public string Reason { get; }
}
