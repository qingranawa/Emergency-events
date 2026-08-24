namespace EmergencyEvents.Crisis;

/// <summary>
/// 由 CrisisManager 为当前评估提供的共享只读上下文。
/// </summary>
public sealed class CrisisContext
{
    public CrisisContext(
        long evaluationId = 0L,
        DlrcEvaluationTrigger trigger = DlrcEvaluationTrigger.PERIODIC,
        CrisisAssessment? previousAssessment = null)
    {
        EvaluationId = evaluationId;
        Trigger = trigger;
        PreviousAssessment = previousAssessment;
    }

    public long EvaluationId { get; }

    public DlrcEvaluationTrigger Trigger { get; }

    public CrisisAssessment? PreviousAssessment { get; }
}
