namespace EmergencyEvents.Evaluation;

/// <summary>
/// 一次评估得到的现场控制分析结果。
/// </summary>
public sealed class ControlAssessment
{
    public ControlAssessment(
        ThreatTrend threatTrend,
        double threatDelta,
        double? fiveMinutesAgoThreat,
        FoundationStrength foundationStrength,
        double foundationCombatShare,
        WavePerformance wavePerformance,
        BattlefieldMomentum battlefieldMomentum,
        int positiveSignals,
        int negativeSignals,
        bool collapseConditionA,
        bool collapseConditionB,
        bool collapseConditionC,
        ControlState controlState,
        int controlLevelCap)
    {
        ThreatTrend = threatTrend;
        ThreatDelta = threatDelta;
        FiveMinutesAgoThreat = fiveMinutesAgoThreat;
        FoundationStrength = foundationStrength;
        FoundationCombatShare = foundationCombatShare;
        WavePerformance = wavePerformance;
        BattlefieldMomentum = battlefieldMomentum;
        PositiveSignals = positiveSignals;
        NegativeSignals = negativeSignals;
        CollapseConditionA = collapseConditionA;
        CollapseConditionB = collapseConditionB;
        CollapseConditionC = collapseConditionC;
        ControlState = controlState;
        ControlLevelCap = controlLevelCap;
    }

    public ThreatTrend ThreatTrend { get; }

    public double ThreatDelta { get; }

    public double? FiveMinutesAgoThreat { get; }

    public FoundationStrength FoundationStrength { get; }

    public double FoundationCombatShare { get; }

    public WavePerformance WavePerformance { get; }

    public BattlefieldMomentum BattlefieldMomentum { get; }

    public int PositiveSignals { get; }

    public int NegativeSignals { get; }

    public bool CollapseConditionA { get; }

    public bool CollapseConditionB { get; }

    public bool CollapseConditionC { get; }

    public ControlState ControlState { get; }

    public int ControlLevelCap { get; }
}
