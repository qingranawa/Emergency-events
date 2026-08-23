namespace EmergencyEvents.Evaluation;

/// <summary>
/// 现场控制状态。
/// </summary>
public enum ControlState
{
    ADVANTAGE,
    CONTROLLED,
    UNCONTROLLED,
    COLLAPSE,
}

/// <summary>
/// SCP 威胁趋势。
/// </summary>
public enum ThreatTrend
{
    INSUFFICIENT,
    IMPROVING,
    WORSENING,
    STALLED_HIGH,
    STABLE,
}

/// <summary>
/// 基金会战力强度。
/// </summary>
public enum FoundationStrength
{
    STRONG,
    ADEQUATE,
    WEAK,
    CRITICAL,
}

/// <summary>
/// 大型支援表现。
/// </summary>
public enum WavePerformance
{
    GOOD,
    NEUTRAL,
    POOR,
    CATASTROPHIC,
}

/// <summary>
/// 最近战场动量方向。
/// </summary>
public enum BattlefieldMomentum
{
    FOUNDATION_POSITIVE,
    FOUNDATION_NEGATIVE,
    NEUTRAL,
}
