using Exiled.API.Features;

namespace EmergencyEvents.O4;

/// <summary>
/// EXILED 玩家到 O4 资格事实的适配边界。
/// </summary>
public interface IO4EligibilityProvider
{
    bool IsEligible(Player player);
}
