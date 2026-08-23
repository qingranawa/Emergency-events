using System;
using System.Collections.Generic;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 计算纯逻辑 Response Score。
/// </summary>
public static class ResponseScoreCalculator
{
    public static ResponseScoreResult Calculate(
        RoundSnapshot snapshot,
        EvaluationOptions options,
        double persistentAdjustment = 0d)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        ScpScore scpScore = CalculateScpThreat(snapshot, options);
        FoundationScore foundationScore = CalculateFoundationPressure(snapshot);

        WaveScore waveScore = CalculateReinforcementFailure(snapshot);
        double timePressure = CalculateTimePressure(snapshot);
        double strategicHazard = CalculateStrategicHazard(snapshot, options);

        double naturalTotal = scpScore.Total
            + foundationScore.Total
            + waveScore.Failure
            + timePressure
            + strategicHazard;
        double effectiveTotal = Clamp(naturalTotal + persistentAdjustment, 0d, 100d);

        ResponseBreakdown breakdown = new ResponseBreakdown(
            scpScore.Presence,
            scpScore.Health,
            scpScore.ZombiePressure,
            scpScore.Scp079Pressure,
            scpScore.Total,
            foundationScore.CombatShare,
            foundationScore.ScpCombatEquivalent,
            foundationScore.CombatTotal,
            foundationScore.CombatPressure,
            foundationScore.SpectatorRatio,
            foundationScore.SpectatorPressure,
            foundationScore.Total,
            waveScore.Failure,
            timePressure,
            strategicHazard,
            naturalTotal,
            persistentAdjustment,
            effectiveTotal,
            waveScore.SurvivalRatio,
            waveScore.BaseFailure,
            waveScore.PreviousBaseFailure,
            waveScore.StartingCount,
            waveScore.SurvivingCount);

