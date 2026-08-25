using System;
using System.Collections.Generic;

namespace EmergencyEvents.Director;

/// <summary>
/// 最近事件历史的只读边界，Phase 1 不把 favorability 参与合法性判断。
/// </summary>
public interface IRecentEventHistory
{
    IReadOnlyList<RecentEventHistoryEntry> RecentEvents { get; }
}

public sealed class RecentEventHistoryEntry
{
    public RecentEventHistoryEntry(string eventId, DateTime completedAt, int? favorability)
    {
        EventId = eventId ?? string.Empty;
        CompletedAt = completedAt;
        Favorability = favorability;
    }

    public string EventId { get; }

    public DateTime CompletedAt { get; }

    public int? Favorability { get; }
}
