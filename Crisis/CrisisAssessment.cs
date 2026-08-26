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
        IEnumerable<CrisisDetectionResult>? detectionResults,
        IReadOnlyDictionary<CrisisTag, long>? episodeIds = null,
        CrisisAssessment? previous = null)
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
        List<CrisisTag> activated = ActiveTags
            .Where(tag => previous is null || !previous.IsActive(tag))
            .ToList();
        List<CrisisTag> resolved = (previous?.ActiveTags ?? Array.Empty<CrisisTag>())
            .Where(tag => !IsActive(tag))
            .ToList();
        ActivatedTags = activated.AsReadOnly();
        ResolvedTags = resolved.AsReadOnly();
        Dictionary<CrisisTag, long> copiedEpisodeIds = new Dictionary<CrisisTag, long>();
        if (episodeIds is not null)
        {
            foreach (KeyValuePair<CrisisTag, long> episode in episodeIds)
            {
                copiedEpisodeIds[episode.Key] = episode.Value;
            }
        }

        EpisodeIds = new ReadOnlyDictionary<CrisisTag, long>(copiedEpisodeIds);
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

    public IReadOnlyList<CrisisTag> ActivatedTags { get; }

    public IReadOnlyList<CrisisTag> ResolvedTags { get; }

    public IReadOnlyDictionary<CrisisTag, long> EpisodeIds { get; }

    public string Code { get; }

    public bool IsActive(CrisisTag tag)
    {
        return detections.TryGetValue(tag, out CrisisDetectionResult? detection) && detection.IsActive;
    }

    public bool TryGetEpisodeId(CrisisTag tag, out long episodeId)
    {
        return EpisodeIds.TryGetValue(tag, out episodeId);
    }
}
