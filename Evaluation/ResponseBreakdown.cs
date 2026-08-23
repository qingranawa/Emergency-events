namespace EmergencyEvents.Evaluation;

/// <summary>
/// Response Score 的分项明细和审计数据。
/// </summary>
public sealed class ResponseBreakdown
{
    internal ResponseBreakdown(
        double scpPresence,
        double scpHealth,
        double zombiePressure,
        double scp079Pressure,
        double scpThreatTotal,
        double foundationCombatShare,
        double scpCombatEquivalent,
        double combatTotal,
        double combatPressure,
        double spectatorRatio,
        double spectatorPressure,
        double foundationPressureTotal,
        double reinforcementFailure,
        double timePressure,
        double strategicHazard,
        double naturalTotal,
        double persistentAdjustment,
        double effectiveTotal,
        double? evaluatedWaveSurvivalRatio,
        double? evaluatedWaveBaseFailure,
        double? previousEvaluatedWaveBaseFailure,
        int? evaluatedWaveStartingCount,
        int? evaluatedWaveSurvivingCount)
    {
        ScpPresence = scpPresence;
        ScpHealth = scpHealth;
        ZombiePressure = zombiePressure;
        Scp079Pressure = scp079Pressure;
        ScpThreatTotal = scpThreatTotal;
        FoundationCombatShare = foundationCombatShare;
        ScpCombatEquivalent = scpCombatEquivalent;
        CombatTotal = combatTotal;
        CombatPressure = combatPressure;
        SpectatorRatio = spectatorRatio;
        SpectatorPressure = spectatorPressure;
        FoundationPressureTotal = foundationPressureTotal;
        ReinforcementFailure = reinforcementFailure;
        TimePressure = timePressure;
        StrategicHazard = strategicHazard;
        NaturalTotal = naturalTotal;
        PersistentAdjustment = persistentAdjustment;
        EffectiveTotal = effectiveTotal;
        EvaluatedWaveSurvivalRatio = evaluatedWaveSurvivalRatio;
        EvaluatedWaveBaseFailure = evaluatedWaveBaseFailure;
        PreviousEvaluatedWaveBaseFailure = previousEvaluatedWaveBaseFailure;
        EvaluatedWaveStartingCount = evaluatedWaveStartingCount;
        EvaluatedWaveSurvivingCount = evaluatedWaveSurvivingCount;
    }

    public double ScpPresence { get; }

    public double ScpHealth { get; }

    public double ZombiePressure { get; }

    public double Scp079Pressure { get; }

    public double ScpThreatTotal { get; }

    public double FoundationCombatShare { get; }

    public double ScpCombatEquivalent { get; }

    public double CombatTotal { get; }

    public double CombatPressure { get; }

    public double SpectatorRatio { get; }

    public double SpectatorPressure { get; }

    public double FoundationPressureTotal { get; }

    public double ReinforcementFailure { get; }

    public double TimePressure { get; }

    public double StrategicHazard { get; }

    public double NaturalTotal { get; }

    public double PersistentAdjustment { get; }

    public double EffectiveTotal { get; }

    public double? EvaluatedWaveSurvivalRatio { get; }

    public double? EvaluatedWaveBaseFailure { get; }

    public double? PreviousEvaluatedWaveBaseFailure { get; }

    public int? EvaluatedWaveStartingCount { get; }

    public int? EvaluatedWaveSurvivingCount { get; }
}
