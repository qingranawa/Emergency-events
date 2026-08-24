using EmergencyEvents.Runtime;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 统一限制会改变或依赖有效回合的 RA 请求。
/// </summary>
public static class EmergencyEventsCommandGuard
{
    public static bool IsAllowed(EmergencyEventsCommandKind kind, PluginRuntimeState runtimeState)
    {
        if (IsAlwaysQueryable(kind) || IsRuntimeControl(kind) || IsDryRunTest(kind))
        {
            return true;
        }

        return runtimeState == PluginRuntimeState.ACTIVE;
    }

    public static bool RequiresActiveRound(EmergencyEventsCommandKind kind)
    {
        return !IsAlwaysQueryable(kind) && !IsRuntimeControl(kind) && !IsDryRunTest(kind);
    }

    private static bool IsAlwaysQueryable(EmergencyEventsCommandKind kind)
    {
        return kind is EmergencyEventsCommandKind.Help
            or EmergencyEventsCommandKind.Status
            or EmergencyEventsCommandKind.Version
            or EmergencyEventsCommandKind.Config
            or EmergencyEventsCommandKind.Health
            or EmergencyEventsCommandKind.Modules
            or EmergencyEventsCommandKind.ModuleDetail
            or EmergencyEventsCommandKind.Round
            or EmergencyEventsCommandKind.WaveState
            or EmergencyEventsCommandKind.WaveCurrent
            or EmergencyEventsCommandKind.WaveLast
            or EmergencyEventsCommandKind.WavePrevious
            or EmergencyEventsCommandKind.WaveHistory
            or EmergencyEventsCommandKind.WaveHistoryDetail
            or EmergencyEventsCommandKind.WaveTimers
            or EmergencyEventsCommandKind.WaveCap
            or EmergencyEventsCommandKind.WaveSurvival
            or EmergencyEventsCommandKind.DlrcState
            or EmergencyEventsCommandKind.DlrcBreakdown
            or EmergencyEventsCommandKind.DlrcControl
            or EmergencyEventsCommandKind.DlrcHistory
            or EmergencyEventsCommandKind.DlrcSnapshot
            or EmergencyEventsCommandKind.CrisisState
            or EmergencyEventsCommandKind.CrisisList
            or EmergencyEventsCommandKind.Cleanup;
    }

    private static bool IsRuntimeControl(EmergencyEventsCommandKind kind)
    {
        return kind is EmergencyEventsCommandKind.Enable or EmergencyEventsCommandKind.Disable;
    }

    private static bool IsDryRunTest(EmergencyEventsCommandKind kind)
    {
        return kind is EmergencyEventsCommandKind.TestCrisisAll
            or EmergencyEventsCommandKind.TestCrisisCheck
            or EmergencyEventsCommandKind.TestCrisisBioZombies
            or EmergencyEventsCommandKind.TestCrisisSysTier
            or EmergencyEventsCommandKind.TestCrisisSec
            or EmergencyEventsCommandKind.TestCrisisWar
            or EmergencyEventsCommandKind.TestCrisisConCheckpoint
            or EmergencyEventsCommandKind.TestCrisisEndCheck
            or EmergencyEventsCommandKind.TestCrisisEndSimulate
            or EmergencyEventsCommandKind.TestCleanupVerify;
    }
}
