using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis.Detectors;
using EmergencyEvents.Evaluation;

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

    /// <summary>
    /// 使用正式判定器检查指定危机，但不写入真实回合状态。
    /// </summary>
    public bool TryDiagnose(
        CrisisTag tag,
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        out CrisisDetectionResult? detection)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ICrisisDetector? detector = FindDetector(tag);
        if (detector is null)
        {
            detection = null;
            return false;
        }

        CrisisState diagnosticState = state.Clone();
        CrisisContext context = new CrisisContext(
            evaluationId: 0L,
            trigger: DlrcEvaluationTrigger.MANUAL_RA,
            previousAssessment: CurrentCrisisAssessment);
        detection = detector.Detect(snapshot, result, diagnosticState, context);
        return true;
    }

    /// <summary>
    /// 以正式 CON 判定器执行一次强制检查点，默认只作用于状态副本。
    /// </summary>
    public bool TryRunContainmentCheckpoint(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        bool commit,
        out CrisisDetectionResult? detection)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ConCrisisDetector? detector = FindDetector(CrisisTag.CON) as ConCrisisDetector;
        CrisisState targetState = commit ? state : state.Clone();
        if (detector is null || !targetState.TryForceContainmentCheckpoint(snapshot.Timestamp))
        {
            detection = null;
            return false;
        }

        detection = detector.Detect(
            snapshot,
            result,
            targetState,
            new CrisisContext(trigger: DlrcEvaluationTrigger.MANUAL_RA, previousAssessment: CurrentCrisisAssessment));
        return true;
    }

    /// <summary>
    /// 使用正式 END 判定器模拟连续地表僵持时长，不修改服务器时间或真实状态。
    /// </summary>
    public bool TryDiagnoseEndSimulation(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        int simulatedSeconds,
        out CrisisDetectionResult? detection)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        EndCrisisDetector? detector = FindDetector(CrisisTag.END) as EndCrisisDetector;
        if (detector is null || !snapshot.WarheadDetonated)
        {
            detection = null;
            return false;
        }

        CrisisState diagnosticState = state.Clone();
        diagnosticState.ResetEndgame();
        diagnosticState.ObserveWarheadDetonation(snapshot.Timestamp);
        diagnosticState.StartSurfaceStalemate(snapshot.Timestamp.AddSeconds(-Math.Max(0, simulatedSeconds)));
        detection = detector.Detect(
            snapshot,
            result,
            diagnosticState,
            new CrisisContext(trigger: DlrcEvaluationTrigger.MANUAL_RA, previousAssessment: CurrentCrisisAssessment));
        return true;
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

    private ICrisisDetector? FindDetector(CrisisTag tag)
    {
        foreach (ICrisisDetector detector in detectors)
        {
            if (MatchesTag(detector, tag))
            {
                return detector;
            }
        }

        return null;
    }

    private static bool MatchesTag(ICrisisDetector detector, CrisisTag tag)
    {
        return (tag == CrisisTag.BIO && detector is BioCrisisDetector)
            || (tag == CrisisTag.SYS && detector is SysCrisisDetector)
            || (tag == CrisisTag.CON && detector is ConCrisisDetector)
            || (tag == CrisisTag.SEC && detector is SecCrisisDetector)
            || (tag == CrisisTag.GOI && detector is GoiCrisisDetector)
            || (tag == CrisisTag.END && detector is EndCrisisDetector);
    }
}
