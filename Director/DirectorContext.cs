using System;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// Event Director 使用的只读事实边界。Director 不在此处重算上游结果。
/// </summary>
public sealed class DirectorContext
{
    public DirectorContext(
        long roundId,
        DateTime timestamp,
        PopulationTier populationTier,
        DlrcEvaluationResult? dlrcResult,
        CrisisAssessment? crisisAssessment,
        FacilityDisorderState? facilityDisorder,
        MajorWaveRecord? currentWave,
        MajorWaveRecord? lastMajorWave,
        MajorWaveRecord? previousMajorWave,
        DirectorPersonnelFacts personnel,
        FacilityState facilityState,
        bool hasO4Selector,
        FacilityDisorderBand facilityDisorderBand = FacilityDisorderBand.LOW,
        IRecentEventHistory? recentEventHistory = null)
    {
        RoundId = roundId;
        Timestamp = timestamp;
        PopulationTier = populationTier;
        DlrcResult = dlrcResult;
        CrisisAssessment = crisisAssessment;
        FacilityDisorder = facilityDisorder;
        CurrentWave = currentWave;
        LastMajorWave = lastMajorWave;
        PreviousMajorWave = previousMajorWave;
        Personnel = personnel ?? throw new ArgumentNullException(nameof(personnel));
        FacilityState = facilityState;
        HasO4Selector = hasO4Selector;
        FacilityDisorderBand = facilityDisorderBand;
        RecentEventHistory = recentEventHistory;
    }

    public long RoundId { get; }

    public DateTime Timestamp { get; }

    public PopulationTier PopulationTier { get; }

    public DlrcEvaluationResult? DlrcResult { get; }

    public CrisisAssessment? CrisisAssessment { get; }

    public FacilityDisorderState? FacilityDisorder { get; }

    public MajorWaveRecord? CurrentWave { get; }

    public MajorWaveRecord? LastMajorWave { get; }

    public MajorWaveRecord? PreviousMajorWave { get; }

    public DirectorPersonnelFacts Personnel { get; }

    public FacilityState FacilityState { get; }

    public bool HasO4Selector { get; }

    public FacilityDisorderBand FacilityDisorderBand { get; }

    public IRecentEventHistory? RecentEventHistory { get; }
}

/// <summary>
/// Director 的人员可用性事实，不执行职业或阵营重算。
/// </summary>
public sealed class DirectorPersonnelFacts
{
    public DirectorPersonnelFacts(
        int foundationAvailable,
        int chaosAvailable,
        int goiAvailable,
        int eligibleSpectators,
        int overwatchCount,
        int totalOnline)
    {
        FoundationAvailable = Math.Max(0, foundationAvailable);
        ChaosAvailable = Math.Max(0, chaosAvailable);
        GoiAvailable = Math.Max(0, goiAvailable);
        EligibleSpectators = Math.Max(0, eligibleSpectators);
        OverwatchCount = Math.Max(0, overwatchCount);
        TotalOnline = Math.Max(0, totalOnline);
    }

    public int FoundationAvailable { get; }

    public int ChaosAvailable { get; }

    public int GoiAvailable { get; }

    public int EligibleSpectators { get; }

    public int OverwatchCount { get; }

    public int TotalOnline { get; }
}
