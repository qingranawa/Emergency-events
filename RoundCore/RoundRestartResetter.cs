using System;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 在原生强制重启回合前，按固定顺序清理上一局的插件状态。
/// </summary>
public static class RoundRestartResetter
{
    public static void Reset(
        Action<string> cleanupDlrc,
        Action cleanupReinforcement,
        Action cleanupRoundCore)
    {
        cleanupDlrc("RestartingRound");
        cleanupReinforcement();
        cleanupRoundCore();
    }
}
