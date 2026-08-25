namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 只记录客观事实，不记录 Event Director 行为。
/// </summary>
public enum DisorderEventCategory
{
    CombatDeath,
    ScpEliminated,
    MtfForceChanged,
    ChaosForceChanged,
    HostileGoiForceChanged,
    ZombieForceChanged,
    Scp079TierChanged,
    CrisisTransition,
    WarheadChanged,
    FactionAdvantageChanged,
}
