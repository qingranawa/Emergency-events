using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// 06:31 结算时的客观存量事实，不包含 FDI 危机结论。
/// </summary>
public sealed class FacilityDisorderStockSnapshot
{
    public FacilityDisorderStockSnapshot(
        int mtfCount,
        int chaosCount,
        int zombieCount,
        int currentHostileForce,
        bool scp079Present,
        int scp079Tier,
        CrisisAssessment? crisisAssessment,
        bool warheadUnlocked,
        bool warheadActive,
        bool warheadDetonated,
        bool isFacilityDestroyed = false)
    {
        MtfCount = Normalize(mtfCount);
        ChaosCount = Normalize(chaosCount);
        ZombieCount = Normalize(zombieCount);
        CurrentHostileForce = Normalize(currentHostileForce);
        Scp079Present = scp079Present;
        Scp079Tier = scp079Present ? System.Math.Min(System.Math.Max(scp079Tier, 0), 5) : 0;
        CrisisAssessment = crisisAssessment;
        WarheadUnlocked = warheadUnlocked;
        WarheadActive = warheadActive;
        WarheadDetonated = warheadDetonated;
        IsFacilityDestroyed = isFacilityDestroyed || warheadDetonated;
    }

    public int MtfCount { get; }

    public int ChaosCount { get; }

    public int ZombieCount { get; }

    public int CurrentHostileForce { get; }

    public bool Scp079Present { get; }

    public int Scp079Tier { get; }

    public CrisisAssessment? CrisisAssessment { get; }

    public bool WarheadUnlocked { get; }

    public bool WarheadActive { get; }

    public bool WarheadDetonated { get; }

    public bool IsFacilityDestroyed { get; }

    private static int Normalize(int value)
    {
        return value < 0 ? 0 : value;
    }
}
