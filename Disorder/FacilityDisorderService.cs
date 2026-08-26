using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 纯逻辑服务。首次成功 PERIODIC 使用存量加窗口瞬时量，之后只使用纯增量。
/// </summary>
public sealed class FacilityDisorderService
{
    private const int RecordedEventIdCapacity = 2048;
    private readonly FacilityDisorderConfig config;
    private readonly List<DisorderEvent> events = new List<DisorderEvent>();
    private readonly HashSet<string> eventIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> recordedEventIdOrder = new Queue<string>();
    private readonly List<FacilityDisorderSettlement> settlementHistory = new List<FacilityDisorderSettlement>();
    private DateTime? failedInitialEvaluationAt;

    public FacilityDisorderService(FacilityDisorderConfig? config = null)
    {
        this.config = config ?? new FacilityDisorderConfig();
        this.config.Validate();
    }

    public FacilityDisorderState State { get; } = new FacilityDisorderState();

    public int EventCount => events.Count;

    public IReadOnlyList<DisorderEvent> Events => events.AsReadOnly();

    public IReadOnlyList<FacilityDisorderSettlement> History => settlementHistory.AsReadOnly();

    public void StartRound(DateTime roundStartedAt, int openingPopulation, long roundId = 0L)
    {
        CleanupRound();
        State.RoundStartedAt = roundStartedAt;
        State.RoundId = roundId;
        if (!config.Enabled || openingPopulation < config.MinimumPlayers)
        {
            return;
        }

        State.IsActive = true;
    }

    public bool ObservePopulation(int currentPopulation)
    {
        if (!State.IsActive || State.IsSuspended || currentPopulation >= config.MinimumPlayers)
        {
            return false;
        }

        State.IsSuspended = true;
        State.IsActive = false;
        return true;
    }

    public bool Record(DisorderEvent disorderEvent)
    {
        if (!State.IsActive || disorderEvent is null || disorderEvent.IsDryRun || recordedEventIdOrder.Contains(disorderEvent.EventId))
        {
            return false;
        }

        recordedEventIdOrder.Enqueue(disorderEvent.EventId);
        eventIds.Add(disorderEvent.EventId);
        while (recordedEventIdOrder.Count > RecordedEventIdCapacity)
        {
            eventIds.Remove(recordedEventIdOrder.Dequeue());
        }

        events.Add(disorderEvent);
        return true;
    }

    public FacilityDisorderSettlement? SettlePeriodic(
        FacilityDisorderEvaluationContext? context,
        FacilityDisorderStockSnapshot? stock)
    {
        DlrcEvaluationCompletedEvent? evaluation = context?.Evaluation;
        if (!State.IsActive || State.IsSuspended || evaluation?.Trigger != DlrcEvaluationTrigger.PERIODIC)
        {
            return null;
        }

        if (!FacilityDisorderUpstreamValidationGuard.IsValid(context, State.RoundId))
        {
            RememberFailedInitialEvaluation(evaluation);
            return null;
        }

        DateTime timestamp = evaluation.Snapshot.Timestamp;
        if (State.IsInitialized && State.LastProcessedAt >= timestamp)
        {
            return null;
        }

        bool isInitialSettlement = !State.IsInitialized;
        DateTime windowStart = isInitialSettlement
            ? (failedInitialEvaluationAt ?? timestamp).AddSeconds(-config.InitialLookbackSeconds)
            : State.LastProcessedAt!.Value;
        IEnumerable<DisorderEvent> selected = events.Where(disorderEvent =>
            !disorderEvent.IsDryRun
            && (isInitialSettlement
                ? disorderEvent.Timestamp >= windowStart
                : disorderEvent.Timestamp > windowStart)
            && disorderEvent.Timestamp <= timestamp);
        List<DisorderEvent> processed = selected.ToList();
        double previousValue = isInitialSettlement ? config.InitialBase : State.CurrentFacilityDisorder;
        double currentStockAdjustment = isInitialSettlement
            ? FacilityDisorderStockCalculator.Calculate(stock ?? throw new ArgumentNullException(nameof(stock)), config)
            : 0d;
        double recentTransientDelta = isInitialSettlement
            ? processed.Where(disorderEvent => !disorderEvent.IsRepresentedByCurrentStock).Sum(disorderEvent => disorderEvent.Delta)
            : processed.Sum(disorderEvent => disorderEvent.Delta);
        double delta = currentStockAdjustment + recentTransientDelta;
        double currentValue = Clamp(previousValue + delta, config.LowMinimum, config.HighMaximum);
        FacilityDisorderSettlement settlement = new FacilityDisorderSettlement(
            windowStart,
            timestamp,
            previousValue,
            delta,
            currentValue,
            currentStockAdjustment,
            recentTransientDelta,
            processed);
        State.IsInitialized = true;
        State.CurrentFacilityDisorder = currentValue;
        State.DisorderBand = ResolveBand(currentValue);
        State.LastProcessedAt = timestamp;
        State.LastSettlementAt = timestamp;
        State.LastSettlement = settlement;
        settlementHistory.Add(settlement);
        TrimSettlementHistory();
        TrimEventStorage();
        failedInitialEvaluationAt = null;
        return settlement;
    }

    public bool ObserveEvaluation(DateTime _, DlrcEvaluationTrigger trigger)
    {
        return trigger is DlrcEvaluationTrigger.POST_MAJOR_WAVE or DlrcEvaluationTrigger.MANUAL or DlrcEvaluationTrigger.MANUAL_RA;
    }

    public void CleanupRound()
    {
        events.Clear();
        eventIds.Clear();
        recordedEventIdOrder.Clear();
        settlementHistory.Clear();
        failedInitialEvaluationAt = null;
        State.Reset();
    }

    private void TrimSettlementHistory()
    {
        int overflow = settlementHistory.Count - config.SettlementHistoryCapacity;
        if (overflow > 0)
        {
            settlementHistory.RemoveRange(0, overflow);
        }
    }

    private void TrimEventStorage()
    {
        if (!State.LastProcessedAt.HasValue || events.Count <= config.EventHistoryCapacity)
        {
            return;
        }

        DateTime processedThrough = State.LastProcessedAt.Value;
        List<DisorderEvent> pending = events
            .Where(disorderEvent => disorderEvent.Timestamp > processedThrough)
            .ToList();
        List<DisorderEvent> settled = events
            .Where(disorderEvent => disorderEvent.Timestamp <= processedThrough)
            .OrderByDescending(disorderEvent => disorderEvent.Timestamp)
            .Take(config.EventHistoryCapacity)
            .ToList();

        events.Clear();
        events.AddRange(pending
            .Concat(settled)
            .OrderBy(disorderEvent => disorderEvent.Timestamp)
            .ThenBy(disorderEvent => disorderEvent.EventId));
        eventIds.Clear();
        foreach (DisorderEvent disorderEvent in events)
        {
            eventIds.Add(disorderEvent.EventId);
        }
    }

    private void RememberFailedInitialEvaluation(DlrcEvaluationCompletedEvent evaluation)
    {
        if (!State.IsInitialized && evaluation.Snapshot.RoundId == State.RoundId && !failedInitialEvaluationAt.HasValue)
        {
            failedInitialEvaluationAt = evaluation.Snapshot.Timestamp;
        }
    }

    private FacilityDisorderBand ResolveBand(double value)
    {
        if (value < config.MediumMinimum)
        {
            return FacilityDisorderBand.LOW;
        }

        if (value < config.HighMinimum)
        {
            return FacilityDisorderBand.MEDIUM;
        }

        return FacilityDisorderBand.HIGH;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }
}
