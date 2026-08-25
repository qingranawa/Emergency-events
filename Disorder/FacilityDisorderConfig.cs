using System;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 配置。默认权重是明确标记的临时平衡值，后续真人数据可独立调整。
/// </summary>
public sealed class FacilityDisorderConfig
{
    public bool Enabled { get; set; } = true;

    public bool IsProvisionalBalance { get; set; } = true;

    public int MinimumPlayers { get; set; } = 16;

    public double InitialBase { get; set; } = 50d;

    public int InitialLookbackSeconds { get; set; } = 120;

    public int SettlementHistoryCapacity { get; set; } = 256;

    public int EventHistoryCapacity { get; set; } = 512;

    public double LowMinimum { get; set; } = 0d;

    public double LowMaximum { get; set; } = 29d;

    public double MediumMinimum { get; set; } = 30d;

    public double MediumMaximum { get; set; } = 59d;

    public double HighMinimum { get; set; } = 60d;

    public double HighMaximum { get; set; } = 100d;

    public double FoundationKilledByScp { get; set; } = 3d;

    public double FoundationKilledByChaos { get; set; } = 2d;

    public double FoundationKilledByHostileGoi { get; set; } = 2d;

    public double FoundationKillsHostileHuman { get; set; } = -2d;

    public double ScpEliminated { get; set; } = -3d;

    public double HostileFactionEliminated { get; set; } = -2d;

    public double MtfGainPerCombatant { get; set; } = -1d;

    public double MtfLossPerCombatant { get; set; } = 2d;

    public double ChaosGainPerCombatant { get; set; } = 1d;

    public double ChaosLossPerCombatant { get; set; } = -1d;

    public double GoiGainPerCombatant { get; set; } = 1d;

    public double GoiLossPerCombatant { get; set; } = -1d;

    public double GainPerZombie { get; set; } = 1d;

    public double LossPerZombie { get; set; } = -1d;

    public double Scp079TierIncreasePerLevel { get; set; } = 2d;

    public double Scp079TierDecreasePerLevel { get; set; } = -2d;

    public double Scp079Removed { get; set; } = -3d;

    public double CrisisActivatedL3 { get; set; } = 3d;

    public double CrisisEscalatedToL4 { get; set; } = 4d;

    public double CrisisEscalatedToL5 { get; set; } = 5d;

    public double CrisisDowngraded { get; set; } = -3d;

    public double CrisisResolved { get; set; } = -4d;

    public double WarheadUnlocked { get; set; } = 2d;

    public double WarheadCountdownActive { get; set; } = 3d;

    public double WarheadCancelled { get; set; } = -2d;

    public double WarheadDetonated { get; set; } = 5d;

    public double FactionAdvantageChanged { get; set; } = 0d;

    public double CurrentMtfPerCombatant { get; set; } = -1d;

    public double CurrentChaosPerCombatant { get; set; } = 1d;

    public double CurrentZombiePerUnit { get; set; } = 1d;

    public double CurrentHostilePerCombatant { get; set; } = 1d;

    public double CurrentScp079Tier { get; set; } = 2d;

    public double CurrentCrisisPerLevel { get; set; } = 1d;

    public double CurrentWarheadUnlocked { get; set; } = 2d;

    public double CurrentWarheadCountdownActive { get; set; } = 3d;

    public double CurrentWarheadDetonated { get; set; } = 0d;

    public void Validate()
    {
        if (MinimumPlayers < 1)
        {
            MinimumPlayers = 16;
        }

        if (InitialLookbackSeconds < 1)
        {
            InitialLookbackSeconds = 120;
        }

        if (SettlementHistoryCapacity < 1)
        {
            SettlementHistoryCapacity = 256;
        }

        if (EventHistoryCapacity < 1)
        {
            EventHistoryCapacity = 512;
        }

        InitialBase = Normalize(InitialBase, 50d);
        LowMinimum = Normalize(LowMinimum, 0d);
        LowMaximum = Normalize(LowMaximum, 29d);
        MediumMinimum = Normalize(MediumMinimum, 30d);
        MediumMaximum = Normalize(MediumMaximum, 59d);
        HighMinimum = Normalize(HighMinimum, 60d);
        HighMaximum = Normalize(HighMaximum, 100d);
        CurrentMtfPerCombatant = Normalize(CurrentMtfPerCombatant, -1d);
        CurrentChaosPerCombatant = Normalize(CurrentChaosPerCombatant, 1d);
        CurrentZombiePerUnit = Normalize(CurrentZombiePerUnit, 1d);
        CurrentHostilePerCombatant = Normalize(CurrentHostilePerCombatant, 1d);
        CurrentScp079Tier = Normalize(CurrentScp079Tier, 2d);
        CurrentCrisisPerLevel = Normalize(CurrentCrisisPerLevel, 1d);
        CurrentWarheadUnlocked = Normalize(CurrentWarheadUnlocked, 2d);
        CurrentWarheadCountdownActive = Normalize(CurrentWarheadCountdownActive, 3d);
        CurrentWarheadDetonated = Normalize(CurrentWarheadDetonated, 0d);
        FactionAdvantageChanged = Normalize(FactionAdvantageChanged, 0d);
        if (LowMinimum > LowMaximum || LowMaximum >= MediumMinimum || MediumMinimum > MediumMaximum || MediumMaximum >= HighMinimum || HighMinimum > HighMaximum)
        {
            LowMinimum = 0d;
            LowMaximum = 29d;
            MediumMinimum = 30d;
            MediumMaximum = 59d;
            HighMinimum = 60d;
            HighMaximum = 100d;
        }
    }

    private static double Normalize(double value, double fallback)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? fallback : value;
    }
}
