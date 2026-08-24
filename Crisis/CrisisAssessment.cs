using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 当前回合的危机评估公开入口。
/// </summary>
public sealed class CrisisAssessment
{
    private readonly IReadOnlyDictionary<CrisisTag, CrisisDetectionResult> detections;

    public CrisisAssessment(
        long evaluationId,
        DlrcEvaluationTrigger trigger,
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        IEnumerable<CrisisDetectionResult>? detectionResults)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        EvaluationId = evaluationId;
        Trigger = trigger;
        Dictionary<CrisisTag, CrisisDetectionResult> copy = new Dictionary<CrisisTag, CrisisDetectionResult>();
        if (detectionResults is not null)
        {
            foreach (CrisisDetectionResult detection in detectionResults)
            {
                if (detection is not null)
                {
                    copy[detection.Tag] = detection;
                }
            }
        }

        detections = new ReadOnlyDictionary<CrisisTag, CrisisDetectionResult>(copy);
        ActiveTags = Array.AsReadOnly(
            copy.Values
                .Where(detection => detection.IsActive)
                .OrderBy(detection => detection.Tag)
                .Select(detection => detection.Tag)
                .ToArray());
        Code = ActiveTags.Count == 0
            ? result.Code
            : $"{result.Code}-{string.Join("+", ActiveTags)}";
    }

    public long EvaluationId { get; }

    public DlrcEvaluationTrigger Trigger { get; }

    public RoundSnapshot Snapshot { get; }

    public DlrcEvaluationResult Result { get; }

    public IReadOnlyDictionary<CrisisTag, CrisisDetectionResult> Detections => detections;

    public IReadOnlyList<CrisisTag> ActiveTags { get; }

    public string Code { get; }

    public bool IsActive(CrisisTag tag)
    {
        return detections.TryGetValue(tag, out CrisisDetectionResult? detection) && detection.IsActive;
    }

    public CrisisSeverity GetSeverity(CrisisTag tag)
    {
        return detections.TryGetValue(tag, out CrisisDetectionResult? detection)
            ? detection.Severity
            : CrisisSeverity.Inactive;
    }
}
