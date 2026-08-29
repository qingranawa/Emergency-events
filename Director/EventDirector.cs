using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.O4;

namespace EmergencyEvents.Director;

/// <summary>
/// Module 05 Director 核心生命周期协调器，不生成具体生产事件。
/// </summary>
public sealed class EventDirector
{
    private readonly EventDirectorConfig config;
    private readonly EventRegistry registry;
    private readonly EventEligibilityService eligibility = new EventEligibilityService();
    private readonly EventSelectionService selection;
    private readonly IEventCostBoundary costBoundary;
    private readonly IO4EventSelector? o4Selector;
    private readonly List<DirectorLogEntry> logs = new List<DirectorLogEntry>();
    private long nextCycleId;

    public EventDirector(
        IEnumerable<EventDefinition> definitions,
        EventDirectorConfig config,
        IEventCostBoundary? costBoundary = null,
        IRandomSource? randomSource = null,
        IO4EventSelector? o4Selector = null)
    {
        this.config = (config ?? new EventDirectorConfig()).Normalize();
        registry = new EventRegistry();
        foreach (EventDefinition definition in definitions ?? Array.Empty<EventDefinition>())
        {
            registry.Register(definition);
        }

        Tracker = new ProfessionalResponseTracker();
        Scheduler = new EventDirectorScheduler(this.config);
        this.costBoundary = costBoundary ?? new NoOpEventCostBoundary();
        this.o4Selector = o4Selector;
        selection = new EventSelectionService(new SupportSourceArbitrator(this.config, randomSource ?? ProductionRandomSource.Shared));
    }

    public bool IsBusy { get; private set; }

    public DirectorCycle? CurrentCycle { get; private set; }

    public ProfessionalResponseTracker Tracker { get; }

    public EventDirectorScheduler Scheduler { get; }

    public IReadOnlyList<DirectorLogEntry> Logs => logs.AsReadOnly();

    public DirectorCycle? SelectCycle(DirectorContext context)
    {
        if (!config.Enabled || IsBusy || context is null)
        {
            return null;
        }

        IsBusy = true;
        Tracker.Observe(context.CrisisAssessment);
        DirectorCycle cycle = new DirectorCycle(++nextCycleId, context.Timestamp, context.RoundId)
        {
            State = EventLifecycleState.Evaluating,
        };
        List<EventCandidate> candidates = new List<EventCandidate>();
        foreach (EventDefinition definition in registry.All)
        {
            EventCandidate candidate = eligibility.Evaluate(context, definition, Tracker);
            candidates.Add(candidate);
            AddLog(new DirectorLogEntry(
                context.Timestamp,
                cycle.CycleId,
                definition.EventId,
                cycle.State,
                candidate.IsLegal,
                candidate.Reason));
        }

        SelectionDecision? support = selection.SelectSupport(context, candidates, Tracker);
        SelectionDecision? nonSupport = selection.SelectNonSupport(context, candidates);
        if (support is null && nonSupport is null)
        {
            IsBusy = false;
            return null;
        }

        cycle.SelectedSupport = support?.Candidate;
        cycle.SelectedNonSupport = nonSupport?.Candidate;
        if (support is not null)
        {
            AddLog(new DirectorLogEntry(
                context.Timestamp,
                cycle.CycleId,
                support.Candidate.Definition.EventId,
                cycle.State,
                true,
                support.Reason));
        }
        cycle.State = EventLifecycleState.Selected;
        CurrentCycle = cycle;
        if (support is not null
            && support.O4SelectionRequired
            && support.O4Shortlist.Count > 1
            && o4Selector?.IsAvailable == true)
        {
            BeginO4Selection(context, cycle, support);
        }

        return cycle;
    }

