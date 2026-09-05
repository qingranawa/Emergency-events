using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.Reinforcement;

namespace EmergencyEvents.Telemetry;

/// <summary>
/// 平衡数据 observer。它只读取官方评估、危机、波次和 FDI 结果，不参与 Gameplay。
/// </summary>
public sealed class BalanceTelemetryService
{
    public const int SchemaVersion = 1;
    private readonly BalanceTelemetryConfig config;
    private readonly string outputDirectory;
    private readonly List<TelemetrySample> samples = new List<TelemetrySample>();
    private readonly List<double> fdiSamples = new List<double>();
    private readonly List<double> waitSamples = new List<double>();
    private readonly Dictionary<int, double> finalLevelSeconds = new Dictionary<int, double>();
    private readonly Dictionary<int, double> theoreticalLevelSeconds = new Dictionary<int, double>();
    private DateTime? roundStartedAt;
    private DateTime? lastPeriodicAt;
    private int peakOnline;
    private int minimumOnline;
    private int maxFinalLevel;
    private int maxTheoreticalLevel;
    private double initialFdi;
    private double minFdi;
    private double maxFdi;
    private bool hasFdiSample;
    private int recoveryCount;
    private double recoveryTotal;
    private int spectatorWaitSampleCount;
    private int? lastPeriodicFinalLevel;
    private int? lastPeriodicTheoreticalLevel;

