using System;
using CommandSystem;
using EmergencyEvents.Evaluation;

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

        if (!EmergencyEventsCommandSyntax.IsDlrcEvaluate(values)
            && !EmergencyEventsCommandSyntax.IsDlrcState(values))
        {
            response = "用法：ee dlrc evaluate | ee dlrc state";
            return false;
        }

        Plugin? plugin = Plugin.Instance;
        if (plugin is null)
        {
            response = "EmergencyEvents 插件当前未启用。";
            return false;
        }

        if (EmergencyEventsCommandSyntax.IsDlrcState(values))
        {
            return plugin.TryGetDlrcState(out response);
        }

        return plugin.TryEvaluateDlrcImmediately(out DlrcEvaluationResult? _, out response);
    }
}