    public bool Advance(EventLifecycleState nextState, bool success, DateTime? at = null)
    {
        if (CurrentCycle is null)
        {
            return false;
        }

        if (CurrentCycle.IsAwaitingO4Selection)
        {
            return false;
        }

        if (!success)
        {
            if (CurrentCycle.State is EventLifecycleState.Selected
                or EventLifecycleState.Prepared
                or EventLifecycleState.Started)
            {
                CurrentCycle.State = EventLifecycleState.Failed;
                AddLog(new DirectorLogEntry(
                    at ?? DateTime.UtcNow,
                    CurrentCycle.CycleId,
                    string.Empty,
                    EventLifecycleState.Failed,
                    false,
                    "LifecycleFailure"));
                return true;
            }

            return false;
        }

        if (!IsValidTransition(CurrentCycle.State, nextState))
        {
            return false;
        }

        CurrentCycle.State = nextState;
        AddLog(new DirectorLogEntry(
            at ?? DateTime.UtcNow,
            CurrentCycle.CycleId,
            string.Empty,
            nextState,
            true,
            "LifecycleTransition"));
        if (nextState == EventLifecycleState.Started && !CurrentCycle.ActualFirstSlotStartedAt.HasValue)
        {
            CurrentCycle.ActualFirstSlotStartedAt = at ?? DateTime.UtcNow;
        }

        if (nextState == EventLifecycleState.Completed)
        {
            IsBusy = false;
        }
        else if (nextState == EventLifecycleState.RolledBack)
        {
            ReleaseCurrentCycle();
        }

        return true;
    }

    public bool RevalidateBeforeCommit(DirectorContext latestContext)
    {
        if (CurrentCycle is null || CurrentCycle.State != EventLifecycleState.Started)
        {
            return false;
        }

        return RevalidateCandidates(latestContext);
    }

    /// <summary>
    /// 使用启动前的最新官方事实重验证并启动当前周期。
    /// </summary>
    public bool TryStart(DirectorContext latestContext, DateTime startedAt)
    {
        if (CurrentCycle is null || CurrentCycle.State != EventLifecycleState.Prepared || CurrentCycle.IsAwaitingO4Selection)
        {
            return false;
        }

        if (!RevalidateCandidates(latestContext))
        {
            return false;
        }

        return Advance(EventLifecycleState.Started, true, startedAt);
    }

    public bool Commit(
        EventCandidate candidate,
        DirectorSlot slot,
        DateTime committedAt,
        DirectorContext? latestContext = null)
    {
        if (latestContext is not null && !RevalidateBeforeCommit(latestContext))
        {
            return false;
        }

        if (CurrentCycle is null
            || CurrentCycle.State != EventLifecycleState.Started
            || candidate is null)
        {
            return false;
        }

        EventCandidate? selectedCandidate = slot == DirectorSlot.Support
            ? CurrentCycle.SelectedSupport
            : CurrentCycle.SelectedNonSupport;
        if (selectedCandidate is null
            || !string.Equals(selectedCandidate.Definition.EventId, candidate.Definition.EventId, StringComparison.Ordinal))
        {
            return false;
        }

        candidate = selectedCandidate;

        if (candidate.Definition.IsProfessionalResponse)
        {
            foreach (var tag in candidate.Definition.RequiredCrisisTags)
            {
                if (!Tracker.CanConsume(tag, candidate.Definition.RequiredResponseLevel))
                {
                    return false;
                }
            }

            foreach (var tag in candidate.Definition.RequiredCrisisTags)
            {
                Tracker.Consume(tag, candidate.Definition.RequiredResponseLevel, CurrentCycle.CycleId.ToString());
            }
        }

        costBoundary.Record(candidate, CurrentCycle.CycleId.ToString(), committedAt);
        CurrentCycle.State = EventLifecycleState.Committed;
        AddLog(new DirectorLogEntry(
            committedAt,
            CurrentCycle.CycleId,
            candidate.Definition.EventId,
            EventLifecycleState.Committed,
            true,
            "Committed"));
        if (slot == DirectorSlot.Support && CurrentCycle.ActualFirstSlotStartedAt.HasValue)
        {
            Scheduler.ScheduleSecondSlot(CurrentCycle.CycleId, CurrentCycle.ActualFirstSlotStartedAt.Value);
            CurrentCycle.SecondSlotDueAt = Scheduler.SecondSlotDueAt;
        }

        return true;
    }

