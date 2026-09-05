using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmergencyEvents.Director;

/// <summary>
/// 两个槽位的候选选择策略，不执行事件生成。
/// </summary>
public sealed class EventSelectionService
{
    private readonly SupportSourceArbitrator arbitrator;

    public EventSelectionService(SupportSourceArbitrator arbitrator)
    {
        this.arbitrator = arbitrator ?? throw new ArgumentNullException(nameof(arbitrator));
    }

    public SelectionDecision? SelectSupport(
        DirectorContext context,
        IReadOnlyList<EventCandidate> candidates,
        ProfessionalResponseTracker tracker)
    {
        EventCandidate[] legal = candidates?
            .Where(candidate => candidate.IsLegal && candidate.Category == EventCategory.Support)
            .ToArray() ?? Array.Empty<EventCandidate>();
        if (legal.Length == 0)
        {
            return null;
        }

        EventCandidate? professional = legal
            .Where(candidate => candidate.Definition.IsProfessionalResponse)
            .OrderByDescending(candidate => candidate.Definition.Priority)
            .ThenByDescending(candidate => candidate.Definition.Weight)
            .ThenBy(candidate => candidate.Definition.EventId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (professional is not null)
        {
            bool isProvisional = legal.Count(candidate => candidate.Definition.IsProfessionalResponse) > 1;
            string reason = isProvisional
                ? "PROVISIONAL:ProfessionalCrisisResponsePriority"
                : "ProfessionalCrisisResponsePriority";
            return new SelectionDecision(professional, professional.Source, false, true, reason);
        }

        EventSource? source = arbitrator.SelectOrdinarySource(context, legal);
        if (source is null)
        {
            return null;
        }

        EventCandidate[] sourceCandidates = legal
            .Where(candidate => candidate.Source == source.Value)
            .OrderByDescending(candidate => candidate.Definition.Priority)
            .ThenByDescending(candidate => candidate.Definition.Weight)
            .ThenBy(candidate => candidate.Definition.EventId, StringComparer.Ordinal)
            .ToArray();
        EventCandidate selected = sourceCandidates[0];
        bool needsO4 = source == EventSource.Foundation && sourceCandidates.Length > 1;
        return new SelectionDecision(
            selected,
            source.Value,
            needsO4,
            !needsO4,
            needsO4 ? "FoundationO4SelectionRequired" : "AutomaticSourceSelection",
            needsO4 ? sourceCandidates.Take(2).ToArray() : new[] { selected });
    }

    public EventCandidate? ResolveTiedCandidates(
        IReadOnlyList<EventCandidate> tiedCandidates,
        IReadOnlyList<string> tiedCandidateIds)
    {
        HashSet<string> allowedIds = new HashSet<string>(
            tiedCandidateIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        return (tiedCandidates ?? Array.Empty<EventCandidate>())
            .Where(candidate => candidate.IsLegal
                && candidate.Category == EventCategory.Support
                && allowedIds.Contains(candidate.Definition.EventId))
            .OrderByDescending(candidate => candidate.Definition.Priority)
            .ThenByDescending(candidate => candidate.Definition.Weight)
            .ThenBy(candidate => candidate.Definition.EventId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public SelectionDecision? SelectNonSupport(
        DirectorContext context,
        IReadOnlyList<EventCandidate> candidates)
    {
        EventCandidate? selected = candidates?
            .Where(candidate => candidate.IsLegal && candidate.Category == EventCategory.NonSupport)
            .OrderByDescending(candidate => candidate.Definition.Priority)
            .ThenByDescending(candidate => candidate.Definition.Weight)
            .ThenBy(candidate => candidate.Definition.EventId, StringComparer.Ordinal)
            .FirstOrDefault();
        return selected is null
            ? null
            : new SelectionDecision(selected, selected.Source, false, true, "AutomaticNonSupportSelection");
    }
}

/// <summary>
/// 选择结果，明确 O4 边界与确定性回退。
/// </summary>
public sealed class SelectionDecision
{
    public SelectionDecision(
        EventCandidate candidate,
        EventSource source,
        bool o4SelectionRequired,
        bool hasFallback,
        string reason,
        IReadOnlyList<EventCandidate>? o4Shortlist = null)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Source = source;
        O4SelectionRequired = o4SelectionRequired;
        HasFallback = hasFallback;
        Reason = reason ?? string.Empty;
        O4Shortlist = new ReadOnlyCollection<EventCandidate>(
            (o4Shortlist ?? new[] { Candidate }).Where(item => item is not null).ToArray());
    }

    public EventCandidate Candidate { get; }

    public EventSource Source { get; }

    public bool O4SelectionRequired { get; }

    public bool HasFallback { get; }

    public string Reason { get; }

    public IReadOnlyList<EventCandidate> O4Shortlist { get; }
}
