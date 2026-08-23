using System;
using System.Collections.Generic;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 根据快照、Response Score 和有效历史判断现场控制状态。
/// </summary>
public static class ControlEvaluator
{
    public static ControlAssessment Assess(
        RoundSnapshot snapshot,
        ResponseScoreResult score,
        EvaluationHistory history,
        EvaluationOptions options)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (score is null)
        {
            throw new ArgumentNullException(nameof(score));
        }

        if (history is null)
        {
            throw new ArgumentNullException(nameof(history));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        ThreatAssessment threat = AssessThreatTrend(snapshot, score, history, options);
        double foundationCombatShare = score.Breakdown.FoundationCombatShare;
        FoundationStrength foundationStrength = ResolveFoundationStrength(foundationCombatShare);
        List<MajorWaveSnapshot> completedWaves = GetCompletedWaves(snapshot);
        WavePerformance wavePerformance = ResolveWavePerformance(completedWaves);
        BattlefieldMomentum battlefieldMomentum = ResolveBattlefieldMomentum(snapshot);

        int positiveSignals = CountPositiveSignals(
            threat.Trend,
            foundationStrength,
            wavePerformance,
            battlefieldMomentum);
        int negativeSignals = CountNegativeSignals(
            threat.Trend,
            foundationStrength,
            wavePerformance,
            battlefieldMomentum);

        bool collapseConditionA = snapshot.FoundationCombatants == 0
            && score.Breakdown.ScpThreatTotal > 0d;
        bool collapseConditionB = foundationCombatShare < 0.10d
            && (threat.Trend == ThreatTrend.WORSENING
                || threat.Trend == ThreatTrend.STALLED_HIGH)
            && score.NaturalResponseScore >= 65d;
        bool collapseConditionC = HasTwoCatastrophicWaves(completedWaves)
            && threat.Trend != ThreatTrend.IMPROVING;

        ControlState controlState = ResolveControlState(
            collapseConditionA,
            collapseConditionB,
            collapseConditionC,
            positiveSignals,
            negativeSignals,
            threat.Trend,
            foundationStrength);
        int controlLevelCap = ResolveControlLevelCap(controlState);

        return new ControlAssessment(
            threat.Trend,
            threat.Delta,
            threat.FiveMinutesAgoThreat,
            foundationStrength,
            foundationCombatShare,
            wavePerformance,
            battlefieldMomentum,
            positiveSignals,
            negativeSignals,
            collapseConditionA,
            collapseConditionB,
            collapseConditionC,
            controlState,
            controlLevelCap);
    }

    private static ThreatAssessment AssessThreatTrend(
        RoundSnapshot snapshot,
        ResponseScoreResult score,
        EvaluationHistory history,
        EvaluationOptions options)
    {
        DateTime target = snapshot.Timestamp.AddSeconds(-options.ThreatTrendWindowSeconds);
        if (!history.TryGetThreatAtOrBefore(target, out DlrcEvaluationResult? previous))
        {
            return ThreatAssessment.Insufficient;
        }

        double previousThreat = previous!.ResponseBreakdown.ScpThreatTotal;
        double delta = score.Breakdown.ScpThreatTotal - previousThreat;
        ThreatTrend trend = ResolveThreatTrend(delta, score.Breakdown.ScpThreatTotal);
        return new ThreatAssessment(trend, delta, previousThreat);
    }

    private static ThreatTrend ResolveThreatTrend(double delta, double currentThreat)
    {
        if (delta <= -5d)
        {
            return ThreatTrend.IMPROVING;
        }

        if (delta >= 5d)
        {
            return ThreatTrend.WORSENING;
        }

        if (currentThreat >= 28d)
        {
            return ThreatTrend.STALLED_HIGH;
        }

        return ThreatTrend.STABLE;
    }

    private static FoundationStrength ResolveFoundationStrength(double foundationCombatShare)
    {
        if (foundationCombatShare >= 0.45d)
        {
            return FoundationStrength.STRONG;
        }

        if (foundationCombatShare >= 0.30d)
        {
            return FoundationStrength.ADEQUATE;
        }

        if (foundationCombatShare >= 0.15d)
        {
            return FoundationStrength.WEAK;
        }

        return FoundationStrength.CRITICAL;
    }

    private static List<MajorWaveSnapshot> GetCompletedWaves(RoundSnapshot snapshot)
    {
        List<MajorWaveSnapshot> completedWaves = new List<MajorWaveSnapshot>();
        foreach (MajorWaveSnapshot wave in snapshot.MajorWaveHistory)
        {
            if (wave.IsEvaluationComplete && wave.StartingCount > 0)
            {
                completedWaves.Add(wave);
            }
        }

        completedWaves.Sort((left, right) =>
            GetWaveTime(right).CompareTo(GetWaveTime(left)));
        return completedWaves;
    }

