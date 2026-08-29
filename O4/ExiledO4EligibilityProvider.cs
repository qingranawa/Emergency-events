using Exiled.API.Features;
using PlayerRoles;

namespace EmergencyEvents.O4;

/// <summary>
/// 使用 EXILED 9.14.2 真实 Role API 判断 O4 资格。
/// </summary>
public sealed class ExiledO4EligibilityProvider : IO4EligibilityProvider
{
    public bool IsEligible(Player player)
    {
        if (player is null || !player.IsConnected)
        {
            return false;
        }

        RoleTypeId role = player.Role.Type;
        return role == RoleTypeId.Spectator
            || role == RoleTypeId.Overwatch
            || player.IsOverwatchEnabled;
    }
}