    public BalanceTelemetryService(BalanceTelemetryConfig? config = null, string? outputDirectory = null)
    {
        this.config = config ?? new BalanceTelemetryConfig();
        this.config.Validate();
        this.outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "telemetry");
    }

    public bool Enabled => config.Enabled;

    public string OutputDirectory => outputDirectory;

    public int CurrentRecordCount => samples.Count;

    public int CurrentEvaluationSamples => samples.Count(sample => sample.RecordType == "DLRC_EVALUATION");

    public int CurrentSpectatorWaitSamples => waitSamples.Count;

    public int CurrentFdiSampleCount => fdiSamples.Count;

    public string? LastWriteError { get; private set; }

    public void StartRound(long roundId, DateTime timestamp, int startingPopulation)
    {
        ClearRound();
        roundStartedAt = timestamp;
        minimumOnline = startingPopulation;
        peakOnline = startingPopulation;
    }

    public void RecordEvaluation(DlrcEvaluationCompletedEvent completedEvent, CrisisAssessment? assessment, double fdi, FacilityDisorderBand fdiBand, string facilityState)
    {
        if (!Enabled || completedEvent is null || !completedEvent.Result.IsValid || !config.WriteEvaluationRecords)
        {
            return;
        }

        RoundSnapshot snapshot = completedEvent.Snapshot;
        DlrcEvaluationResult result = completedEvent.Result;
        Record("DLRC_EVALUATION", snapshot.RoundId, snapshot.Timestamp, new Dictionary<string, string>
        {
            ["evaluationId"] = completedEvent.EvaluationId.ToString(CultureInfo.InvariantCulture),
            ["trigger"] = completedEvent.Trigger.ToString(),
            ["elapsedSeconds"] = snapshot.RoundElapsedTime.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            ["lockedPopulationTier"] = result.PopulationTier.ToString(),
            ["startingPopulation"] = snapshot.RoundStartPopulation.ToString(CultureInfo.InvariantCulture),
            ["currentOnline"] = snapshot.CurrentOnlinePlayers.ToString(CultureInfo.InvariantCulture),
            ["foundationCombatants"] = snapshot.FoundationCombatants.ToString(CultureInfo.InvariantCulture),
            ["chaosCombatants"] = snapshot.ChaosCombatants.ToString(CultureInfo.InvariantCulture),
            ["hostileHumanCombatants"] = snapshot.OtherHostileCombatants.ToString(CultureInfo.InvariantCulture),
            ["mainScpAlive"] = snapshot.MainScpAlive.ToString(CultureInfo.InvariantCulture),
            ["startingScpCount"] = snapshot.StartingScpCount.ToString(CultureInfo.InvariantCulture),
            ["zombieCount"] = snapshot.Scp0492Count.ToString(CultureInfo.InvariantCulture),
            ["scpThreat"] = result.ResponseBreakdown.ScpThreatTotal.ToString("0.####", CultureInfo.InvariantCulture),
            ["foundationPressure"] = result.ResponseBreakdown.FoundationPressureTotal.ToString("0.####", CultureInfo.InvariantCulture),
            ["reinforcementFailure"] = result.ResponseBreakdown.ReinforcementFailure.ToString("0.####", CultureInfo.InvariantCulture),
            ["timePressure"] = result.ResponseBreakdown.TimePressure.ToString("0.####", CultureInfo.InvariantCulture),
            ["strategicHazard"] = result.ResponseBreakdown.StrategicHazard.ToString("0.####", CultureInfo.InvariantCulture),
            ["naturalScore"] = result.NaturalResponseScore.ToString("0.####", CultureInfo.InvariantCulture),
            ["effectiveScore"] = result.EffectiveResponseScore.ToString("0.####", CultureInfo.InvariantCulture),
            ["theoreticalLevel"] = result.TheoreticalLevel.ToString(CultureInfo.InvariantCulture),
            ["controlState"] = result.ControlState.ToString(),
            ["controlStateCap"] = result.ControlAssessment.ControlLevelCap.ToString(CultureInfo.InvariantCulture),
            ["finalLevel"] = result.FinalLevel.ToString(CultureInfo.InvariantCulture),
            ["activeCrisisTags"] = assessment is null ? string.Empty : string.Join(",", assessment.ActiveTags),
            ["fdi"] = fdi.ToString("0.####", CultureInfo.InvariantCulture),
            ["fdiBand"] = fdiBand.ToString(),
            ["facilityState"] = facilityState,
        });

        maxFinalLevel = Math.Max(maxFinalLevel, result.FinalLevel);
        maxTheoreticalLevel = Math.Max(maxTheoreticalLevel, result.TheoreticalLevel);
        peakOnline = Math.Max(peakOnline, snapshot.CurrentOnlinePlayers);
        minimumOnline = Math.Min(minimumOnline, snapshot.CurrentOnlinePlayers);
        double safeFdi = IsFinite(fdi) ? Math.Max(0d, Math.Min(100d, fdi)) : 0d;
        AddMetricSample(fdiSamples, safeFdi);
        if (!hasFdiSample)
        {
            initialFdi = safeFdi;
            minFdi = safeFdi;
            maxFdi = safeFdi;
            hasFdiSample = true;
        }
        else
        {
            minFdi = Math.Min(minFdi, safeFdi);
            maxFdi = Math.Max(maxFdi, safeFdi);
        }
        if (completedEvent.Trigger == DlrcEvaluationTrigger.PERIODIC)
        {
            AddLevelDuration(completedEvent.Snapshot.Timestamp);
            lastPeriodicFinalLevel = result.FinalLevel;
            lastPeriodicTheoreticalLevel = result.TheoreticalLevel;
            lastPeriodicAt = snapshot.Timestamp;
        }
    }

    public void RecordSpectatorWait(long roundId, string roundLocalSpectatorId, double waitSeconds, string source, bool isCensored)
    {
        if (!Enabled || !config.TrackSpectatorWait || string.IsNullOrWhiteSpace(roundLocalSpectatorId))
        {
            return;
        }

        double safeWait = Math.Max(0d, waitSeconds);
        if (!isCensored)
        {
            AddMetricSample(waitSamples, safeWait);
            spectatorWaitSampleCount++;
        }

        Record("SPECTATOR_WAIT", roundId, DateTime.UtcNow, new Dictionary<string, string>
        {
            ["roundLocalSpectatorId"] = roundLocalSpectatorId,
            ["waitSeconds"] = isCensored ? string.Empty : safeWait.ToString("0.###", CultureInfo.InvariantCulture),
            ["respawnSource"] = source ?? string.Empty,
            ["status"] = isCensored ? (source ?? "CENSORED") : "COMPLETED",
        });
    }

    public void RecordSettlement(FacilityDisorderSettlement settlement, long roundId, DateTime timestamp, string? activeCrises)
    {
        if (!Enabled)
        {
            return;
        }

        recoveryCount += settlement.OrderRecoveryDelta < 0d ? 1 : 0;
        recoveryTotal += settlement.OrderRecoveryDelta;
        Record("FDI_SETTLEMENT", roundId, timestamp, new Dictionary<string, string>
        {
            ["beforeFdi"] = settlement.PreviousValue.ToString("0.####", CultureInfo.InvariantCulture),
            ["ordinaryDelta"] = settlement.RecentTransientDelta.ToString("0.####", CultureInfo.InvariantCulture),
            ["orderRecoveryDelta"] = settlement.OrderRecoveryDelta.ToString("0.####", CultureInfo.InvariantCulture),
            ["afterFdi"] = settlement.CurrentValue.ToString("0.####", CultureInfo.InvariantCulture),
            ["eventsProcessedCount"] = settlement.ProcessedEvents.Count.ToString(CultureInfo.InvariantCulture),
            ["recoveryApplied"] = (settlement.OrderRecoveryDelta < 0d).ToString(),
            ["recoveryResult"] = settlement.RecoveryResult,
            ["activeCrises"] = activeCrises ?? string.Empty,
        });
    }

    public void RecordWave(long roundId, MajorWaveRecord record)
    {
        if (!Enabled || !config.WriteWaveRecords || record is null)
        {
            return;
        }

        Record("PRIMARY_WAVE", roundId, record.CompletedAt, new Dictionary<string, string>
        {
            ["waveId"] = record.WaveId,
            ["faction"] = record.Faction,
            ["startedAt"] = record.StartedAt.ToString("O"),
            ["completedAt"] = record.CompletedAt.ToString("O"),
            ["actualSpawned"] = record.ActualSpawnedCount.ToString(CultureInfo.InvariantCulture),
            ["matureAt"] = record.SurvivalObservedAt?.ToString("O") ?? string.Empty,
            ["survivalCount120s"] = record.IsSurvivalObservationComplete ? record.SurvivingCountAtObservation.ToString(CultureInfo.InvariantCulture) : string.Empty,
        });
    }

    public void RecordWaveMaturity(long roundId, MajorWaveRecord record)
    {
        if (!Enabled
            || !config.WriteWaveRecords
            || record is null
            || !record.IsSurvivalObservationComplete
            || !record.SurvivalObservedAt.HasValue)
        {
            return;
        }

        Record("PRIMARY_WAVE_MATURED", roundId, record.SurvivalObservedAt.Value, new Dictionary<string, string>
        {
            ["waveId"] = record.WaveId,
            ["faction"] = record.Faction,
            ["startedAt"] = record.StartedAt.ToString("O"),
            ["completedAt"] = record.CompletedAt.ToString("O"),
            ["actualSpawned"] = record.ActualSpawnedCount.ToString(CultureInfo.InvariantCulture),
            ["matureAt"] = record.SurvivalObservedAt.Value.ToString("O"),
            ["survivalCount120s"] = record.SurvivingCountAtObservation.ToString(CultureInfo.InvariantCulture),
        });
    }

    public void RecordCrisis(CrisisAssessment assessment)
    {
        if (!Enabled || assessment is null)
        {
            return;
        }

        foreach (CrisisTag tag in assessment.ActivatedTags)
        {
            Record("CRISIS_TRANSITION", assessment.Snapshot.RoundId, assessment.Snapshot.Timestamp, new Dictionary<string, string>
            {
                ["evaluationId"] = assessment.EvaluationId.ToString(CultureInfo.InvariantCulture),
                ["tag"] = tag.ToString(),
                ["transition"] = "ACTIVATED",
                ["episodeId"] = assessment.TryGetEpisodeId(tag, out long episodeId) ? episodeId.ToString(CultureInfo.InvariantCulture) : string.Empty,
            });
        }

        foreach (CrisisTag tag in assessment.ResolvedTags)
        {
            Record("CRISIS_TRANSITION", assessment.Snapshot.RoundId, assessment.Snapshot.Timestamp, new Dictionary<string, string>
            {
                ["evaluationId"] = assessment.EvaluationId.ToString(CultureInfo.InvariantCulture),
                ["tag"] = tag.ToString(),
                ["transition"] = "RESOLVED",
            });
        }
    }

    public void CompleteRound(long roundId, DateTime timestamp, string? lockedTier, int startingPopulation)
    {
        if (!Enabled || !config.WriteRoundSummary || !config.FlushOnRoundEnd)
        {
            ClearRound();
            return;
        }

        AddLevelDuration(timestamp);
        double duration = roundStartedAt.HasValue ? Math.Max(0d, (timestamp - roundStartedAt.Value).TotalSeconds) : 0d;
        double? waitMean = waitSamples.Count == 0 ? (double?)null : waitSamples.Average();
        double? waitMedian = Percentile(waitSamples, 0.5d);
        double? waitP90 = Percentile(waitSamples, 0.9d);
        Record("ROUND_BALANCE_SUMMARY", roundId, timestamp, new Dictionary<string, string>
        {
            ["lockedTier"] = lockedTier ?? string.Empty,
            ["startingPopulation"] = startingPopulation.ToString(CultureInfo.InvariantCulture),
            ["roundDurationSeconds"] = duration.ToString("0.###", CultureInfo.InvariantCulture),
            ["peakOnline"] = peakOnline.ToString(CultureInfo.InvariantCulture),
            ["minimumOnline"] = minimumOnline.ToString(CultureInfo.InvariantCulture),
            ["maxFinalLevel"] = maxFinalLevel.ToString(CultureInfo.InvariantCulture),
            ["maxTheoreticalLevel"] = maxTheoreticalLevel.ToString(CultureInfo.InvariantCulture),
            ["initialFdi"] = initialFdi.ToString("0.####", CultureInfo.InvariantCulture),
            ["minFdi"] = minFdi.ToString("0.####", CultureInfo.InvariantCulture),
            ["maxFdi"] = maxFdi.ToString("0.####", CultureInfo.InvariantCulture),
            ["recoveryAppliedCount"] = recoveryCount.ToString(CultureInfo.InvariantCulture),
            ["recoveryTotalDelta"] = recoveryTotal.ToString("0.####", CultureInfo.InvariantCulture),
            ["evaluationSamples"] = CurrentEvaluationSamples.ToString(CultureInfo.InvariantCulture),
            ["waitSamples"] = spectatorWaitSampleCount.ToString(CultureInfo.InvariantCulture),
            ["waitMeanSeconds"] = FormatNullable(waitMean),
            ["waitMedianSeconds"] = FormatNullable(waitMedian),
            ["waitP90Seconds"] = FormatNullable(waitP90),
            ["l0Seconds"] = GetDuration(finalLevelSeconds, 0),
            ["l1Seconds"] = GetDuration(finalLevelSeconds, 1),
            ["l2Seconds"] = GetDuration(finalLevelSeconds, 2),
            ["l3Seconds"] = GetDuration(finalLevelSeconds, 3),
            ["l4Seconds"] = GetDuration(finalLevelSeconds, 4),
            ["l5Seconds"] = GetDuration(finalLevelSeconds, 5),
        });
        ClearRound();
    }

    public void ClearRound()
    {
        samples.Clear();
        fdiSamples.Clear();
        waitSamples.Clear();
        roundStartedAt = null;
        lastPeriodicAt = null;
        peakOnline = 0;
        minimumOnline = 0;
        maxFinalLevel = 0;
        maxTheoreticalLevel = 0;
        initialFdi = 0d;
        minFdi = 0d;
        maxFdi = 0d;
        hasFdiSample = false;
        recoveryCount = 0;
        recoveryTotal = 0d;
        spectatorWaitSampleCount = 0;
        finalLevelSeconds.Clear();
        theoreticalLevelSeconds.Clear();
        lastPeriodicFinalLevel = null;
        lastPeriodicTheoreticalLevel = null;
    }

    private void Record(string recordType, long roundId, DateTime timestamp, IReadOnlyDictionary<string, string> fields)
    {
        Dictionary<string, string> allFields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> field in fields)
        {
            allFields[field.Key] = field.Value;
        }

        allFields["schemaVersion"] = SchemaVersion.ToString(CultureInfo.InvariantCulture);
        allFields["recordType"] = recordType;
        allFields["timestamp"] = timestamp.ToUniversalTime().ToString("O");
        allFields["roundId"] = roundId.ToString(CultureInfo.InvariantCulture);
        samples.Add(new TelemetrySample(recordType, Serialize(allFields)));
        while (samples.Count > config.RecentRecordCapacity)
        {
            samples.RemoveAt(0);
        }
        TryAppend(recordType, Serialize(allFields));
    }

    private void TryAppend(string recordType, string line)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            string fileName = recordType == "ROUND_BALANCE_SUMMARY"
                ? $"round-summary-{DateTime.UtcNow:yyyy-MM-dd}.jsonl"
                : $"balance-{DateTime.UtcNow:yyyy-MM-dd}.jsonl";
            File.AppendAllText(Path.Combine(outputDirectory, fileName), line + Environment.NewLine);
            LastWriteError = null;
        }
        catch (Exception exception)
        {
            LastWriteError = exception.GetType().Name + ": " + exception.Message;
        }
    }

    private void AddLevelDuration(DateTime timestamp)
    {
        if (!lastPeriodicAt.HasValue)
        {
            return;
        }

        double seconds = Math.Max(0d, (timestamp - lastPeriodicAt.Value).TotalSeconds);
        if (lastPeriodicFinalLevel.HasValue)
        {
            AddDuration(finalLevelSeconds, lastPeriodicFinalLevel.Value, seconds);
        }

        if (lastPeriodicTheoreticalLevel.HasValue)
        {
            AddDuration(theoreticalLevelSeconds, lastPeriodicTheoreticalLevel.Value, seconds);
        }
    }

    private static void AddDuration(Dictionary<int, double> durations, int level, double seconds)
    {
        durations[level] = durations.TryGetValue(level, out double current) ? current + seconds : seconds;
    }

    private void AddMetricSample(List<double> samples, double value)
    {
        samples.Add(value);
        while (samples.Count > config.RecentRecordCapacity)
        {
            samples.RemoveAt(0);
        }
    }

    private static string GetDuration(Dictionary<int, double> durations, int level)
    {
        return durations.TryGetValue(level, out double value) ? value.ToString("0.###", CultureInfo.InvariantCulture) : "0";
    }

    private static double? Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return null;
        }

        double[] sorted = values.OrderBy(value => value).ToArray();
        int rank = (int)Math.Ceiling(percentile * sorted.Length);
        return sorted[Math.Max(0, Math.Min(sorted.Length - 1, rank - 1))];
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string Serialize(IReadOnlyDictionary<string, string> fields)
    {
        return "{" + string.Join(",", fields.Select(pair => "\"" + Escape(pair.Key) + "\":\"" + Escape(pair.Value) + "\"")) + "}";
    }

    private static string Escape(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u");
                        builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private sealed class TelemetrySample
    {
        public TelemetrySample(string recordType, string payload)
        {
            RecordType = recordType;
            Payload = payload;
        }

        public string RecordType { get; }

        public string Payload { get; }
    }
}
