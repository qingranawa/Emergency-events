using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmergencyEvents.O4;

/// <summary>
/// 一个 Director Cycle 对应的 O4 投票会话。
/// </summary>
public sealed class O4VoteSession
{
    private readonly O4SelectionRequest request;
    private readonly HashSet<string> eligibleO4Ids;
    private readonly Dictionary<string, int> votes = new Dictionary<string, int>(StringComparer.Ordinal);
    private O4SelectionResult? terminalResult;

    public O4VoteSession(
        O4SelectionRequest request,
        IEnumerable<O4PlayerSnapshot> eligibleO4,
        DateTime startedAt,
        DateTime endsAt,
        bool allowVoteChange)
    {
        this.request = request ?? throw new ArgumentNullException(nameof(request));
        if (endsAt <= startedAt)
        {
            throw new ArgumentException("EndsAt 必须晚于 StartedAt。", nameof(endsAt));
        }

        RoundId = request.RoundId;
        CycleId = request.CycleId;
        SessionId = request.SessionId;
        StartedAt = startedAt;
        EndsAt = endsAt;
        AllowVoteChange = allowVoteChange;
        CandidateIds = new ReadOnlyCollection<string>(
            request.Candidates.Select(candidate => candidate.EventId).ToArray());
        eligibleO4Ids = new HashSet<string>(
            (eligibleO4 ?? Array.Empty<O4PlayerSnapshot>())
                .Where(O4EligibilityPolicy.IsEligible)
                .Select(player => player.RoundLocalO4Id),
            StringComparer.Ordinal);
        State = O4VoteSessionState.CREATED;
    }

    public long RoundId { get; }

    public long CycleId { get; }

    public string SessionId { get; }

    public DateTime StartedAt { get; }

    public DateTime EndsAt { get; }

    public bool AllowVoteChange { get; }

    public IReadOnlyList<string> CandidateIds { get; }

    public IReadOnlyCollection<string> EligibleO4Ids => eligibleO4Ids;

    public IReadOnlyDictionary<string, int> Votes => new ReadOnlyDictionary<string, int>(votes);

    public O4VoteSessionState State { get; private set; }

    public int VoteCount => votes.Count;

    public bool TryOpen()
    {
        if (State != O4VoteSessionState.CREATED || CandidateIds.Count == 0)
        {
            return false;
        }

        State = O4VoteSessionState.OPEN;
        return true;
    }

    public bool TryCastVote(
        string voterId,
        int candidateIndex,
        DateTime at,
        bool isCurrentlyEligible,
        out bool changedVote,
        out string reason)
    {
        changedVote = false;
        reason = string.Empty;
        if (State != O4VoteSessionState.OPEN)
        {
            reason = "SESSION_NOT_OPEN";
            return false;
        }

        if (at >= EndsAt)
        {
            State = O4VoteSessionState.EXPIRED;
            terminalResult = O4SelectionResult.Fallback(RoundId, CycleId, SessionId, "TIMEOUT", at);
            reason = "EXPIRED";
            return false;
        }

        if (string.IsNullOrWhiteSpace(voterId) || !eligibleO4Ids.Contains(voterId) || !isCurrentlyEligible)
        {
            reason = "O4_NOT_ELIGIBLE";
            return false;
        }

        if (candidateIndex < 1 || candidateIndex > CandidateIds.Count)
        {
            reason = "INVALID_CANDIDATE";
            return false;
        }

        if (votes.TryGetValue(voterId, out int previousIndex))
        {
            if (previousIndex == candidateIndex)
            {
                reason = "UNCHANGED";
                return true;
            }

            if (!AllowVoteChange)
            {
                reason = "VOTE_CHANGE_DISABLED";
                return false;
            }

            changedVote = true;
        }

        votes[voterId] = candidateIndex;
        reason = changedVote ? "CHANGED" : "CAST";
        return true;
    }

    public int GetVoteCount(int candidateIndex)
    {
        return votes.Values.Count(index => index == candidateIndex);
    }

    public O4SelectionResult Resolve(DateTime at, IReadOnlyList<O4PlayerSnapshot> currentO4)
    {
        if (terminalResult is not null)
        {
            return terminalResult;
        }

        HashSet<string> currentEligibleIds = new HashSet<string>(
            (currentO4 ?? Array.Empty<O4PlayerSnapshot>())
                .Where(O4EligibilityPolicy.IsEligible)
                .Select(player => player.RoundLocalO4Id),
            StringComparer.Ordinal);
        int[] counts = new int[CandidateIds.Count];
        int eligibleVotes = 0;
        foreach (KeyValuePair<string, int> vote in votes)
        {
            if (!currentEligibleIds.Contains(vote.Key)
                || vote.Value < 1
                || vote.Value > counts.Length)
            {
                continue;
            }

            counts[vote.Value - 1]++;
            eligibleVotes++;
        }

        int highest = counts.Length == 0 ? 0 : counts.Max();
        int winnerIndex = highest == 0
            ? -1
            : Array.FindIndex(counts, count => count == highest);
        bool hasTie = highest > 0 && counts.Count(count => count == highest) > 1;
        if (highest == 0 || hasTie)
        {
            State = O4VoteSessionState.EXPIRED;
            terminalResult = O4SelectionResult.Fallback(
                RoundId,
                CycleId,
                SessionId,
                highest == 0 ? "NO_VOTE" : "TIE",
                at,
                counts,
                eligibleVotes,
                votes.Count);
            return terminalResult;
        }

        State = O4VoteSessionState.RESOLVED;
        terminalResult = O4SelectionResult.ExplicitWinner(
            RoundId,
            CycleId,
            SessionId,
            request.Candidates[winnerIndex].EventId,
            at,
            counts,
            eligibleVotes,
            votes.Count);
        return terminalResult;
    }

    public O4SelectionResult Cancel(string reason, DateTime at)
    {
        if (terminalResult is not null)
        {
            return terminalResult;
        }

        State = O4VoteSessionState.CANCELLED;
        terminalResult = O4SelectionResult.Cancelled(RoundId, CycleId, SessionId, reason, at);
        return terminalResult;
    }
}
