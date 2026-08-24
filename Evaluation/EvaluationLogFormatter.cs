using System;
using System.Collections.Generic;
using System.Text;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 生成不改变游戏状态的评估审计文本。
/// </summary>
public static class EvaluationLogFormatter
{
    public static string FormatSnapshot(RoundSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"RoundId={snapshot.RoundId}; Timestamp={snapshot.Timestamp:O}; RoundElapsed={snapshot.RoundElapsedTime}; PopulationTier={snapshot.PopulationTier}; RoundStartPopulation={snapshot.RoundStartPopulation}; CurrentOnlinePlayers={snapshot.CurrentOnlinePlayers}; FoundationCombatants={snapshot.FoundationCombatants}; ChaosCombatants={snapshot.ChaosCombatants}; OtherHostileCombatants={snapshot.OtherHostileCombatants}; ClassDAlive={snapshot.ClassDAlive}; ScientistsAlive={snapshot.ScientistsAlive}; EligibleSpectators={snapshot.EligibleSpectators}; OverwatchCount={snapshot.OverwatchCount}; MainScpAlive={snapshot.MainScpAlive}; StartingScpCount={snapshot.StartingScpCount}; Scp0492Count={snapshot.Scp0492Count}; Scp079Present={snapshot.Scp079Present}; Scp079Tier={snapshot.Scp079Tier}; Scp079TierIsValid={snapshot.Scp079TierIsValid}; WarheadUnlocked={snapshot.WarheadUnlocked}; WarheadActive={snapshot.WarheadActive}; WarheadDetonated={snapshot.WarheadDetonated}; WarheadDetonatedAt={snapshot.WarheadDetonatedAt:O}; WarheadCancellationCount={snapshot.WarheadCancellationCount}; HostileThirdPartyActive={snapshot.HostileThirdPartyActive}; HostileThirdPartyCombatants={snapshot.HostileThirdPartyCombatants}; SurfaceFoundationCombatants={snapshot.SurfaceFoundationCombatants}; SurfaceChaosCombatants={snapshot.SurfaceChaosCombatants}; SurfaceMainScp={snapshot.SurfaceMainScp}; SurfaceOtherHostiles={snapshot.SurfaceOtherHostiles}; MajorWaveHistoryCount={snapshot.MajorWaveHistory.Count}; RecentFoundationDeaths120s={snapshot.RecentFoundationDeaths120s}; RecentHostileDeaths120s={snapshot.RecentHostileDeaths120s}; RecentMainScpDeaths120s={snapshot.RecentMainScpDeaths120s}; ActivePlayerIds=");
        AppendPlayerIds(builder, snapshot.ActivePlayerIds);
        builder.Append("; ScpStates=");
        AppendScpStates(builder, snapshot.ScpStates);
        builder.Append("; MajorWaveHistory=");
        AppendMajorWaves(builder, snapshot.MajorWaveHistory);
        return builder.ToString();
    }

    public static string FormatActivation(
        int startTimeSeconds,
        int intervalSeconds,
        double initialDelaySeconds)
    {
        return $"D-LRC EVALUATOR ACTIVATED; FirstEvaluationAt={startTimeSeconds}s; Interval={intervalSeconds}s; InitialDelay={initialDelaySeconds:0.###}s";
    }

    public static string FormatDetailed(DlrcEvaluationResult result, long roundId)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        ResponseBreakdown breakdown = result.ResponseBreakdown;
        ControlAssessment control = result.ControlAssessment;
        return $"RoundId={roundId}; Code={result.Code}; PopulationTier={result.PopulationTier}; NaturalResponseScore={result.NaturalResponseScore:0.####}; EffectiveResponseScore={result.EffectiveResponseScore:0.####}; ScpPresence={breakdown.ScpPresence:0.####}; ScpHealth={breakdown.ScpHealth:0.####}; ZombiePressure={breakdown.ZombiePressure:0.####}; Scp079Pressure={breakdown.Scp079Pressure:0.####}; SCPThreat={breakdown.ScpThreatTotal:0.####}; ScpThreatTotal={breakdown.ScpThreatTotal:0.####}; FoundationCombatShare={breakdown.FoundationCombatShare:0.####}; ScpCombatEquivalent={breakdown.ScpCombatEquivalent:0.####}; CombatTotal={breakdown.CombatTotal:0.####}; CombatPressure={breakdown.CombatPressure:0.####}; SpectatorRatio={breakdown.SpectatorRatio:0.####}; SpectatorPressure={breakdown.SpectatorPressure:0.####}; FoundationPressure={breakdown.FoundationPressureTotal:0.####}; FoundationPressureTotal={breakdown.FoundationPressureTotal:0.####}; ReinforcementFailure={breakdown.ReinforcementFailure:0.####}; EvaluatedWaveSurvivalRatio={FormatNullableDouble(breakdown.EvaluatedWaveSurvivalRatio)}; EvaluatedWaveBaseFailure={FormatNullableDouble(breakdown.EvaluatedWaveBaseFailure)}; PreviousEvaluatedWaveBaseFailure={FormatNullableDouble(breakdown.PreviousEvaluatedWaveBaseFailure)}; EvaluatedWaveStartingCount={FormatNullableInt(breakdown.EvaluatedWaveStartingCount)}; EvaluatedWaveSurvivingCount={FormatNullableInt(breakdown.EvaluatedWaveSurvivingCount)}; TimePressure={breakdown.TimePressure:0.####}; StrategicHazard={breakdown.StrategicHazard:0.####}; NaturalTotal={breakdown.NaturalTotal:0.####}; PersistentAdjustment={breakdown.PersistentAdjustment:0.####}; EffectiveTotal={breakdown.EffectiveTotal:0.####}; TheoreticalLevel={result.TheoreticalLevel}; ThreatTrend={control.ThreatTrend}; ThreatDelta={control.ThreatDelta:0.####}; FiveMinutesAgoThreat={FormatNullableDouble(control.FiveMinutesAgoThreat)}; FoundationStrength={control.FoundationStrength}; WavePerformance={control.WavePerformance}; BattlefieldMomentum={control.BattlefieldMomentum}; PositiveSignals={control.PositiveSignals}; NegativeSignals={control.NegativeSignals}; CollapseConditionA={control.CollapseConditionA}; CollapseConditionB={control.CollapseConditionB}; CollapseConditionC={control.CollapseConditionC}; ControlState={result.ControlState}; ControlLevelCap={control.ControlLevelCap}; FinalLevel={result.FinalLevel}; Valid={result.IsValid}";
    }

    public static string FormatChange(
        DlrcEvaluationResult? previous,
        DlrcEvaluationResult current)
    {
        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        string previousCode = previous?.Code ?? "NONE";
        string previousControl = previous?.ControlState.ToString() ?? "NONE";
        return $"PreviousCode={previousCode}; CurrentCode={current.Code}; PreviousControl={previousControl}; CurrentControl={current.ControlState}; EffectiveResponseScore={current.EffectiveResponseScore:0.####}";
    }

    private static void AppendPlayerIds(StringBuilder builder, IReadOnlyList<int> playerIds)
    {
        for (int index = 0; index < playerIds.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(playerIds[index]);
        }
    }

    private static void AppendScpStates(StringBuilder builder, IReadOnlyList<ScpSnapshot> scpStates)
    {
        for (int index = 0; index < scpStates.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('|');
            }

            ScpSnapshot scp = scpStates[index];
            builder.Append($"[Role={scp.RoleName};Alive={scp.IsAlive};CurrentHP={scp.CurrentHealth:0.####};MaxHP={scp.MaxHealth:0.####};CurrentHume={scp.CurrentHume:0.####};MaxHume={scp.MaxHume:0.####};IsScp079={scp.IsScp079};HealthDataUnavailable={scp.IsHealthDataUnavailable}]");
        }
    }

    private static void AppendMajorWaves(StringBuilder builder, IReadOnlyList<MajorWaveSnapshot> waves)
    {
        for (int index = 0; index < waves.Count; index++)
        {
            if (index > 0)
            {
                builder.Append('|');
            }

            MajorWaveSnapshot wave = waves[index];
            builder.Append($"[Name={wave.Name};StartingCount={wave.StartingCount};SurvivingCount={wave.SurvivingCountAtEvaluation};IsEvaluationComplete={wave.IsEvaluationComplete};BaseFailureScore={wave.BaseFailureScore:0.####};IsCatastrophic={wave.IsCatastrophic};StartedAt={wave.StartedAt:O};EvaluatedAt={FormatNullableDateTime(wave.EvaluatedAt)}]");
        }
    }

    private static string FormatNullableDouble(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.####") : "null";
    }

    private static string FormatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "null";
    }

    private static string FormatNullableDateTime(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("O") : "null";
    }
}
