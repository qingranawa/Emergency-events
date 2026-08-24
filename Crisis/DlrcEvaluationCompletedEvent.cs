using System;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis;

/// <summary>
/// Module 03 成功完成一次评估后发布的不可变事件。
/// </summary>
public sealed class DlrcEvaluationCompletedEvent
{
    public DlrcEvaluationCompletedEvent(
        long evaluationId,
        DlrcEvaluationTrigger trigger,
        RoundSnapshot snapshot,
        DlrcEvaluationResult result)
    {
        EvaluationId = evaluationId;
        Trigger = trigger;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public long EvaluationId { get; }

    public DlrcEvaluationTrigger Trigger { get; }

    public RoundSnapshot Snapshot { get; }

    public DlrcEvaluationResult Result { get; }
}

/// <summary>
/// D-LRC 评估来源。
/// </summary>
public enum DlrcEvaluationTrigger
{
    PERIODIC,
    POST_MAJOR_WAVE,
    MANUAL,
    MANUAL_RA,
}
