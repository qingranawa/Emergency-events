using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// Module 05 运行时适配器，只拼接 M01-M04.5 已发布事实，不重算任何上游结果。
/// </summary>
public sealed class EventDirectorRuntimeManager
{
    private readonly int minimumPlayers;

    public EventDirectorRuntimeManager(EventDirector director, int minimumPlayers)
    {
        Director = director ?? throw new ArgumentNullException(nameof(director));
        this.minimumPlayers = Math.Max(1, minimumPlayers);
    }

    public EventDirector Director { get; }

    public bool IsActive { get; private set; }

    public bool IsSuspended { get; private set; }

    public long RoundId { get; private set; }

    public int ObservedEvaluationCount { get; private set; }

    public int CreatedCycleCount { get; private set; }

    public DirectorContext? LastContext { get; private set; }

    public bool StartRound(long roundId, PopulationTier populationTier)
    {
        if (IsSuspended && RoundId == roundId)
        {
            return false;
        }

        RoundId = roundId;
        IsActive = true;
        IsSuspended = false;
        return true;
    }

    public bool HandleEvaluation(
        DlrcEvaluationCompletedEvent completedEvent,
        CrisisAssessment? assessment,
        FacilityDisorderState? facilityDisorder,
        IReadOnlyList<MajorWaveRecord> waveHistory,
        DirectorPersonnelFacts personnel,
        FacilityState facilityState,
        bool hasO4Selector)
    {
        if (!IsActive || IsSuspended || completedEvent is null)
        {
            return false;
        }

        ObservedEvaluationCount++;
        if (!completedEvent.Result.IsValid
            || completedEvent.Result.RoundId != RoundId
            || completedEvent.Snapshot.RoundId != RoundId
            || assessment is null
            || assessment.EvaluationId != completedEvent.EvaluationId
            || assessment.Result.RoundId != RoundId)
        {
            return false;
        }

        MajorWaveRecord? current = null;
        MajorWaveRecord? last = null;
        MajorWaveRecord? previous = null;
        if (waveHistory is not null && waveHistory.Count > 0)
        {
            current = waveHistory[waveHistory.Count - 1];
            last = current;
            if (waveHistory.Count > 1)
            {
                previous = waveHistory[waveHistory.Count - 2];
            }
        }

        DirectorContext context = new DirectorContext(
            RoundId,
            completedEvent.Snapshot.Timestamp,
            completedEvent.Result.PopulationTier,
            completedEvent.Result,
            assessment,
            facilityDisorder,
            current,
            last,
            previous,
            personnel,
            facilityState,
            hasO4Selector,
            facilityDisorder?.DisorderBand ?? FacilityDisorderBand.LOW);
        LastContext = context;

        return false;
    }

    public bool TryCreateExplicitCycle()
    {
        if (!IsActive || IsSuspended || LastContext is null)
        {
            return false;
        }

        DirectorCycle? cycle = Director.SelectCycle(LastContext);
        if (cycle is null)
        {
            return false;
        }

        CreatedCycleCount++;
        return true;
    }

    public bool ObservePopulation(int currentPopulation)
    {
        if (!IsActive || IsSuspended || currentPopulation >= minimumPlayers)
        {
            return false;
        }

        SuspendForRound("InsufficientPopulation");
        return true;
    }

    public void SuspendForRound(string reason)
    {
        IsSuspended = true;
        IsActive = false;
        Director.CleanupRound();
    }

    public void CleanupRound()
    {
        IsActive = false;
        IsSuspended = false;
        RoundId = 0L;
        ObservedEvaluationCount = 0;
        CreatedCycleCount = 0;
        LastContext = null;
        Director.CleanupRound();
    }
}
