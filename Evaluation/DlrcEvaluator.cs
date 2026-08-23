using System;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 聚合 Response Score、Control State 和最终 DLRC 代码。
/// </summary>
public static class DlrcEvaluator
{
    public static DlrcEvaluationResult Evaluate(
        RoundSnapshot snapshot,
        EvaluationHistory history,
        EvaluationOptions options,
        double persistentAdjustment = 0d)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (history is null)
        {
            throw new ArgumentNullException(nameof(history));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        ResponseScoreResult score = ResponseScoreCalculator.Calculate(
            snapshot,
            options,
            persistentAdjustment);
        return EvaluateWithScore(snapshot, history, options, score);
    }

    internal static DlrcEvaluationResult EvaluateWithScore(
        RoundSnapshot snapshot,
        EvaluationHistory history,
        EvaluationOptions options,
        ResponseScoreResult score)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (history is null)
        {
            throw new ArgumentNullException(nameof(history));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (score is null)
        {
            throw new ArgumentNullException(nameof(score));
        }

        ControlAssessment controlAssessment = ControlEvaluator.Assess(
            snapshot,
            score,
            history,
            options);
        int theoreticalLevel = LevelResolver.ResolveTheoreticalLevel(
            snapshot.PopulationTier,
            score.EffectiveResponseScore,
            options);
        int finalLevel = Math.Min(
            theoreticalLevel,
            controlAssessment.ControlLevelCap);
        string code = $"DLRC-{snapshot.PopulationTier}{finalLevel}";

        return new DlrcEvaluationResult(
            snapshot.RoundId,
            snapshot.Timestamp,
            snapshot.PopulationTier,
            score.NaturalResponseScore,
            score.PersistentAdjustment,
            score.EffectiveResponseScore,
            score.Breakdown,
            theoreticalLevel,
            controlAssessment,
            controlAssessment.ControlState,
            finalLevel,
            isValid: true,
            code);
    }
}