    public bool TryBeginSecondSlot(DateTime now)
    {
        if (CurrentCycle is null
            || CurrentCycle.State != EventLifecycleState.Committed
            || CurrentCycle.SelectedNonSupport is null
            || !Scheduler.TryConsumeDueSlot(now))
        {
            return false;
        }

        CurrentCycle.State = EventLifecycleState.Selected;
        AddLog(new DirectorLogEntry(
            now,
            CurrentCycle.CycleId,
            CurrentCycle.SelectedNonSupport.Definition.EventId,
            EventLifecycleState.Selected,
            true,
            "SecondSlotDue"));
        return true;
    }

    public void CleanupRound()
    {
        CancelO4Selection("RoundEnd");
        IsBusy = false;
        CurrentCycle = null;
        Scheduler.Cleanup();
        Tracker.Reset();
        nextCycleId = 0L;
        logs.Clear();
    }

    private void BeginO4Selection(DirectorContext context, DirectorCycle cycle, SelectionDecision decision)
    {
        string sessionId = $"O4-{context.RoundId}-{cycle.CycleId}";
        cycle.O4SelectionCandidates = decision.O4Shortlist;
        cycle.PendingO4SelectionSessionId = sessionId;
        O4SelectionRequest request = new O4SelectionRequest(
            context.RoundId,
            cycle.CycleId,
            sessionId,
            context.Timestamp,
            decision.O4Shortlist.Select(O4CandidateView.From).ToArray(),
            decision.Candidate.Definition.EventId);
        try
        {
            o4Selector!.RequestSelection(request, HandleO4Selection);
            AddLog(new DirectorLogEntry(
                context.Timestamp,
                cycle.CycleId,
                decision.Candidate.Definition.EventId,
                cycle.State,
                true,
                "O4_SELECTION_REQUESTED"));
        }
        catch (Exception exception)
        {
            cycle.PendingO4SelectionSessionId = null;
            AddLog(new DirectorLogEntry(
                context.Timestamp,
                cycle.CycleId,
                decision.Candidate.Definition.EventId,
                EventLifecycleState.Selected,
                true,
                $"O4_SELECTION_FALLBACK:REQUEST_FAILED:{exception.GetType().Name}"));
        }
    }

    private void HandleO4Selection(O4SelectionResult result)
    {
        if (CurrentCycle is null
            || !CurrentCycle.IsAwaitingO4Selection
            || result is null
            || result.RoundId != CurrentCycle.RoundId
            || !result.MatchesBinding(
                result.RoundId,
                CurrentCycle.CycleId,
                CurrentCycle.PendingO4SelectionSessionId ?? string.Empty))
        {
            AddLog(new DirectorLogEntry(
                result?.ResolvedAt ?? DateTime.UtcNow,
                CurrentCycle?.CycleId ?? 0L,
                result?.SelectedEventId ?? string.Empty,
                EventLifecycleState.Selected,
                false,
                "IGNORED_STALE_SELECTION"));
            return;
        }

        DirectorCycle cycle = CurrentCycle;
        cycle.PendingO4SelectionSessionId = null;
        if (result.Outcome != O4SelectionOutcome.EXPLICIT_WINNER)
        {
            AddLog(new DirectorLogEntry(
                result.ResolvedAt,
                cycle.CycleId,
                cycle.SelectedSupport?.Definition.EventId ?? string.Empty,
                cycle.State,
                true,
                $"O4_SELECTION_FALLBACK:{result.Reason}"));
            return;
        }

        EventCandidate? selected = cycle.O4SelectionCandidates.FirstOrDefault(candidate =>
            string.Equals(candidate.Definition.EventId, result.SelectedEventId, StringComparison.Ordinal)
            && candidate.IsLegal);
        if (selected is null)
        {
            AddLog(new DirectorLogEntry(
                result.ResolvedAt,
                cycle.CycleId,
                cycle.SelectedSupport?.Definition.EventId ?? string.Empty,
                cycle.State,
                true,
                "O4_SELECTION_FALLBACK:INVALIDATED"));
            return;
        }

        cycle.SelectedSupport = selected;
        AddLog(new DirectorLogEntry(
            result.ResolvedAt,
            cycle.CycleId,
            selected.Definition.EventId,
            cycle.State,
            true,
            "O4_SELECTION_RESOLVED"));
    }

