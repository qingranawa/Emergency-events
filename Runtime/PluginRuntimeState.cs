namespace EmergencyEvents.Runtime;

/// <summary>
/// EmergencyEvents 在当前回合的统一运行状态。
/// </summary>
public enum PluginRuntimeState
{
    DISABLED,
    STANDBY,
    ACTIVE,
    LOW_POPULATION_SUSPENDED,
    ROUND_ENDED,
    ERROR,
}
