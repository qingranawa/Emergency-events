using System;
using System.Diagnostics;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// EmergencyEvents 的 Remote Admin 根命令。
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class EmergencyEventsCommand : ICommand
{
    public string Command => "EmergencyEvents";

    public string[] Aliases => new[] { "ee" };

    public string Description => "EmergencyEvents 管理命令。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string[] values = new string[arguments.Count];
        if (arguments.Array is not null)
        {
            Array.Copy(arguments.Array, arguments.Offset, values, 0, arguments.Count);
        }

        if (!EmergencyEventsCommandSyntax.TryParse(values, out EmergencyEventsCommandRequest request))
        {
            response = "未知 EmergencyEvents 命令。使用 ee help 查看可用命令。";
            return false;
        }

        Plugin? plugin = Plugin.Instance;
        if (plugin is null)
        {
            response = "EmergencyEvents 插件当前未启用。";
            return false;
        }

        if (!sender.CheckPermission("emergencyevents.ra"))
        {
            response = "你没有 emergencyevents.ra 权限。";
            return false;
        }

        if (RequiresDebugPermission(request.Kind)
            && (!sender.CheckPermission("emergencyevents.ra.debug")
                || (IsTestCommand(request.Kind) && !plugin.DebugCommandsEnabled)))
        {
            response = IsTestCommand(request.Kind) && !plugin.DebugCommandsEnabled
                ? "DebugCommandsEnabled=false，ee test 当前已关闭。"
                : "你没有 emergencyevents.ra.debug 权限。";
            return false;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        bool succeeded = plugin.TryExecuteRemoteAdminCommand(request, out response);
        stopwatch.Stop();
        LogCommand(request.Kind, plugin.Runtime?.State, succeeded, stopwatch.ElapsedMilliseconds);
        return succeeded;
    }

    private static bool RequiresDebugPermission(EmergencyEventsCommandKind kind)
    {
        return IsTestCommand(kind) || kind == EmergencyEventsCommandKind.DlrcStageRaw;
    }

    private static bool IsTestCommand(EmergencyEventsCommandKind kind)
    {
        return kind is EmergencyEventsCommandKind.TestCrisisAll
            or EmergencyEventsCommandKind.TestCrisisCheck
            or EmergencyEventsCommandKind.TestCrisisBioZombies
            or EmergencyEventsCommandKind.TestCrisisSysTier
            or EmergencyEventsCommandKind.TestCrisisSec
            or EmergencyEventsCommandKind.TestCrisisWar
            or EmergencyEventsCommandKind.TestCrisisConCheckpoint
            or EmergencyEventsCommandKind.TestCrisisConCheckpointCommit
            or EmergencyEventsCommandKind.TestCrisisEndCheck
            or EmergencyEventsCommandKind.TestCrisisEndSimulate
            or EmergencyEventsCommandKind.TestCleanupVerify;
    }

    private static void LogCommand(
        EmergencyEventsCommandKind kind,
        Runtime.PluginRuntimeState? runtimeState,
        bool succeeded,
        long elapsedMilliseconds)
    {
        string message = $"[EmergencyEvents][RA] Command={kind}; RuntimeState={runtimeState?.ToString() ?? "ERROR"}; Result={(succeeded ? "Success" : "Rejected")}; DurationMs={elapsedMilliseconds}";
        if (kind is EmergencyEventsCommandKind.Enable
            or EmergencyEventsCommandKind.Disable
            or EmergencyEventsCommandKind.TestCrisisConCheckpointCommit)
        {
            Log.Info(message);
            return;
        }

        Log.Debug(message);
    }
}
