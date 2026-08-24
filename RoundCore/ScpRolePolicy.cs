using System;
using System.Collections.Generic;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 从合法 SCP 候选池随机生成开局角色，不保证任何单一角色出现。
/// </summary>
public static class ScpRolePolicy
{
    public static List<T> BuildRoles<T>(
        int count,
        IReadOnlyList<T> pool,
        Random random)
    {
        if (count <= 0)
        {
            return new List<T>();
        }

        if (pool is null || pool.Count == 0)
        {
            throw new ArgumentException("SCP role pool cannot be empty.", nameof(pool));
        }

        List<T> roles = new List<T>(count);
        for (int index = 0; index < count; index++)
        {
            roles.Add(pool[random.Next(pool.Count)]);
        }

        return roles;
    }
}
