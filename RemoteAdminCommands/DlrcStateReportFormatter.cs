using System;
using System.Collections.Generic;
using System.Text;
using EmergencyEvents.Crisis;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 将最近一次 D-LRC 评估整理为 Remote Admin 可读状态报告。
/// </summary>
public static class DlrcStateReportFormatter
{
    public static string Format(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisAssessment? assessment)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ResponseBreakdown score = result.ResponseBreakdown;
        ControlAssessment control = result.ControlAssessment;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[EmergencyEvents][D-LRC State] 最近一次评估快照");
        builder.AppendLine($"EvaluationAt={result.Timestamp:O}; RoundElapsed={snapshot.RoundElapsedTime}; RoundId={snapshot.RoundId}");
        builder.AppendLine($"PopulationTier={snapshot.PopulationTier}; RoundStartPopulation={snapshot.RoundStartPopulation}; CurrentOnlinePlayers={snapshot.CurrentOnlinePlayers}");
        builder.AppendLine($"Personnel: FoundationCombatants={snapshot.FoundationCombatants}; ChaosCombatants={snapshot.ChaosCombatants}; OtherHostileCombatants={snapshot.OtherHostileCombatants}; ClassDAlive={snapshot.ClassDAlive}; ScientistsAlive={snapshot.ScientistsAlive}; EligibleSpectators={snapshot.EligibleSpectators}; OverwatchCount={snapshot.OverwatchCount}");
        builder.AppendLine($"SCP: StartingScpCount={snapshot.StartingScpCount}; MainScpAlive={snapshot.MainScpAlive}; Scp0492Count={snapshot.Scp0492Count}; Scp079Present={snapshot.Scp079Present}; Scp079Tier={snapshot.Scp079Tier}; WarheadUnlocked={snapshot.WarheadUnlocked}; WarheadActive={snapshot.WarheadActive}; WarheadDetonated={snapshot.WarheadDetonated}; WarheadCancellationCount={snapshot.WarheadCancellationCount}");
        builder.AppendLine($"ResponseCode={result.Code}; NaturalResponseScore={result.NaturalResponseScore:0.####}; PersistentAdjustment={result.PersistentAdjustment:0.####}; EffectiveResponseScore={result.EffectiveResponseScore:0.####}; TheoreticalLevel={result.TheoreticalLevel}; FinalLevel={result.FinalLevel}; ControlState={result.ControlState}; Valid={result.IsValid}");
        builder.AppendLine($"Score: ScpPresence={score.ScpPresence:0.####}; ScpHealth={score.ScpHealth:0.####}; ZombiePressure={score.ZombiePressure:0.####}; Scp079Pressure={score.Scp079Pressure:0.####}; ScpThreatTotal={score.ScpThreatTotal:0.####}; FoundationCombatShare={score.FoundationCombatShare:0.####}; ScpCombatEquivalent={score.ScpCombatEquivalent:0.####}; CombatTotal={score.CombatTotal:0.####}; CombatPressure={score.CombatPressure:0.####}; SpectatorRatio={score.SpectatorRatio:0.####}; SpectatorPressure={score.SpectatorPressure:0.####}; FoundationPressureTotal={score.FoundationPressureTotal:0.####}; ReinforcementFailure={score.ReinforcementFailure:0.####}; TimePressure={score.TimePressure:0.####}; StrategicHazard={score.StrategicHazard:0.####}");
        builder.AppendLine($"Control: ThreatTrend={control.ThreatTrend}; ThreatDelta={control.ThreatDelta:0.####}; FiveMinutesAgoThreat={FormatNullable(control.FiveMinutesAgoThreat)}; FoundationStrength={control.FoundationStrength}; WavePerformance={control.WavePerformance}; BattlefieldMomentum={control.BattlefieldMomentum}; PositiveSignals={control.PositiveSignals}; NegativeSignals={control.NegativeSignals}; ControlLevelCap={control.ControlLevelCap}");
        AppendCrisis(builder, assessment);
        return builder.ToString().TrimEnd();
    }

    private static void AppendCrisis(StringBuilder builder, CrisisAssessment? assessment)
    {
        if (assessment is null)
        {
            builder.Append("CrisisCode=UNAVAILABLE; ActiveTags=; Reason=CrisisSystemDisabledOrNoAssessment");
            return;
        }

        builder.AppendLine($"CrisisCode={assessment.Code}; ActiveTags={string.Join("+", assessment.ActiveTags)}; EvaluationId={assessment.EvaluationId}; Trigger={assessment.Trigger}");
        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            if (!assessment.Detections.TryGetValue(tag, out CrisisDetectionResult? detection))
            {
                continue;
            }

            builder.Append($"{tag}=Active:{detection.IsActive};Reason={detection.Reason};Metrics=");
            AppendMetrics(builder, detection.Metrics);
            builder.AppendLine();
        }
    }

    private static void AppendMetrics(StringBuilder builder, IReadOnlyDictionary<string, double> metrics)
    {
        bool isFirst = true;
        foreach (KeyValuePair<string, double> metric in metrics)
        {
            if (!isFirst)
            {
                builder.Append(',');
            }

            builder.Append($"{metric.Key}={metric.Value:0.####}");
            isFirst = false;
        }
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.####") : "null";
    }
}
