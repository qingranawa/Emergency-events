using System;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// 将当前真实存量转换为一次性的 InitialFDI 调整。
/// </summary>
public static class FacilityDisorderStockCalculator
{
    public static double Calculate(FacilityDisorderStockSnapshot stock, FacilityDisorderConfig config)
    {
        if (stock is null)
        {
            throw new ArgumentNullException(nameof(stock));
        }

        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        double adjustment = stock.MtfCount * config.CurrentMtfPerCombatant
            + stock.ChaosCount * config.CurrentChaosPerCombatant
            + stock.ZombieCount * config.CurrentZombiePerUnit
            + stock.CurrentHostileForce * config.CurrentHostilePerCombatant;
        bool sysExpresses079 = stock.CrisisAssessment?.GetSeverity(CrisisTag.SYS) > CrisisSeverity.Inactive;
        if (stock.Scp079Present && !sysExpresses079)
        {
            adjustment += stock.Scp079Tier * config.CurrentScp079Tier;
        }

        if (stock.CrisisAssessment is not null)
        {
            foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
            {
                adjustment += (int)stock.CrisisAssessment.GetSeverity(tag) * config.CurrentCrisisPerLevel;
            }
        }

        bool warheadExpressedByCrisis = stock.CrisisAssessment?.IsActive(CrisisTag.WAR) == true
            || stock.CrisisAssessment?.IsActive(CrisisTag.END) == true;
        if (!warheadExpressedByCrisis)
        {
            if (stock.WarheadUnlocked)
            {
                adjustment += config.CurrentWarheadUnlocked;
            }

            if (stock.WarheadActive)
            {
                adjustment += config.CurrentWarheadCountdownActive;
            }

            if (stock.WarheadDetonated)
            {
                adjustment += config.CurrentWarheadDetonated;
            }
        }

        return adjustment;
    }
}