        return new ResponseScoreResult(
            breakdown,
            naturalTotal,
            persistentAdjustment,
            effectiveTotal);
    }

    private static ScpScore CalculateScpThreat(
        RoundSnapshot snapshot,
        EvaluationOptions options)
    {
        double presence = CalculateScpPresence(snapshot);
        double health = CalculateScpHealth(snapshot);
        double zombiePressure = CalculateZombiePressure(snapshot, options);
        double scp079Pressure = CalculateScp079Pressure(snapshot);
        double total = Clamp(
            presence + health + zombiePressure + scp079Pressure,
            0d,
            40d);
        return new ScpScore(
            presence,
            health,
            zombiePressure,
            scp079Pressure,
            total);
    }

    private static FoundationScore CalculateFoundationPressure(RoundSnapshot snapshot)
    {
        double scpCombatEquivalent = snapshot.MainScpAlive + snapshot.Scp0492Count / 3d;
        double combatTotal = snapshot.FoundationCombatants
            + snapshot.ChaosCombatants
            + snapshot.OtherHostileCombatants
            + scpCombatEquivalent;
        double combatShare = combatTotal <= 0d
            ? 1d
            : Clamp(snapshot.FoundationCombatants / combatTotal, 0d, 1d);
        double combatPressure = CalculateCombatPressure(combatShare);
        double spectatorRatio = snapshot.CurrentOnlinePlayers <= 0
            ? 0d
            : snapshot.EligibleSpectators / (double)snapshot.CurrentOnlinePlayers;
        double spectatorPressure = CalculateSpectatorPressure(spectatorRatio);
        double total = Clamp(combatPressure + spectatorPressure, 0d, 20d);
        return new FoundationScore(
            combatShare,
            scpCombatEquivalent,
            combatTotal,
            combatPressure,
            spectatorRatio,
            spectatorPressure,
            total);
    }

    private static double CalculateScpPresence(RoundSnapshot snapshot)
    {
        if (snapshot.StartingScpCount <= 0)
        {
            return 0d;
        }

        double ratio = snapshot.MainScpAlive / (double)snapshot.StartingScpCount;
        return Clamp(ratio, 0d, 1d) * 20d;
    }

    private static double CalculateScpHealth(RoundSnapshot snapshot)
    {
        if (snapshot.StartingScpCount <= 0)
        {
            return 0d;
        }

        double validHealthRatioTotal = 0d;
        foreach (ScpSnapshot scp in snapshot.ScpStates)
        {
            if (!scp.IsAlive || scp.IsScp079 || scp.IsHealthDataUnavailable)
            {
                continue;
            }

            double maximum = scp.MaxHealth + scp.MaxHume;
            double current = scp.CurrentHealth + scp.CurrentHume;
            if (!IsFinite(maximum) || maximum <= 0d || !IsFinite(current))
            {
                continue;
            }

            validHealthRatioTotal += Clamp(current / maximum, 0d, 1d);
        }

        return Clamp(
            validHealthRatioTotal / snapshot.StartingScpCount * 10d,
            0d,
            10d);
    }

    private static double CalculateZombiePressure(
        RoundSnapshot snapshot,
        EvaluationOptions options)
    {
        if (options.ZombieFullPressureCount <= 0)
        {
            return 0d;
        }

        double ratio = snapshot.Scp0492Count / (double)options.ZombieFullPressureCount;
        return Clamp(ratio, 0d, 1d) * 4d;
    }

    private static double CalculateScp079Pressure(RoundSnapshot snapshot)
    {
        if (!snapshot.Scp079Present)
        {
            return 0d;
        }

        return snapshot.Scp079Tier switch
        {
            2 => 1.5d,
            3 => 3d,
            4 => 4.5d,
            5 => 6d,
            _ => 0d,
        };
    }

    private static double CalculateCombatPressure(double foundationCombatShare)
    {
        if (foundationCombatShare >= 0.50d)
        {
            return 0d;
        }

        if (foundationCombatShare >= 0.40d)
        {
            return 3d;
        }

        if (foundationCombatShare >= 0.30d)
        {
            return 6d;
        }

        if (foundationCombatShare >= 0.20d)
        {
            return 10d;
        }

        if (foundationCombatShare >= 0.10d)
        {
            return 12d;
        }

        return 14d;
    }

    private static double CalculateSpectatorPressure(double spectatorRatio)
    {
        if (spectatorRatio < 0.10d)
        {
            return 0d;
        }

        if (spectatorRatio < 0.20d)
        {
            return 1d;
        }

        if (spectatorRatio < 0.30d)
        {
            return 2d;
        }

        if (spectatorRatio < 0.40d)
        {
            return 3d;
        }

        if (spectatorRatio < 0.50d)
        {
            return 4d;
        }

        return 6d;
    }

    private static WaveScore CalculateReinforcementFailure(RoundSnapshot snapshot)
    {
        List<MajorWaveSnapshot> completedWaves = GetCompletedWaves(snapshot);
        if (completedWaves.Count == 0)
        {
            return WaveScore.Empty;
        }

        MajorWaveSnapshot current = completedWaves[0];
        double currentSurvivalRatio = CalculateSurvivalRatio(current);
        double currentBaseFailure = CalculateBaseFailure(currentSurvivalRatio);
        double? previousBaseFailure = null;
        if (completedWaves.Count > 1)
        {
            double previousSurvivalRatio = CalculateSurvivalRatio(completedWaves[1]);
            previousBaseFailure = CalculateBaseFailure(previousSurvivalRatio);
        }

        double failure = currentBaseFailure;
        if (previousBaseFailure.HasValue
            && currentBaseFailure >= 8d
            && previousBaseFailure.Value >= 8d)
        {
            failure += 5d;
        }

        return new WaveScore(
            Clamp(failure, 0d, 20d),
            currentSurvivalRatio,
            currentBaseFailure,
            previousBaseFailure,
            current.StartingCount,
            current.SurvivingCountAtEvaluation);
    }

    private static List<MajorWaveSnapshot> GetCompletedWaves(RoundSnapshot snapshot)
    {
        List<MajorWaveSnapshot> completedWaves = new List<MajorWaveSnapshot>();
        foreach (MajorWaveSnapshot wave in snapshot.MajorWaveHistory)
        {
            if (wave.IsEvaluationComplete && wave.StartingCount > 0)
            {
                completedWaves.Add(wave);
            }
        }

        completedWaves.Sort((left, right) =>
            GetWaveSortTime(right).CompareTo(GetWaveSortTime(left)));
        if (completedWaves.Count > 2)
        {
            completedWaves.RemoveRange(2, completedWaves.Count - 2);
        }

        return completedWaves;
    }

    private static DateTime GetWaveSortTime(MajorWaveSnapshot wave)
    {
        return wave.EvaluatedAt ?? wave.StartedAt;
    }

    private static double CalculateSurvivalRatio(MajorWaveSnapshot wave)
    {
        return Clamp(
            wave.SurvivingCountAtEvaluation / (double)wave.StartingCount,
            0d,
            1d);
    }

    private static double CalculateBaseFailure(double survivalRatio)
    {
        if (survivalRatio > 0.75d)
        {
            return 0d;
        }

        if (survivalRatio > 0.50d)
        {
            return 4d;
        }

        if (survivalRatio > 0.25d)
        {
            return 8d;
        }

        if (survivalRatio > 0d)
        {
            return 12d;
        }

        return 15d;
    }

    private static double CalculateTimePressure(RoundSnapshot snapshot)
    {
        TimeSpan elapsed = snapshot.RoundElapsedTime;
        if (elapsed < TimeSpan.FromMinutes(10))
        {
            return 0d;
        }

        if (elapsed < TimeSpan.FromMinutes(15))
        {
            return 2d;
        }

        if (elapsed < TimeSpan.FromMinutes(20))
        {
            return 4d;
        }

        if (elapsed < TimeSpan.FromMinutes(25))
        {
            return 6d;
        }

        if (elapsed < TimeSpan.FromMinutes(30))
        {
            return 8d;
        }

        return 10d;
    }

    private static double CalculateStrategicHazard(
        RoundSnapshot snapshot,
        EvaluationOptions options)
    {
        double score = snapshot.WarheadCancellationCount * options.WarheadCancelScore;
        score = Math.Min(score, options.WarheadCancelMaxScore);
        return Clamp(score, 0d, 10d);
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (double.IsNaN(value))
        {
            return minimum;
        }

        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private readonly struct ScpScore
    {
        public ScpScore(
            double presence,
            double health,
            double zombiePressure,
            double scp079Pressure,
            double total)
        {
            Presence = presence;
            Health = health;
            ZombiePressure = zombiePressure;
            Scp079Pressure = scp079Pressure;
            Total = total;
        }

        public double Presence { get; }

        public double Health { get; }

        public double ZombiePressure { get; }

        public double Scp079Pressure { get; }

        public double Total { get; }
    }

    private readonly struct FoundationScore
    {
        public FoundationScore(
            double combatShare,
            double scpCombatEquivalent,
            double combatTotal,
            double combatPressure,
            double spectatorRatio,
            double spectatorPressure,
            double total)
        {
            CombatShare = combatShare;
            ScpCombatEquivalent = scpCombatEquivalent;
            CombatTotal = combatTotal;
            CombatPressure = combatPressure;
            SpectatorRatio = spectatorRatio;
            SpectatorPressure = spectatorPressure;
            Total = total;
        }

        public double CombatShare { get; }

        public double ScpCombatEquivalent { get; }

        public double CombatTotal { get; }

        public double CombatPressure { get; }

        public double SpectatorRatio { get; }

        public double SpectatorPressure { get; }

        public double Total { get; }
    }

    private readonly struct WaveScore
    {
        public static WaveScore Empty { get; } = new WaveScore(
            0d,
            null,
            null,
            null,
            null,
            null);

        public WaveScore(
            double failure,
            double? survivalRatio,
            double? baseFailure,
            double? previousBaseFailure,
            int? startingCount,
            int? survivingCount)
        {
            Failure = failure;
            SurvivalRatio = survivalRatio;
            BaseFailure = baseFailure;
            PreviousBaseFailure = previousBaseFailure;
            StartingCount = startingCount;
            SurvivingCount = survivingCount;
        }

        public double Failure { get; }

        public double? SurvivalRatio { get; }

        public double? BaseFailure { get; }

        public double? PreviousBaseFailure { get; }

        public int? StartingCount { get; }

        public int? SurvivingCount { get; }
    }
}

/// <summary>
/// Response Score 计算结果。
/// </summary>
public sealed class ResponseScoreResult
{
    internal ResponseScoreResult(
        ResponseBreakdown breakdown,
        double naturalResponseScore,
        double persistentAdjustment,
        double effectiveResponseScore)
    {
        Breakdown = breakdown;
        NaturalResponseScore = naturalResponseScore;
        PersistentAdjustment = persistentAdjustment;
        EffectiveResponseScore = effectiveResponseScore;
    }

    public ResponseBreakdown Breakdown { get; }

    public double NaturalResponseScore { get; }

    public double PersistentAdjustment { get; }

    public double EffectiveResponseScore { get; }
}
