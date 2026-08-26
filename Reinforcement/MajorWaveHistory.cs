using System;
using System.Collections.Generic;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 一次实际 Primary Wave 的事实记录，不包含 D-LRC 判定。
/// </summary>
public sealed class MajorWaveRecord
{
    public MajorWaveRecord(
        string waveId,
        string faction,
        PopulationTier populationTier,
        int actualSpawnedCount,
        IEnumerable<int>? memberIds,
        DateTime startedAt,
        DateTime completedAt,
        double? scpCombatEquivalentAtCompletion = null)
    {
        WaveId = string.IsNullOrWhiteSpace(waveId) ? string.Empty : waveId;
        Faction = string.IsNullOrWhiteSpace(faction) ? string.Empty : faction;
        PopulationTier = populationTier;
        ActualSpawnedCount = Math.Max(0, actualSpawnedCount);
        StartedAt = startedAt;
        CompletedAt = completedAt;
        ScpCombatEquivalentAtCompletion = scpCombatEquivalentAtCompletion;
        List<int> copiedIds = memberIds is null ? new List<int>() : new List<int>(memberIds);
        MemberIds = copiedIds.AsReadOnly();
    }

    public string WaveId { get; }

    public string Faction { get; }

    public PopulationTier PopulationTier { get; }

    public int ActualSpawnedCount { get; }

    public IReadOnlyList<int> MemberIds { get; }

    public DateTime StartedAt { get; }

    public DateTime CompletedAt { get; }

    public double? ScpCombatEquivalentAtCompletion { get; }

    public bool IsPostMajorWavePublished { get; private set; }

    public bool IsTimerExtensionProcessed { get; private set; }

    public bool IsSurvivalObservationComplete { get; private set; }

    public int SurvivingCountAtObservation { get; private set; }

    public DateTime? SurvivalObservedAt { get; private set; }

    internal bool TryMarkPostMajorWavePublished()
    {
        if (IsPostMajorWavePublished)
        {
            return false;
        }

        IsPostMajorWavePublished = true;
        return true;
    }

    public bool TryMarkTimerExtensionProcessed()
    {
        if (IsTimerExtensionProcessed)
        {
            return false;
        }

        IsTimerExtensionProcessed = true;
        return true;
    }

    public bool TryCompleteSurvivalObservation(int survivingCount, DateTime observedAt)
    {
        if (IsSurvivalObservationComplete)
        {
            return false;
        }

        SurvivingCountAtObservation = Math.Min(
            ActualSpawnedCount,
            Math.Max(0, survivingCount));
        SurvivalObservedAt = observedAt;
        IsSurvivalObservationComplete = true;
        return true;
    }

    public MajorWaveSnapshot ToSnapshot()
    {
        return new MajorWaveSnapshot(
            Faction,
            ActualSpawnedCount,
            SurvivingCountAtObservation,
            IsSurvivalObservationComplete,
            baseFailureScore: 0d,
            isCatastrophic: IsSurvivalObservationComplete && SurvivingCountAtObservation == 0,
            startedAt: StartedAt,
            evaluatedAt: SurvivalObservedAt,
            memberIds: MemberIds,
            completedAt: CompletedAt,
            scpCombatEquivalentAtCompletion: ScpCombatEquivalentAtCompletion,
            faction: Faction);
    }
}

/// <summary>
/// Module 02 对外发布的 Primary Wave 完成事件。
/// </summary>
public sealed class MajorWaveCompletedEvent
{
    public MajorWaveCompletedEvent(long roundId, MajorWaveRecord record)
    {
        RoundId = roundId;
        WaveId = record?.WaveId ?? string.Empty;
        Faction = record?.Faction ?? string.Empty;
        PopulationTier = record?.PopulationTier ?? PopulationTier.E;
        ActualSpawnedCount = record?.ActualSpawnedCount ?? 0;
        CompletedAt = record?.CompletedAt ?? default(DateTime);
    }

    public long RoundId { get; }

    public string WaveId { get; }

    public string Faction { get; }

    public PopulationTier PopulationTier { get; }

    public int ActualSpawnedCount { get; }

    public DateTime CompletedAt { get; }
}

/// <summary>
/// 保存 Current、Last、Previous 和完整波次历史的回合内存储。
/// </summary>
public sealed class MajorWaveHistory
{
    private readonly List<MajorWaveRecord> records = new List<MajorWaveRecord>();

    public MajorWaveHistory(int capacity = 256)
    {
        Capacity = Math.Max(2, capacity);
    }

    public int Capacity { get; }

    public int Count => records.Count;

    public MajorWaveRecord? CurrentWave { get; private set; }

    public MajorWaveRecord? LastMajorWave { get; private set; }

    public MajorWaveRecord? PreviousMajorWave { get; private set; }

    public IReadOnlyList<MajorWaveRecord> Records => records.AsReadOnly();

    public MajorWaveRecord Record(
        string waveId,
        string faction,
        PopulationTier populationTier,
        int actualSpawnedCount,
        IEnumerable<int>? memberIds,
        DateTime startedAt,
        DateTime completedAt,
        double? scpCombatEquivalentAtCompletion = null)
    {
        foreach (MajorWaveRecord existingRecord in records)
        {
            if (string.Equals(existingRecord.WaveId, waveId, StringComparison.Ordinal))
            {
                return existingRecord;
            }
        }

        MajorWaveRecord record = new MajorWaveRecord(
            waveId,
            faction,
            populationTier,
            actualSpawnedCount,
            memberIds,
            startedAt,
            completedAt,
            scpCombatEquivalentAtCompletion);
        PreviousMajorWave = LastMajorWave;
        LastMajorWave = record;
        CurrentWave = record;
        records.Add(record);
        if (records.Count > Capacity)
        {
            records.RemoveRange(0, records.Count - Capacity);
        }
        return record;
    }

    public bool TryMarkPostMajorWavePublished(MajorWaveRecord? record)
    {
        return record is not null && records.Contains(record) && record.TryMarkPostMajorWavePublished();
    }

    public IReadOnlyList<MajorWaveSnapshot> GetSnapshots()
    {
        List<MajorWaveSnapshot> snapshots = new List<MajorWaveSnapshot>(records.Count);
        foreach (MajorWaveRecord record in records)
        {
            snapshots.Add(record.ToSnapshot());
        }

        return snapshots.AsReadOnly();
    }

    public void Clear()
    {
        records.Clear();
        CurrentWave = null;
        LastMajorWave = null;
        PreviousMajorWave = null;
    }
}
