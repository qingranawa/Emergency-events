using System;
using System.Collections.Generic;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 动量统计使用的死亡类别。
/// </summary>
public enum BattlefieldDeathCategory
{
    Foundation,
    HostileHuman,
    MainScp,
}

/// <summary>
/// 最近窗口内的死亡动量快照。
/// </summary>
public sealed class BattlefieldMomentumSnapshot
{
    public BattlefieldMomentumSnapshot(
        int foundationDeaths,
        int hostileHumanDeaths,
        int mainScpDeaths)
    {
        FoundationDeaths = Math.Max(0, foundationDeaths);
        HostileHumanDeaths = Math.Max(0, hostileHumanDeaths);
        MainScpDeaths = Math.Max(0, mainScpDeaths);
    }

    public int FoundationDeaths { get; }

    public int HostileHumanDeaths { get; }

    public int MainScpDeaths { get; }
}

/// <summary>
/// 保存当前回合最近一段时间的死亡动量，不保存跨回合状态。
/// </summary>
public sealed class BattlefieldMomentumTracker
{
    private readonly List<DeathRecord> records = new List<DeathRecord>();

    public void RecordDeath(DateTime timestamp, BattlefieldDeathCategory category)
    {
        if (!Enum.IsDefined(typeof(BattlefieldDeathCategory), category))
        {
            return;
        }

        records.Add(new DeathRecord(timestamp, category));
    }

    public BattlefieldMomentumSnapshot GetSnapshot(DateTime now, int windowSeconds)
    {
        int normalizedWindowSeconds = Math.Max(1, windowSeconds);
        DateTime cutoff = now.AddSeconds(-normalizedWindowSeconds);
        int foundationDeaths = 0;
        int hostileHumanDeaths = 0;
        int mainScpDeaths = 0;

        for (int index = records.Count - 1; index >= 0; index--)
        {
            DeathRecord record = records[index];
            if (record.Timestamp < cutoff)
            {
                records.RemoveAt(index);
                continue;
            }

            if (record.Timestamp > now)
            {
                continue;
            }

            switch (record.Category)
            {
                case BattlefieldDeathCategory.Foundation:
                    foundationDeaths++;
                    break;
                case BattlefieldDeathCategory.HostileHuman:
                    hostileHumanDeaths++;
                    break;
                case BattlefieldDeathCategory.MainScp:
                    mainScpDeaths++;
                    break;
            }
        }

        return new BattlefieldMomentumSnapshot(
            foundationDeaths,
            hostileHumanDeaths,
            mainScpDeaths);
    }

    public void Clear()
    {
        records.Clear();
    }

    private readonly struct DeathRecord
    {
        public DeathRecord(DateTime timestamp, BattlefieldDeathCategory category)
        {
            Timestamp = timestamp;
            Category = category;
        }

        public DateTime Timestamp { get; }

        public BattlefieldDeathCategory Category { get; }
    }
}
