using System;
using System.Collections.Generic;

namespace EmergencyEvents.RoundCore;

/// <summary>
/// 生成开局 SCP 角色池，避免高人口回合无条件塞入第二只 SCP-939。
/// </summary>
public static class ScpRolePolicy
{
    public static List<T> BuildRoles<T>(
        int count,
        IReadOnlyList<T> pool,
        T guaranteedRole,
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

        List<T> roles = new List<T>(count)
        {
            guaranteedRole,
        };

        List<T> remainingPool = new List<T>(pool.Count);
        for (int index = 0; index < pool.Count; index++)
        {
            if (!EqualityComparer<T>.Default.Equals(pool[index], guaranteedRole))
            {
                remainingPool.Add(pool[index]);
            }
        }

        Shuffle(remainingPool, random);
        for (int index = roles.Count; index < count; index++)
        {
            roles.Add(remainingPool[(index - 1) % remainingPool.Count]);
        }

        Shuffle(roles, random);
        return roles;
    }

    private static void Shuffle<T>(IList<T> values, Random random)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }
}
