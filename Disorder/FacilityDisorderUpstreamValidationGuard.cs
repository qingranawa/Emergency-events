using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 独立验证上游评估，避免无效结果消费事件窗口。
/// </summary>
public static class FacilityDisorderUpstreamValidationGuard
{
    public static bool IsValid(FacilityDisorderEvaluationContext? context, long expectedRoundId)
    {
        DlrcEvaluationCompletedEvent? evaluation = context?.Evaluation;
        CrisisAssessment? assessment = context?.CrisisAssessment;
        if (evaluation is null || assessment is null || !evaluation.Result.IsValid || !assessment.Result.IsValid)
        {
            return false;
        }

        if (evaluation.Snapshot is null
            || evaluation.Result.RoundId != evaluation.Snapshot.RoundId
            || evaluation.Result.Timestamp != evaluation.Snapshot.Timestamp
            || expectedRoundId != 0L && evaluation.Snapshot.RoundId != expectedRoundId)
        {
            return false;
        }

        return assessment.EvaluationId == evaluation.EvaluationId
            && assessment.Trigger == evaluation.Trigger
            && assessment.Snapshot.RoundId == evaluation.Snapshot.RoundId
            && assessment.Snapshot.Timestamp == evaluation.Snapshot.Timestamp
            && assessment.Result.RoundId == evaluation.Result.RoundId
            && assessment.Result.Timestamp == evaluation.Result.Timestamp;
    }
}
