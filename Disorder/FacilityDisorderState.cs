using System;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 当前回合状态。只有 FacilityDisorderService 的 PERIODIC 入口可以写入。
/// </summary>
public sealed class FacilityDisorderState
{
    public bool IsActive { get; internal set; }

    public bool IsSuspended { get; internal set; }

    public bool IsInitialized { get; internal set; }

    public double CurrentFacilityDisorder { get; internal set; }

    public FacilityDisorderBand DisorderBand { get; internal set; } = FacilityDisorderBand.LOW;

    public DateTime? RoundStartedAt { get; internal set; }

    public long RoundId { get; internal set; }

    public DateTime? LastProcessedAt { get; internal set; }

    public DateTime? LastSettlementAt { get; internal set; }

    public FacilityDisorderSettlement? LastSettlement { get; internal set; }

    internal void Reset()
    {
        IsActive = false;
        IsSuspended = false;
        IsInitialized = false;
        CurrentFacilityDisorder = 0d;
        DisorderBand = FacilityDisorderBand.LOW;
        RoundStartedAt = null;
        RoundId = 0L;
        LastProcessedAt = null;
        LastSettlementAt = null;
        LastSettlement = null;
    }
}
