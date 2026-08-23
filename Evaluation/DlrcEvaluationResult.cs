using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 一次 DLRC 评估的最终纯逻辑结果。
/// </summary>
public sealed class DlrcEvaluationResult
{
    public DlrcEvaluationResult(
        long roundId,
        DateTime timestamp,
        PopulationTier populationTier,
        double naturalResponseScore,
        double persistentAdjustment,
        double effectiveResponseScore,
        ResponseBreakdown responseBreakdown,
        int theoreticalLevel,
        ControlAssessment controlAssessment,
        ControlState controlState,
        int finalLevel,
        bool isValid,
        string code)
    {
        if (responseBreakdown is null)
        {
            throw new ArgumentNullException(nameof(responseBreakdown));
        }

        if (controlAssessment is null)
        {
            throw new ArgumentNullException(nameof(controlAssessment));
        }

        RoundId = roundId;
        Timestamp = timestamp;
        PopulationTier = populationTier;
        NaturalResponseScore = naturalResponseScore;
        PersistentAdjustment = persistentAdjustment;
        EffectiveResponseScore = effectiveResponseScore;
        ResponseBreakdown = responseBreakdown;
        TheoreticalLevel = theoreticalLevel;
        ControlAssessment = controlAssessment;
        ControlState = controlState;
        FinalLevel = finalLevel;
        IsValid = isValid;
        Code = code ?? string.Empty;
    }

    public long RoundId { get; }

    public DateTime Timestamp { get; }

    public PopulationTier PopulationTier { get; }

    public double NaturalResponseScore { get; }

    public double PersistentAdjustment { get; }

    public double EffectiveResponseScore { get; }

    public ResponseBreakdown ResponseBreakdown { get; }

    public int TheoreticalLevel { get; }

    public ControlAssessment ControlAssessment { get; }

    public ControlState ControlState { get; }

    public int FinalLevel { get; }

    public bool IsValid { get; }

    public string Code { get; }
}
