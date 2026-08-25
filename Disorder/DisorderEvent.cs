using System;

namespace EmergencyEvents.Disorder;

/// <summary>
/// 一条带唯一来源标识的客观回合事实。
/// </summary>
public sealed class DisorderEvent
{
    public DisorderEvent(
        string eventId,
        DateTime timestamp,
        DisorderEventCategory category,
        double delta,
        string? description = null,
        bool isDryRun = false,
        bool isRepresentedByCurrentStock = false)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("事件必须有唯一来源标识。", nameof(eventId));
        }

        if (double.IsNaN(delta) || double.IsInfinity(delta))
        {
            throw new ArgumentOutOfRangeException(nameof(delta), "事件变化量必须是有限数值。 ");
        }

        EventId = eventId.Trim();
        Timestamp = timestamp;
        Category = category;
        Delta = delta;
        Description = description?.Trim() ?? string.Empty;
        IsDryRun = isDryRun;
        IsRepresentedByCurrentStock = isRepresentedByCurrentStock;
    }

    public string EventId { get; }

    public DateTime Timestamp { get; }

    public DisorderEventCategory Category { get; }

    public double Delta { get; }

    public string Description { get; }

    public bool IsDryRun { get; }

    public bool IsRepresentedByCurrentStock { get; }
}
