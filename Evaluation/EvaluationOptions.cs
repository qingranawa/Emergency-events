using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// Evaluator 的固定默认选项和安全归一化入口。
/// </summary>
public sealed class EvaluationOptions
{
    private static readonly double[] DefaultEThresholds = { 0d, 18d, 32d, 48d, 65d, 82d };
    private static readonly double[] DefaultDThresholds = { 0d, 20d, 34d, 50d, 67d, 84d };
    private static readonly double[] DefaultCThresholds = { 0d, 22d, 36d, 52d, 69d, 86d };
    private static readonly double[] DefaultBThresholds = { 0d, 24d, 38d, 54d, 71d, 88d };
    private static readonly double[] DefaultAThresholds = { 0d, 26d, 40d, 56d, 73d, 90d };

    public EvaluationOptions(
        int zombieFullPressureCount = 6,
        int threatTrendWindowSeconds = 300,
        int momentumWindowSeconds = 120,
        double warheadCancelScore = 5d,
        double warheadCancelMaxScore = 10d,
        int evaluationStartTimeSeconds = 391,
        int evaluationIntervalSeconds = 30,
        int historyCapacity = 20,
        IEnumerable<double>? eThresholds = null,
        IEnumerable<double>? dThresholds = null,
        IEnumerable<double>? cThresholds = null,
        IEnumerable<double>? bThresholds = null,
        IEnumerable<double>? aThresholds = null)
    {
        ZombieFullPressureCount = NormalizePositiveInt(zombieFullPressureCount, 6);
        ThreatTrendWindowSeconds = NormalizePositiveInt(threatTrendWindowSeconds, 300);
        MomentumWindowSeconds = NormalizePositiveInt(momentumWindowSeconds, 120);
        WarheadCancelScore = NormalizeScore(warheadCancelScore, 5d);
        WarheadCancelMaxScore = NormalizeScore(warheadCancelMaxScore, 10d);
        if (WarheadCancelMaxScore < WarheadCancelScore)
        {
            WarheadCancelMaxScore = WarheadCancelScore;
        }

        EvaluationStartTimeSeconds = NormalizePositiveInt(evaluationStartTimeSeconds, 391);
        EvaluationIntervalSeconds = NormalizePositiveInt(evaluationIntervalSeconds, 30);
        HistoryCapacity = NormalizePositiveInt(historyCapacity, 20);
        EThresholds = NormalizeThresholds(eThresholds, DefaultEThresholds);
        DThresholds = NormalizeThresholds(dThresholds, DefaultDThresholds);
        CThresholds = NormalizeThresholds(cThresholds, DefaultCThresholds);
        BThresholds = NormalizeThresholds(bThresholds, DefaultBThresholds);
        AThresholds = NormalizeThresholds(aThresholds, DefaultAThresholds);
    }

    public static EvaluationOptions Default { get; } = new EvaluationOptions();

    public int ZombieFullPressureCount { get; }

    public int ThreatTrendWindowSeconds { get; }

    public int MomentumWindowSeconds { get; }

    public double WarheadCancelScore { get; }

    public double WarheadCancelMaxScore { get; }

    public int EvaluationStartTimeSeconds { get; }

    public int EvaluationIntervalSeconds { get; }

    public int HistoryCapacity { get; }

    public IReadOnlyList<double> EThresholds { get; }

    public IReadOnlyList<double> DThresholds { get; }

    public IReadOnlyList<double> CThresholds { get; }

    public IReadOnlyList<double> BThresholds { get; }

    public IReadOnlyList<double> AThresholds { get; }

    public IReadOnlyList<double> GetThresholds(PopulationTier tier)
    {
        return tier switch
        {
            PopulationTier.A => AThresholds,
            PopulationTier.B => BThresholds,
            PopulationTier.C => CThresholds,
            PopulationTier.D => DThresholds,
            _ => EThresholds,
        };
    }

    private static int NormalizePositiveInt(int value, int fallback)
    {
        return value > 0 ? value : fallback;
    }

    private static double NormalizeScore(double value, double fallback)
    {
        return EvaluationValueNormalizer.IsFinite(value) && value >= 0d
            ? value
            : fallback;
    }

    private static IReadOnlyList<double> NormalizeThresholds(
        IEnumerable<double>? values,
        IReadOnlyList<double> fallback)
    {
        if (values is null)
        {
            return CloneThresholds(fallback);
        }

        List<double> candidate = new List<double>(values);
        if (candidate.Count != 6 || !IsValidThresholdSequence(candidate))
        {
            return CloneThresholds(fallback);
        }

        return new ReadOnlyCollection<double>(candidate);
    }

    private static bool IsValidThresholdSequence(IReadOnlyList<double> values)
    {
        double previous = -1d;
        for (int index = 0; index < values.Count; index++)
        {
            double current = values[index];
            if (!EvaluationValueNormalizer.IsFinite(current) || current < 0d || current < previous)
            {
                return false;
            }

            previous = current;
        }

        return true;
    }

    private static IReadOnlyList<double> CloneThresholds(IReadOnlyList<double> values)
    {
        double[] copy = new double[values.Count];
        for (int index = 0; index < values.Count; index++)
        {
            copy[index] = values[index];
        }

        return new ReadOnlyCollection<double>(copy);
    }
}
