using System;
using System.Collections.Generic;
using Exiled.API.Enums;
using EmergencyEvents.Evaluation;
using MEC;

namespace EmergencyEvents.Reinforcement;

/// <summary>
/// 一次实际刷新的正常大型支援波次记录。
/// </summary>
public sealed class MajorWaveRecord
{
    public MajorWaveRecord(
        string? name,
        int startingCount,
        IEnumerable<int>? memberIds,
        DateTime startedAt)
    {
        Name = name ?? string.Empty;
        StartingCount = startingCount;
        MemberIds = memberIds is null
            ? new HashSet<int>()
            : new HashSet<int>(memberIds);
        StartedAt = startedAt;
    }

    public string Name { get; }

    public int StartingCount { get; }

    public HashSet<int> MemberIds { get; }

    public DateTime StartedAt { get; }

    public DateTime? EvaluatedAt { get; set; }

    public int SurvivingCountAtEvaluation { get; set; }

    public double BaseFailureScore { get; set; }

    public bool IsEvaluationComplete { get; set; }

    public bool IsCatastrophic { get; set; }

    public string EvaluationReason { get; set; } = string.Empty;

    public MajorWaveSnapshot ToSnapshot()
    {
        return new MajorWaveSnapshot(
            Name,
            StartingCount,
            SurvivingCountAtEvaluation,
            IsEvaluationComplete,
            BaseFailureScore,
            IsCatastrophic,
            StartedAt,
            EvaluatedAt,
            MemberIds);
    }
}

/// <summary>
/// 第一正常大波的生命周期。
/// </summary>
public enum FirstWaveState
{
    NotReady,
    WaitingForObservers,
    Requested,
    Skipped,
    Completed,
}

/// <summary>
/// 一局普通支援调度所需的可审计状态。
/// </summary>
public sealed class ReinforcementState
{
    public ReinforcementState(long roundId)
    {
        RoundId = roundId;
        StartedAtUtc = DateTime.UtcNow;
        FirstWaveState = FirstWaveState.NotReady;
    }

    public long RoundId { get; }

    public DateTime StartedAtUtc { get; }

    public bool IsActive { get; set; } = true;

    public int FoundationSupportScore { get; set; }

    public int ChaosSupportScore { get; set; }

    public FirstWaveState FirstWaveState { get; set; }

    public float NextNormalWaveDueSeconds { get; set; }

    public bool HasFirstWaveFaction { get; set; }

    public SpawnableFaction FirstWaveFaction { get; set; } = SpawnableFaction.None;

    public bool FirstWaveSelectionHandled { get; set; }

    public bool FirstWaveRespawnStarted { get; set; }

    public bool PluginWaveRequestPending { get; set; }

    public bool PluginWaveInProgress { get; set; }

    public bool ManualWaveInProgress { get; set; }

    public SpawnableFaction? RequestedWaveFaction { get; set; }

    public int SupportCycleCount { get; set; }

    public SpawnableFaction? PendingWaveFaction { get; set; }

    public bool PendingWaveIsMini { get; set; }

    public int PendingWavePlayerCount { get; set; }

    public DateTime? LastWaveStartedAtUtc { get; set; }

    public string? LastWaveName { get; set; }

    public List<MajorWaveRecord> MajorWaveHistory { get; } = new List<MajorWaveRecord>();

    public HashSet<int> PendingWavePlayerIds { get; } = new HashSet<int>();

    public List<CoroutineHandle> ScheduledHandles { get; } = new List<CoroutineHandle>();

    public HashSet<int> ScoredEscapePlayerIds { get; } = new HashSet<int>();
}
