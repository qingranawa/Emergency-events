using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 结算所需的同一次 Module 03 与 Module 04 输出。
/// </summary>
public sealed class FacilityDisorderEvaluationContext
{
    public FacilityDisorderEvaluationContext(
        DlrcEvaluationCompletedEvent? evaluation,
        CrisisAssessment? crisisAssessment)
    {
        Evaluation = evaluation;
        CrisisAssessment = crisisAssessment;
    }

    public DlrcEvaluationCompletedEvent? Evaluation { get; }

    public CrisisAssessment? CrisisAssessment { get; }
}
