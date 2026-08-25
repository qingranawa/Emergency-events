using System;

namespace EmergencyEvents.Director;

/// <summary>
/// Module 05 的临时配置。生产事件默认关闭，避免 Phase 1 自动产生游戏副作用。
/// </summary>
public sealed class EventDirectorConfig
{
    public bool Enabled { get; set; }

    public int CadenceSeconds { get; set; }

    public int SecondSlotDelaySeconds { get; set; } = 60;

    public int MaxLogEntries { get; set; } = 256;

    public SecondSlotWithoutSuccessfulFirstEventPolicy SecondSlotFailurePolicy { get; set; } = SecondSlotWithoutSuccessfulFirstEventPolicy.Skip;

    public double FoundationWeight { get; set; } = 1d;

    public double ChaosWeight { get; set; } = 1d;

    public double GoiWeight { get; set; } = 1d;

    public EventDirectorConfig Normalize()
    {
        CadenceSeconds = Math.Max(0, CadenceSeconds);
        SecondSlotDelaySeconds = Math.Max(1, SecondSlotDelaySeconds);
        MaxLogEntries = Math.Max(1, MaxLogEntries);
        FoundationWeight = NormalizeWeight(FoundationWeight);
        ChaosWeight = NormalizeWeight(ChaosWeight);
        GoiWeight = NormalizeWeight(GoiWeight);
        return this;
    }

    private static double NormalizeWeight(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d ? value : 0d;
    }
}
