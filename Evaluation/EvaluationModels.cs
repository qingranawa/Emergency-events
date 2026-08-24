using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 一名 SCP 在评估时的纯逻辑状态。
/// </summary>
public sealed class ScpSnapshot
{
    public ScpSnapshot(
        string? roleName = null,
        bool isAlive = false,
        double currentHealth = 0d,
        double maxHealth = 0d,
        double currentHume = 0d,
        double maxHume = 0d,
        bool isScp079 = false,
        bool? healthDataUnavailable = null)
    {
        RoleName = EvaluationValueNormalizer.NormalizeName(roleName);
        IsAlive = isAlive;
        MaxHealth = EvaluationValueNormalizer.NormalizeNonNegativeDouble(maxHealth);
        CurrentHealth = EvaluationValueNormalizer.NormalizeHealth(currentHealth, MaxHealth);
        MaxHume = EvaluationValueNormalizer.NormalizeNonNegativeDouble(maxHume);
        CurrentHume = EvaluationValueNormalizer.NormalizeHealth(currentHume, MaxHume);
        IsScp079 = isScp079;
        IsHealthDataUnavailable = healthDataUnavailable
            ?? (MaxHealth <= 0d && MaxHume <= 0d);
    }

    public string RoleName { get; }

    public bool IsAlive { get; }

    public double CurrentHealth { get; }

    public double MaxHealth { get; }

    public double CurrentHume { get; }

    public double MaxHume { get; }

    public bool IsScp079 { get; }

    public bool IsHealthDataUnavailable { get; }

    public bool HealthDataUnavailable => IsHealthDataUnavailable;

    public bool IsHealthDataAvailable => !IsHealthDataUnavailable;
}

/// <summary>
/// 一次大型支援波次的纯逻辑评估快照。
/// </summary>
public sealed class MajorWaveSnapshot
{
    public MajorWaveSnapshot(
        string? name,
        int startingCount,
        int survivingCountAtEvaluation,
        bool isEvaluationComplete,
        double baseFailureScore,
        bool isCatastrophic,
        DateTime startedAt,
        DateTime? evaluatedAt = null,
        IEnumerable<int>? memberIds = null,
        DateTime? completedAt = null)
    {
        Name = EvaluationValueNormalizer.NormalizeName(name);
        StartingCount = EvaluationValueNormalizer.NormalizeNonNegativeInt(startingCount);
        SurvivingCountAtEvaluation = EvaluationValueNormalizer.NormalizeNonNegativeInt(survivingCountAtEvaluation);
        IsEvaluationComplete = isEvaluationComplete;
        BaseFailureScore = EvaluationValueNormalizer.NormalizeScore(baseFailureScore, 20d);
        IsCatastrophic = isCatastrophic;
        StartedAt = startedAt;
        CompletedAt = completedAt ?? startedAt;
        EvaluatedAt = evaluatedAt;
        MemberIds = EvaluationValueNormalizer.ClonePlayerIds(memberIds);
    }

    public string Name { get; }

    public int StartingCount { get; }

    public int StartCount => StartingCount;

    public int SurvivingCountAtEvaluation { get; }

    public int EvaluationSurvivingCount => SurvivingCountAtEvaluation;

    public bool IsEvaluationComplete { get; }

    public bool IsEvaluated => IsEvaluationComplete;

    public double BaseFailureScore { get; }

    public bool IsCatastrophic { get; }

    public DateTime StartedAt { get; }

    public DateTime StartTime => StartedAt;

    public DateTime CompletedAt { get; }

    public DateTime? EvaluatedAt { get; }

    public DateTime? EvaluationTime => EvaluatedAt;

    public IReadOnlyList<int> MemberIds { get; }
}

