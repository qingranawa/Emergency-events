using System;
using EmergencyEvents.Crisis;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// 一个事件定义在当前 DirectorContext 下的合法性结果。
/// </summary>
public sealed class EventCandidate
{
    public EventCandidate(
        EventDefinition definition,
        bool isLegal,
        string reason,
        int availablePersonnel,
        int requestedPersonnel,
        int minimumPersonnel,
        int plannedPersonnel,
        CandidateRejectReason rejectReason = CandidateRejectReason.None)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        IsLegal = isLegal;
        Reason = string.IsNullOrWhiteSpace(reason) ? (isLegal ? "Eligible" : "Rejected") : reason;
        RejectReason = rejectReason;
        AvailablePersonnel = Math.Max(0, availablePersonnel);
        RequestedPersonnel = Math.Max(0, requestedPersonnel);
        MinimumPersonnel = Math.Max(0, minimumPersonnel);
        PlannedPersonnel = Math.Max(0, plannedPersonnel);
    }

    public EventDefinition Definition { get; }

    public bool IsLegal { get; }

    public string Reason { get; }

    public CandidateRejectReason RejectReason { get; }

    public EventCategory Category => Definition.Category;

    public EventSource Source => Definition.Source;

    public EventResponseLevel RequiredResponseLevel => Definition.RequiredResponseLevel;

    public int AvailablePersonnel { get; }

    public int RequestedPersonnel { get; }

    public int MinimumPersonnel { get; }

    public int PlannedPersonnel { get; }

}
