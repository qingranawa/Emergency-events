using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis.Detectors;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 聚合所有危机判定器的回合服务。
/// </summary>
public sealed class CrisisManager
{
    private readonly CrisisState state = new CrisisState();
    private readonly IReadOnlyList<ICrisisDetector> detectors;
    private readonly HashSet<long> processedEvaluationIds = new HashSet<long>();

    public CrisisManager(CrisisOptions? options = null)
    {
        CrisisOptions configuredOptions = options ?? CrisisOptions.Default;
        detectors = new ICrisisDetector[]
        {
            new BioCrisisDetector(configuredOptions),
            new SysCrisisDetector(),
            new ConCrisisDetector(configuredOptions),
            new SecCrisisDetector(configuredOptions),
            new GoiCrisisDetector(),
            new EndCrisisDetector(configuredOptions),
        };
    }

    public event Action<CrisisAssessment>? CrisisAssessmentUpdated;

    public event Action<CrisisAssessment?, CrisisAssessment>? CrisisChanged;

    public CrisisAssessment? CurrentCrisisAssessment { get; private set; }

    public CrisisAssessment? PreviousCrisisAssessment { get; private set; }

    public CrisisAssessment? Evaluate(DlrcEvaluationCompletedEvent completedEvent)
    {
        if (completedEvent is null)
        {
            throw new ArgumentNullException(nameof(completedEvent));
        }

        if (!completedEvent.Result.IsValid
            || completedEvent.Snapshot.RoundId != completedEvent.Result.RoundId)
        {
            return CurrentCrisisAssessment;
        }

        if (!processedEvaluationIds.Add(completedEvent.EvaluationId))
        {
            return CurrentCrisisAssessment;
        }

        CrisisAssessment? previous = CurrentCrisisAssessment;
        CrisisContext context = new CrisisContext(
            completedEvent.EvaluationId,
            completedEvent.Trigger,
            previous);
        List<CrisisDetectionResult> detections = new List<CrisisDetectionResult>(detectors.Count);
        foreach (ICrisisDetector detector in detectors)
        {
            detections.Add(detector.Detect(
                completedEvent.Snapshot,
                completedEvent.Result,
                state,
                context));
        }

        CrisisAssessment current = new CrisisAssessment(
            completedEvent.EvaluationId,
            completedEvent.Trigger,
            completedEvent.Snapshot,
            completedEvent.Result,
            detections);
        PreviousCrisisAssessment = previous;
        CurrentCrisisAssessment = current;
        CrisisAssessmentUpdated?.Invoke(current);
        if (HasStateChanged(previous, current))
        {
            CrisisChanged?.Invoke(previous, current);
        }

        return current;
    }

    public void CleanupRound()
    {
        state.Reset();
        processedEvaluationIds.Clear();
        PreviousCrisisAssessment = null;
        CurrentCrisisAssessment = null;
    }

    private static bool HasStateChanged(CrisisAssessment? previous, CrisisAssessment current)
    {
        if (previous is null || !string.Equals(previous.Code, current.Code, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            if (previous.GetSeverity(tag) != current.GetSeverity(tag))
            {
                return true;
            }
        }

        return false;
    }
}
