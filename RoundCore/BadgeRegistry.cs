using System.Collections.Generic;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 保存本回合由 Round Core 接管过的玩家原始 Badge。
/// </summary>
public sealed class BadgeRegistry
{
    private readonly Dictionary<int, string?> originalBadges = new Dictionary<int, string?>();

    public void Remember(int playerId, string? originalBadge)
    {
        if (!originalBadges.ContainsKey(playerId))
        {
            originalBadges[playerId] = originalBadge;
        }
    }

    public bool TryGet(int playerId, out string? originalBadge)
    {
        return originalBadges.TryGetValue(playerId, out originalBadge);
    }

    public void Remove(int playerId)
    {
        originalBadges.Remove(playerId);
    }

    public IReadOnlyList<KeyValuePair<int, string?>> Snapshot()
    {
        return new List<KeyValuePair<int, string?>>(originalBadges).AsReadOnly();
    }

    public void Clear()
    {
        originalBadges.Clear();
    }
}
