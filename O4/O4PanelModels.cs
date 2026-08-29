using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EmergencyEvents.Crisis;
using EmergencyEvents.Director;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.O4;

/// <summary>
/// O4 的运行时角色分类，只表达面板资格所需的事实。
/// </summary>
public enum O4RoleState
{
    None,
    Alive,
    Spectator,
    Overwatch,
}

/// <summary>
/// O4 投票会话的显式生命周期。
/// </summary>
public enum O4VoteSessionState
{
    CREATED,
    OPEN,
    RESOLVED,
    CANCELLED,
    EXPIRED,
}

/// <summary>
/// O4 选择结果类型。
/// </summary>
public enum O4SelectionOutcome
{
    PENDING,
    EXPLICIT_WINNER,
    FALLBACK,
    CANCELLED,
    STALE,
}

/// <summary>
/// 会话开始时拍摄的 O4 资格快照。
/// </summary>
public sealed class O4PlayerSnapshot
{
    public O4PlayerSnapshot(string roundLocalO4Id, bool isConnected, O4RoleState role)
    {
        if (string.IsNullOrWhiteSpace(roundLocalO4Id))
        {
            throw new ArgumentException("RoundLocalO4Id 不能为空。", nameof(roundLocalO4Id));
        }

        RoundLocalO4Id = roundLocalO4Id.Trim();
        IsConnected = isConnected;
        Role = role;
    }

    public string RoundLocalO4Id { get; }

    public bool IsConnected { get; }

    public O4RoleState Role { get; }
}

/// <summary>
/// O4 资格的唯一规则入口。
/// </summary>
public static class O4EligibilityPolicy
{
    public static bool IsEligible(O4PlayerSnapshot player)
    {
        return player is not null && IsEligible(player.IsConnected, player.Role);
    }

    public static bool IsEligible(bool isConnected, O4RoleState role)
    {
        return isConnected && (role == O4RoleState.Spectator || role == O4RoleState.Overwatch);
    }
}

/// <summary>
/// M05 已经选出的、只用于展示的候选字段。
/// </summary>
public sealed class O4CandidateView
{
    public O4CandidateView(
        string eventId,
        string displayName,
        EventCategory category,
        EventSource source,
        bool isProfessionalResponse)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("EventId 不能为空。", nameof(eventId));
        }

        EventId = eventId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? EventId : displayName.Trim();
        Category = category;
        Source = source;
        IsProfessionalResponse = isProfessionalResponse;
    }

    public string EventId { get; }

    public string DisplayName { get; }

    public EventCategory Category { get; }

    public EventSource Source { get; }

    public bool IsProfessionalResponse { get; }

    public static O4CandidateView From(EventCandidate candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        return new O4CandidateView(
            candidate.Definition.EventId,
            candidate.Definition.DisplayName,
            candidate.Category,
            candidate.Source,
            candidate.Definition.IsProfessionalResponse);
    }
}

/// <summary>
/// M05 发给 M06 的最小选择请求。
/// </summary>
public sealed class O4SelectionRequest
{
    public O4SelectionRequest(
        long roundId,
        long cycleId,
        string sessionId,
        DateTime requestedAt,
        IReadOnlyList<O4CandidateView> candidates,
        string fallbackCandidateId)
    {
        if (roundId <= 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(roundId));
        }

        if (cycleId <= 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(cycleId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("SessionId 不能为空。", nameof(sessionId));
        }

        RoundId = roundId;
        CycleId = cycleId;
        SessionId = sessionId.Trim();
        RequestedAt = requestedAt;
        Candidates = new ReadOnlyCollection<O4CandidateView>(
            (candidates ?? Array.Empty<O4CandidateView>()).Where(candidate => candidate is not null).ToArray());
        FallbackCandidateId = fallbackCandidateId?.Trim() ?? string.Empty;
    }

    public long RoundId { get; }

    public long CycleId { get; }

    public string SessionId { get; }

    public DateTime RequestedAt { get; }

    public IReadOnlyList<O4CandidateView> Candidates { get; }

    public string FallbackCandidateId { get; }
}

/// <summary>
/// O4 只读面板模型，字段全部来自已经完成的上游评估。
/// </summary>
public sealed class O4PanelViewModel
{
    public O4PanelViewModel(
        string dlrcCode,
        int responseLevel,
        ControlState controlState,
        IEnumerable<CrisisTag>? activeCrisisTags,
        FacilityDisorderBand fdiBand,
        DateTime timestamp,
        DateTime? nextEvaluationAt)
    {
        DlrcCode = string.IsNullOrWhiteSpace(dlrcCode) ? "DLRC --" : dlrcCode.Trim();
        ResponseLevel = Math.Min(5, Math.Max(0, responseLevel));
        ControlState = controlState;
        ActiveCrisisTags = new ReadOnlyCollection<CrisisTag>(
            (activeCrisisTags ?? Array.Empty<CrisisTag>()).Distinct().OrderBy(tag => tag).ToArray());
        FdiBand = fdiBand;
        Timestamp = timestamp;
        NextEvaluationAt = nextEvaluationAt;
    }

    public string DlrcCode { get; }

    public int ResponseLevel { get; }

    public ControlState ControlState { get; }

    public IReadOnlyList<CrisisTag> ActiveCrisisTags { get; }

    public FacilityDisorderBand FdiBand { get; }

    public DateTime Timestamp { get; }

    public DateTime? NextEvaluationAt { get; }
}