/// <summary>
/// 一次评估所使用的游戏状态快照。
/// </summary>
public sealed class RoundSnapshot
{
    public RoundSnapshot(
        long roundId,
        DateTime timestamp,
        TimeSpan roundElapsedTime,
        PopulationTier populationTier,
        int roundStartPopulation,
        int startingScpCount,
        int currentOnlinePlayers = 0,
        int foundationCombatants = 0,
        int chaosCombatants = 0,
        int otherHostileCombatants = 0,
        int classDAlive = 0,
        int scientistsAlive = 0,
        int eligibleSpectators = 0,
        int overwatchCount = 0,
        int mainScpAlive = 0,
        IEnumerable<ScpSnapshot>? scpStates = null,
        int scp0492Count = 0,
        bool scp079Present = false,
        int scp079Tier = 0,
        bool warheadUnlocked = false,
        bool warheadActive = false,
        bool warheadDetonated = false,
        int warheadCancellationCount = 0,
        IEnumerable<MajorWaveSnapshot>? majorWaveHistory = null,
        int recentFoundationDeaths120s = 0,
        int recentHostileDeaths120s = 0,
        int recentMainScpDeaths120s = 0,
        IEnumerable<int>? activePlayerIds = null,
        bool hostileThirdPartyActive = false,
        int hostileThirdPartyCombatants = 0,
        int surfaceFoundationCombatants = 0,
        int surfaceChaosCombatants = 0,
        int surfaceMainScp = 0,
        int surfaceOtherHostiles = 0,
        bool scp079TierIsValid = true)
    {
        RoundId = EvaluationValueNormalizer.NormalizeRoundId(roundId);
        Timestamp = timestamp;
        RoundElapsedTime = EvaluationValueNormalizer.NormalizeElapsedTime(roundElapsedTime);
        PopulationTier = EvaluationValueNormalizer.NormalizePopulationTier(populationTier);
        RoundStartPopulation = EvaluationValueNormalizer.NormalizeNonNegativeInt(roundStartPopulation);
        CurrentOnlinePlayers = EvaluationValueNormalizer.NormalizeNonNegativeInt(currentOnlinePlayers);
        FoundationCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(foundationCombatants);
        ChaosCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(chaosCombatants);
        OtherHostileCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(otherHostileCombatants);
        ClassDAlive = EvaluationValueNormalizer.NormalizeNonNegativeInt(classDAlive);
        ScientistsAlive = EvaluationValueNormalizer.NormalizeNonNegativeInt(scientistsAlive);
        EligibleSpectators = EvaluationValueNormalizer.NormalizeNonNegativeInt(eligibleSpectators);
        OverwatchCount = EvaluationValueNormalizer.NormalizeNonNegativeInt(overwatchCount);
        MainScpAlive = EvaluationValueNormalizer.NormalizeNonNegativeInt(mainScpAlive);
        StartingScpCount = EvaluationValueNormalizer.NormalizeNonNegativeInt(startingScpCount);
        ScpStates = EvaluationValueNormalizer.CloneReadOnlyList(scpStates);
        Scp0492Count = EvaluationValueNormalizer.NormalizeNonNegativeInt(scp0492Count);
        Scp079Present = scp079Present;
        Scp079TierIsValid = scp079Present
            && scp079TierIsValid
            && scp079Tier >= 0
            && scp079Tier <= 5;
        Scp079Tier = scp079Present
            ? EvaluationValueNormalizer.ClampInt(scp079Tier, 0, 5)
            : 0;
        WarheadUnlocked = warheadUnlocked;
        WarheadActive = warheadActive;
        WarheadDetonated = warheadDetonated;
        WarheadCancellationCount = EvaluationValueNormalizer.NormalizeNonNegativeInt(warheadCancellationCount);
        MajorWaveHistory = EvaluationValueNormalizer.CloneReadOnlyList(majorWaveHistory);
        RecentFoundationDeaths120s = EvaluationValueNormalizer.NormalizeNonNegativeInt(recentFoundationDeaths120s);
        RecentHostileDeaths120s = EvaluationValueNormalizer.NormalizeNonNegativeInt(recentHostileDeaths120s);
        RecentMainScpDeaths120s = EvaluationValueNormalizer.NormalizeNonNegativeInt(recentMainScpDeaths120s);
        ActivePlayerIds = EvaluationValueNormalizer.ClonePlayerIds(activePlayerIds);
        HostileThirdPartyActive = hostileThirdPartyActive;
        HostileThirdPartyCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(hostileThirdPartyCombatants);
        SurfaceFoundationCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(surfaceFoundationCombatants);
        SurfaceChaosCombatants = EvaluationValueNormalizer.NormalizeNonNegativeInt(surfaceChaosCombatants);
        SurfaceMainScp = EvaluationValueNormalizer.NormalizeNonNegativeInt(surfaceMainScp);
        SurfaceOtherHostiles = EvaluationValueNormalizer.NormalizeNonNegativeInt(surfaceOtherHostiles);
    }

