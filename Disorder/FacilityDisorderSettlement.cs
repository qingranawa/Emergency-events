using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyEvents.Disorder;

/// <summary>
/// 一次 PERIODIC FDI 结算的只读结果。
/// </summary>
public sealed class FacilityDisorderSettlement
{
    public FacilityDisorderSettlement(
        DateTime windowStart,
        DateTime windowEnd,
        double previousValue,
        double delta,
        double currentValue,
        double currentStockAdjustment,
        double recentTransientDelta,
        IReadOnlyList<DisorderEvent> processedEvents,
        double orderRecoveryDelta = 0d,
        string recoveryResult = "NOT_EVALUATED")
    {
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        PreviousValue = previousValue;
        Delta = delta;
        CurrentValue = currentValue;
        CurrentStockAdjustment = currentStockAdjustment;
        RecentTransientDelta = recentTransientDelta;
        OrderRecoveryDelta = orderRecoveryDelta;
        RecoveryResult = recoveryResult ?? string.Empty;
        ProcessedEvents = new ReadOnlyCollection<DisorderEvent>(new List<DisorderEvent>(processedEvents));
    }

    public DateTime WindowStart { get; }

    public DateTime WindowEnd { get; }

    public double PreviousValue { get; }

    public double Delta { get; }

    public double CurrentValue { get; }

    public double CurrentStockAdjustment { get; }

    public double RecentTransientDelta { get; }

    public double OrderRecoveryDelta { get; }

    public string RecoveryResult { get; }

    public IReadOnlyList<DisorderEvent> ProcessedEvents { get; }
}
