using System;
using System.Collections.Generic;
using Exiled.API.Enums;

namespace EmergencyEvents.Reinforcement;

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

    public int SupportCycleCount { get; set; }

    public SpawnableFaction? PendingWaveFaction { get; set; }

    public bool PendingWaveIsMini { get; set; }

    public int PendingWavePlayerCount { get; set; }

    public DateTime? LastWaveStartedAtUtc { get; set; }

    public string? LastWaveName { get; set; }

    public HashSet<int> ScoredEscapePlayerIds { get; } = new HashSet<int>();
}