    public long RoundId { get; }

    public DateTime Timestamp { get; }

    public TimeSpan RoundElapsedTime { get; }

    public PopulationTier PopulationTier { get; }

    public int RoundStartPopulation { get; }

    public int CurrentOnlinePlayers { get; }

    public int FoundationCombatants { get; }

    public int ChaosCombatants { get; }

    public int OtherHostileCombatants { get; }

    public int ClassDAlive { get; }

    public int ScientistsAlive { get; }

    public int EligibleSpectators { get; }

    public int OverwatchCount { get; }

    public int MainScpAlive { get; }

    public int StartingScpCount { get; }

    public IReadOnlyList<ScpSnapshot> ScpStates { get; }

    public int Scp0492Count { get; }

    public bool Scp079Present { get; }

    public int Scp079Tier { get; }

    public bool Scp079TierIsValid { get; }

    public bool WarheadUnlocked { get; }

    public bool WarheadActive { get; }

    public bool WarheadDetonated { get; }

    public int WarheadCancellationCount { get; }

    public IReadOnlyList<MajorWaveSnapshot> MajorWaveHistory { get; }

    public int RecentFoundationDeaths120s { get; }

    public int RecentHostileDeaths120s { get; }

    public int RecentMainScpDeaths120s { get; }

    public IReadOnlyList<int> ActivePlayerIds { get; }

    public IReadOnlyList<int> OnlineActivePlayerIds => ActivePlayerIds;

    public bool HostileThirdPartyActive { get; }

    public int HostileThirdPartyCombatants { get; }

    public int SurfaceFoundationCombatants { get; }

    public int SurfaceChaosCombatants { get; }

    public int SurfaceMainScp { get; }

    public int SurfaceOtherHostiles { get; }
}

internal static class EvaluationValueNormalizer
{
    public static long NormalizeRoundId(long value)
    {
        return value < 0L ? 0L : value;
    }

    public static int NormalizeNonNegativeInt(int value)
    {
        return value < 0 ? 0 : value;
    }

    public static double NormalizeNonNegativeDouble(double value)
    {
        return IsFinite(value) && value >= 0d ? value : 0d;
    }

    public static double NormalizeHealth(double value, double maximum)
    {
        double normalized = NormalizeNonNegativeDouble(value);
        return maximum > 0d ? Math.Min(normalized, maximum) : 0d;
    }

    public static double NormalizeScore(double value, double maximum)
    {
        double normalized = NormalizeNonNegativeDouble(value);
        return Math.Min(normalized, maximum);
    }

    public static string NormalizeName(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static TimeSpan NormalizeElapsedTime(TimeSpan value)
    {
        return value < TimeSpan.Zero ? TimeSpan.Zero : value;
    }

    public static PopulationTier NormalizePopulationTier(PopulationTier value)
    {
        return Enum.IsDefined(typeof(PopulationTier), value)
            ? value
            : PopulationTier.E;
    }

    public static int ClampInt(int value, int minimum, int maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    public static IReadOnlyList<T> CloneReadOnlyList<T>(IEnumerable<T>? values)
    {
        List<T> copy = values is null
            ? new List<T>()
            : new List<T>(values);
        return new ReadOnlyCollection<T>(copy);
    }

    public static IReadOnlyList<int> ClonePlayerIds(IEnumerable<int>? values)
    {
        List<int> copy = new List<int>();
        if (values is null)
        {
            return new ReadOnlyCollection<int>(copy);
        }

        HashSet<int> seen = new HashSet<int>();
        foreach (int value in values)
        {
            if (value >= 0 && seen.Add(value))
            {
                copy.Add(value);
            }
        }

        return new ReadOnlyCollection<int>(copy);
    }

    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
