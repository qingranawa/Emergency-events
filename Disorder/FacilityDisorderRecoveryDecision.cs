using System;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 被动秩序恢复的一次纯逻辑判定。
/// </summary>
public sealed class FacilityDisorderRecoveryDecision
{
    public FacilityDisorderRecoveryDecision(
        bool eligible,
        double delta,
        string result,
        double quietElapsed,
        string gateReason)
    {
        Eligible = eligible;
        Delta = delta;
        Result = result;
        QuietElapsed = quietElapsed;
        GateReason = gateReason;
    }

    public bool Eligible { get; }

    public double Delta { get; }

    public string Result { get; }

    public double QuietElapsed { get; }

    public string GateReason { get; }
}

public static class FacilityDisorderRecoveryPolicy
{
    public static FacilityDisorderRecoveryDecision Evaluate(
        DateTime timestamp,
        FacilityDisorderState state,
        FacilityDisorderStockSnapshot stock,
        FacilityDisorderConfig config,
        bool hasOrdinaryDelta)
    {
        if (!config.OrderRecoveryEnabled)
        {
            return Denied("DISABLED");
        }

        if (stock.IsFacilityDestroyed)
        {
            return Denied("BLOCKED_FACILITY_DESTROYED");
        }

        if (stock.CrisisAssessment is not null && stock.CrisisAssessment.ActiveTags.Count > 0)
        {
            return Denied("BLOCKED_ACTIVE_CRISIS");
        }

        if (HasStrongHostilePressure(stock))
        {
            return Denied("BLOCKED_HOSTILE_STATE");
        }

        if (hasOrdinaryDelta)
        {
            return Denied("ORDINARY_DELTA_PRESENT");
        }

        DateTime quietStart = state.QuietWindowStart ?? timestamp;
        double quietElapsed = Math.Max(0d, (timestamp - quietStart).TotalSeconds);
        if (quietElapsed < config.OrderRecoveryQuietWindowSeconds)
        {
            return new FacilityDisorderRecoveryDecision(false, 0d, "NOT_QUIET_LONG_ENOUGH", quietElapsed, "QUIET_WINDOW");
        }

        double delta = state.DisorderBand switch
        {
            FacilityDisorderBand.HIGH => config.OrderRecoveryHighDelta,
            FacilityDisorderBand.MEDIUM => config.OrderRecoveryMediumDelta,
            _ => config.OrderRecoveryLowDelta,
        };
        return new FacilityDisorderRecoveryDecision(delta < 0d, delta, delta < 0d ? "APPLIED" : "LOW_BAND_NO_RECOVERY", quietElapsed, "PASSED");
    }

    private static bool HasStrongHostilePressure(FacilityDisorderStockSnapshot stock)
    {
        bool chaosHasClearAdvantage = stock.ChaosCount >= 4 && stock.ChaosCount > stock.MtfCount;
        bool hostileForceHasClearAdvantage = stock.CurrentHostileForce >= 8
            && stock.CurrentHostileForce > stock.MtfCount * 2;
        return chaosHasClearAdvantage || hostileForceHasClearAdvantage;
    }

    private static FacilityDisorderRecoveryDecision Denied(string result)
    {
        return new FacilityDisorderRecoveryDecision(false, 0d, result, 0d, result);
    }
}
