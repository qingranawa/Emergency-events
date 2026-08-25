using System;

namespace EmergencyEvents.Director;

/// <summary>
/// 事件成本边界接口，Phase 1 不定义具体成本公式。
/// </summary>
public interface IEventCostBoundary
{
    void Record(EventCandidate candidate, string cycleId, DateTime committedAt);
}

/// <summary>
/// 默认无副作用成本记录器。
/// </summary>
public sealed class NoOpEventCostBoundary : IEventCostBoundary
{
    public void Record(EventCandidate candidate, string cycleId, DateTime committedAt)
    {
    }
}
