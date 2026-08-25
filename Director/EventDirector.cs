using System;
using System.Collections.Generic;
using System.Linq;

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
    private readonly List<DirectorLogEntry> logs = new List<DirectorLogEntry>();
    private long nextCycleId;

    public EventDirector(
        IEnumerable<EventDefinition> definitions,
        EventDirectorConfig config,
        IEventCostBoundary? costBoundary = null,
        IRandomSource? randomSource = null)
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
        DirectorCycle cycle = new DirectorCycle(++nextCycleId, context.Timestamp)
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
        return cycle;
    }

    public bool Advance(EventLifecycleState nextState, bool success, DateTime? at = null)
    {
        if (CurrentCycle is null)
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

        return true;
    }

    public bool Commit(EventCandidate candidate, DirectorSlot slot, DateTime committedAt)
    {
        if (CurrentCycle is null
            || CurrentCycle.State != EventLifecycleState.Started
            || candidate is null
            || !IsSelected(candidate, slot))
        {
            return false;
        }

        if (candidate.Definition.IsProfessionalResponse)
        {
            foreach (var tag in candidate.Definition.RequiredCrisisTags)
            {
                if (!Tracker.CanConsume(tag, candidate.Definition.RequiredCrisisSeverity))
                {
                    return false;
                }
            }

            foreach (var tag in candidate.Definition.RequiredCrisisTags)
            {
                Tracker.Consume(tag, candidate.Definition.RequiredCrisisSeverity, CurrentCycle.CycleId.ToString());
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
        IsBusy = false;
        CurrentCycle = null;
        Scheduler.Cleanup();
        Tracker.Reset();
        nextCycleId = 0L;
        logs.Clear();
    }

    private bool IsSelected(EventCandidate candidate, DirectorSlot slot)
    {
        return slot == DirectorSlot.Support
            ? ReferenceEquals(CurrentCycle!.SelectedSupport, candidate)
            : ReferenceEquals(CurrentCycle!.SelectedNonSupport, candidate);
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
