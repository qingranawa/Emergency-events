using System;
using CommandSystem;
using Exiled.API.Features;

namespace EmergencyEvents.O4;

/// <summary>
/// EXILED 9.14.2 ClientCommandHandler 的 O4 投票入口。
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
public sealed class O4VoteCommand : ICommand
{
    public string Command => "o4vote";

    public string[] Aliases => new[] { "eevote" };

    public string Description => "O4 事件选择投票。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 1 || !int.TryParse(arguments.Array?[arguments.Offset], out int candidateIndex))
        {
            response = "用法：o4vote <1|2>";
            return false;
        }

        Plugin? plugin = Plugin.Instance;
        Player? player = Player.Get(sender);
        if (plugin?.O4Panel is null || player is null)
        {
            response = "O4 面板当前不可用。";
            return false;
        }

        return plugin.O4Panel.TryCastVote(player, candidateIndex, out response);
    }
}
