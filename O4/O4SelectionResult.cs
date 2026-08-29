using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmergencyEvents.O4;

/// <summary>
/// 从 M06 返回 M05 的不可变选择结果。
/// </summary>
public sealed class O4SelectionResult
{
    public O4SelectionResult(
        long roundId,
        long cycleId,
        string sessionId,
        string? selectedEventId,
        O4SelectionOutcome outcome,
        string reason,
        DateTime resolvedAt,
        IReadOnlyList<int>? candidateVoteCounts = null,
        int eligibleVotes = 0,
        int votesReceived = 0)
    {
        RoundId = roundId;
        CycleId = cycleId;
        SessionId = sessionId ?? string.Empty;
        SelectedEventId = selectedEventId ?? string.Empty;
        Outcome = outcome;
        Reason = reason ?? string.Empty;
        ResolvedAt = resolvedAt;
        CandidateVoteCounts = new ReadOnlyCollection<int>(
            (candidateVoteCounts ?? Array.Empty<int>()).Select(count => Math.Max(0, count)).ToArray());
        EligibleVotes = Math.Max(0, eligibleVotes);
        VotesReceived = Math.Max(0, votesReceived);
    }

    public long RoundId { get; }

    public long CycleId { get; }

    public string SessionId { get; }

    public string SelectedEventId { get; }

    public O4SelectionOutcome Outcome { get; }

    public string Reason { get; }

    public DateTime ResolvedAt { get; }

    public IReadOnlyList<int> CandidateVoteCounts { get; }

    public int EligibleVotes { get; }

    public int VotesReceived { get; }

    public static O4SelectionResult Pending(O4SelectionRequest request)
    {
        return new O4SelectionResult(
            request.RoundId,
            request.CycleId,
            request.SessionId,
            string.Empty,
            O4SelectionOutcome.PENDING,
            "OPEN",
            request.RequestedAt);
    }

    public static O4SelectionResult ExplicitWinner(
        long roundId,
        long cycleId,
        string sessionId,
        string selectedEventId,
        DateTime resolvedAt,
        IReadOnlyList<int>? candidateVoteCounts = null,
        int eligibleVotes = 0,
        int votesReceived = 0)
    {
        return new O4SelectionResult(
            roundId,
            cycleId,
            sessionId,
            selectedEventId,
            O4SelectionOutcome.EXPLICIT_WINNER,
            "O4_WINNER",
            resolvedAt,
            candidateVoteCounts,
            eligibleVotes,
            votesReceived);
    }

    public static O4SelectionResult Fallback(
        long roundId,
        long cycleId,
        string sessionId,
        string reason,
        DateTime resolvedAt,
        IReadOnlyList<int>? candidateVoteCounts = null,
        int eligibleVotes = 0,
        int votesReceived = 0)
    {
        return new O4SelectionResult(
            roundId,
            cycleId,
            sessionId,
            string.Empty,
            O4SelectionOutcome.FALLBACK,
            reason,
            resolvedAt,
            candidateVoteCounts,
            eligibleVotes,
            votesReceived);
    }

    public static O4SelectionResult Cancelled(
        long roundId,
        long cycleId,
        string sessionId,
        string reason,
        DateTime resolvedAt)
    {
        return new O4SelectionResult(
            roundId,
            cycleId,
            sessionId,
            string.Empty,
            O4SelectionOutcome.CANCELLED,
            reason,
            resolvedAt);
    }

    public bool MatchesBinding(long roundId, long cycleId, string sessionId)
    {
        return RoundId == roundId
            && CycleId == cycleId
            && string.Equals(SessionId, sessionId, StringComparison.Ordinal);
    }
}