    private static WavePerformance ResolveWavePerformance(
        IReadOnlyList<MajorWaveSnapshot> completedWaves)
    {
        if (completedWaves.Count == 0)
        {
            return WavePerformance.NEUTRAL;
        }

        MajorWaveSnapshot latest = completedWaves[0];
        if (IsCatastrophic(latest))
        {
            return WavePerformance.CATASTROPHIC;
        }

        if (latest.BaseFailureScore <= 4d)
        {
            return WavePerformance.GOOD;
        }

        if (latest.BaseFailureScore >= 12d)
        {
            return WavePerformance.POOR;
        }

        return WavePerformance.NEUTRAL;
    }

    private static bool HasTwoCatastrophicWaves(
        IReadOnlyList<MajorWaveSnapshot> completedWaves)
    {
        return completedWaves.Count >= 2
            && IsCatastrophic(completedWaves[0])
            && IsCatastrophic(completedWaves[1]);
    }

    private static bool IsCatastrophic(MajorWaveSnapshot wave)
    {
        return wave.IsCatastrophic || wave.SurvivingCountAtEvaluation == 0;
    }

    private static DateTime GetWaveTime(MajorWaveSnapshot wave)
    {
        return wave.EvaluatedAt ?? wave.StartedAt;
    }

    private static BattlefieldMomentum ResolveBattlefieldMomentum(RoundSnapshot snapshot)
    {
        int enemyLosses = snapshot.RecentHostileDeaths120s
            + snapshot.RecentMainScpDeaths120s;
        int foundationDeaths = snapshot.RecentFoundationDeaths120s;

        if (enemyLosses >= 3 && enemyLosses >= foundationDeaths + 2)
        {
            return BattlefieldMomentum.FOUNDATION_POSITIVE;
        }

        if (foundationDeaths >= 3 && foundationDeaths >= enemyLosses + 2)
        {
            return BattlefieldMomentum.FOUNDATION_NEGATIVE;
        }

        return BattlefieldMomentum.NEUTRAL;
    }

    private static int CountPositiveSignals(
        ThreatTrend threatTrend,
        FoundationStrength foundationStrength,
        WavePerformance wavePerformance,
        BattlefieldMomentum battlefieldMomentum)
    {
        int count = 0;
        if (threatTrend == ThreatTrend.IMPROVING)
        {
            count++;
        }

        if (foundationStrength == FoundationStrength.STRONG)
        {
            count++;
        }

        if (wavePerformance == WavePerformance.GOOD)
        {
            count++;
        }

        if (battlefieldMomentum == BattlefieldMomentum.FOUNDATION_POSITIVE)
        {
            count++;
        }

        return count;
    }

    private static int CountNegativeSignals(
        ThreatTrend threatTrend,
        FoundationStrength foundationStrength,
        WavePerformance wavePerformance,
        BattlefieldMomentum battlefieldMomentum)
    {
        int count = 0;
        if (threatTrend == ThreatTrend.WORSENING
            || threatTrend == ThreatTrend.STALLED_HIGH)
        {
            count++;
        }

        if (foundationStrength == FoundationStrength.WEAK
            || foundationStrength == FoundationStrength.CRITICAL)
        {
            count++;
        }

        if (wavePerformance == WavePerformance.POOR
            || wavePerformance == WavePerformance.CATASTROPHIC)
        {
            count++;
        }

        if (battlefieldMomentum == BattlefieldMomentum.FOUNDATION_NEGATIVE)
        {
            count++;
        }

        return count;
    }

    private static ControlState ResolveControlState(
        bool collapseConditionA,
        bool collapseConditionB,
        bool collapseConditionC,
        int positiveSignals,
        int negativeSignals,
        ThreatTrend threatTrend,
        FoundationStrength foundationStrength)
    {
        if (collapseConditionA || collapseConditionB || collapseConditionC)
        {
            return ControlState.COLLAPSE;
        }

        if (negativeSignals >= 2)
        {
            return ControlState.UNCONTROLLED;
        }

        if (positiveSignals >= 2
            && negativeSignals == 0
            && (threatTrend == ThreatTrend.IMPROVING
                || foundationStrength == FoundationStrength.STRONG))
        {
            return ControlState.ADVANTAGE;
        }

        return ControlState.CONTROLLED;
    }

    private static int ResolveControlLevelCap(ControlState controlState)
    {
        return controlState switch
        {
            ControlState.ADVANTAGE => 2,
            ControlState.CONTROLLED => 3,
            ControlState.UNCONTROLLED => 4,
            _ => 5,
        };
    }

    private readonly struct ThreatAssessment
    {
        public static ThreatAssessment Insufficient { get; } = new ThreatAssessment(
            ThreatTrend.INSUFFICIENT,
            0d,
            null);

        public ThreatAssessment(
            ThreatTrend trend,
            double delta,
            double? fiveMinutesAgoThreat)
        {
            Trend = trend;
            Delta = delta;
            FiveMinutesAgoThreat = fiveMinutesAgoThreat;
        }

        public ThreatTrend Trend { get; }

        public double Delta { get; }

        public double? FiveMinutesAgoThreat { get; }
    }
}
