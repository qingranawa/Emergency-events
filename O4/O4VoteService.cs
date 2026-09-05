using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EmergencyEvents.O4;

/// <summary>
/// O4 投票会话的有界协调器，不负责候选资格或事件生命周期。
/// </summary>
public sealed class O4VoteService
{
    private readonly O4PanelConfig config;
    private readonly Queue<O4SelectionResult> recentResults = new Queue<O4SelectionResult>();
    private O4VoteSession? activeSession;

    public O4VoteService(O4PanelConfig config)
    {
        this.config = (config ?? new O4PanelConfig()).Normalize();
    }

    public O4VoteSession? ActiveSession => activeSession;

    public IReadOnlyList<O4SelectionResult> RecentResults => new ReadOnlyCollection<O4SelectionResult>(recentResults.ToArray());

    public bool TryOpenSession(
        O4SelectionRequest request,
        IEnumerable<O4PlayerSnapshot> eligibleO4,
        DateTime now,
        out O4SelectionResult immediateResult)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (activeSession is not null)
        {
            immediateResult = O4SelectionResult.Skipped(
                request.RoundId,
                request.CycleId,
                request.SessionId,
                "SESSION_ALREADY_OPEN",
                now);
            return false;
        }

        if (request.Candidates.Count == 0)
        {
            immediateResult = O4SelectionResult.Fallback(
                request.RoundId,
                request.CycleId,
                request.SessionId,
                "NO_CANDIDATE",
                now);
            AddResult(immediateResult);
            return false;
        }

        if (request.Candidates.Count > config.MaxCandidates)
        {
            immediateResult = O4SelectionResult.Fallback(
                request.RoundId,
                request.CycleId,
                request.SessionId,
                "INVALID_CANDIDATE_COUNT",
                now);
            AddResult(immediateResult);
            return false;
        }

        O4PlayerSnapshot[] snapshot = (eligibleO4 ?? Array.Empty<O4PlayerSnapshot>())
            .Where(O4EligibilityPolicy.IsEligible)
            .GroupBy(player => player.RoundLocalO4Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();
        if (request.Candidates.Count == 1)
        {
            immediateResult = O4SelectionResult.ExplicitWinner(
                request.RoundId,
                request.CycleId,
                request.SessionId,
                request.Candidates[0].EventId,
                now);
            AddResult(immediateResult);
            return false;
        }

        if (snapshot.Length == 0)
        {
            immediateResult = O4SelectionResult.Skipped(
                request.RoundId,
                request.CycleId,
                request.SessionId,
                "NO_O4_AVAILABLE",
                now);
            AddResult(immediateResult);
            return false;
        }

        activeSession = new O4VoteSession(
            request,
            now,
            now.AddSeconds(config.VoteDurationSeconds));
        activeSession.TryOpen();
        immediateResult = O4SelectionResult.Pending(request);
        return true;
    }

    public bool TryCastVote(
        string sessionId,
        string voterId,
        int candidateIndex,
        DateTime now,
        bool isCurrentlyEligible,
        out bool changedVote,
        out string reason)
    {
        changedVote = false;
        reason = "NO_ACTIVE_SESSION";
        if (activeSession is null
            || !string.Equals(activeSession.SessionId, sessionId, StringComparison.Ordinal))
        {
            return false;
        }

        return activeSession.TryCastVote(voterId, candidateIndex, now, isCurrentlyEligible, out changedVote, out reason);
    }

    public O4SelectionResult? ResolveActive(DateTime now, IReadOnlyList<O4PlayerSnapshot> currentO4)
    {
        if (activeSession is null)
        {
            return null;
        }

        O4VoteSession session = activeSession;
        O4SelectionResult result = session.Resolve(now, currentO4);
        activeSession = null;
        AddResult(result);
        return result;
    }

    public O4SelectionResult? CancelActive(string reason, DateTime now)
    {
        if (activeSession is null)
        {
            return null;
        }

        O4SelectionResult result = activeSession.Cancel(reason, now);
        activeSession = null;
        AddResult(result);
        return result;
    }

    private void AddResult(O4SelectionResult result)
    {
        recentResults.Enqueue(result);
        while (recentResults.Count > config.HistoryCapacity)
        {
            recentResults.Dequeue();
        }
    }
}