    private void CancelO4Selection(string reason)
    {
        if (CurrentCycle?.IsAwaitingO4Selection != true || o4Selector is null)
        {
            return;
        }

        try
        {
            o4Selector.CancelAll(reason);
        }
        catch (Exception exception)
        {
            AddLog(new DirectorLogEntry(
                DateTime.UtcNow,
                CurrentCycle.CycleId,
                string.Empty,
                EventLifecycleState.Failed,
                false,
                $"O4_SELECTION_CANCEL_FAILED:{exception.GetType().Name}"));
        }

        CurrentCycle.PendingO4SelectionSessionId = null;
    }

    private EventCandidate? RevalidateCandidate(EventCandidate? candidate, DirectorContext context)
    {
        if (candidate is null)
        {
            return null;
        }

        EventCandidate refreshed = eligibility.Evaluate(context, candidate.Definition, Tracker);
        return refreshed.IsLegal ? refreshed : null;
    }

    private bool RevalidateCandidates(DirectorContext latestContext)
    {
        if (CurrentCycle is null)
        {
            return false;
        }

        if (latestContext is null || latestContext.RoundId != latestContext.DlrcResult?.RoundId)
        {
            AbortCurrentCycle("RevalidationFailed", latestContext?.Timestamp ?? DateTime.UtcNow);
            return false;
        }

        Tracker.Observe(latestContext.CrisisAssessment);

        EventCandidate? support = RevalidateCandidate(CurrentCycle.SelectedSupport, latestContext);
        EventCandidate? nonSupport = RevalidateCandidate(CurrentCycle.SelectedNonSupport, latestContext);
        if ((CurrentCycle.SelectedSupport is not null && support is null)
            || (CurrentCycle.SelectedNonSupport is not null && nonSupport is null))
        {
            AbortCurrentCycle("RevalidationFailed", latestContext.Timestamp);
            return false;
        }

        CurrentCycle.SelectedSupport = support;
        CurrentCycle.SelectedNonSupport = nonSupport;
        return true;
    }

    private void AbortCurrentCycle(string reason, DateTime at)
    {
        if (CurrentCycle is null)
        {
            return;
        }

        CurrentCycle.State = EventLifecycleState.Failed;
        AddLog(new DirectorLogEntry(at, CurrentCycle.CycleId, string.Empty, EventLifecycleState.Failed, false, reason));
        CurrentCycle.State = EventLifecycleState.RolledBack;
        AddLog(new DirectorLogEntry(at, CurrentCycle.CycleId, string.Empty, EventLifecycleState.RolledBack, true, reason));
        ReleaseCurrentCycle();
    }

    private void ReleaseCurrentCycle()
    {
        CancelO4Selection("RuntimeCleanup");
        IsBusy = false;
        CurrentCycle = null;
        Scheduler.Cleanup();
    }

    private static bool IsValidTransition(EventLifecycleState current, EventLifecycleState next)
    {
        return (current, next) switch
        {
            (EventLifecycleState.Scheduled, EventLifecycleState.Evaluating) => true,
            (EventLifecycleState.Evaluating, EventLifecycleState.Selected) => true,
            (EventLifecycleState.Selected, EventLifecycleState.Prepared) => true,
            (EventLifecycleState.Prepared, EventLifecycleState.Started) => true,
            (EventLifecycleState.Failed, EventLifecycleState.RolledBack) => true,
            (EventLifecycleState.Committed, EventLifecycleState.Completed) => true,
            _ => false,
        };
    }

    private void AddLog(DirectorLogEntry entry)
    {
        logs.Add(entry);
        if (logs.Count > config.MaxLogEntries)
        {
            logs.RemoveRange(0, logs.Count - config.MaxLogEntries);
        }
    }
}
