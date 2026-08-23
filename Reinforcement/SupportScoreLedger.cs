using System;
using System.Collections.Generic;

namespace EmergencyEvents.Reinforcement;

public enum SupportFaction
{
    None,
    Foundation,
    Chaos,
}

public enum SupportItemKind
{
    None,
    UniqueScp,
    ConsumableScp,
}

public readonly struct SupportThresholdAward
{
    public SupportThresholdAward(int thresholdPercent, SupportFaction faction, int score)
    {
        ThresholdPercent = thresholdPercent;
        Faction = faction;
        Score = score;
    }

    public int ThresholdPercent { get; }

    public SupportFaction Faction { get; }

    public int Score { get; }
}

/// <summary>
/// 记录一局中与原版 Influence Objectives 等价的 Support Score 事件，避免重复计分。
/// </summary>
public sealed class SupportScoreLedger
{
    private readonly Dictionary<string, ScpDamageState> scpDamage = new Dictionary<string, ScpDamageState>(StringComparer.Ordinal);
    private readonly HashSet<string> scoredScpDeaths = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<ushort> scoredItemInstances = new HashSet<ushort>();

    public int FoundationScore { get; private set; }

    public int ChaosScore { get; private set; }

    public bool TryScoreScpDeath(string scpId, SupportFaction faction, out int score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(scpId) || !scoredScpDeaths.Add(scpId))
        {
            return false;
        }

        score = AddScore(faction, 15);
        return score > 0;
    }

    public IReadOnlyList<SupportThresholdAward> RecordScpDamage(
        string scpId,
        double damage,
        double maxHealth,
        SupportFaction faction)
    {
        if (string.IsNullOrWhiteSpace(scpId) || damage <= 0d || maxHealth <= 0d)
        {
            return Array.Empty<SupportThresholdAward>();
        }

        if (!scpDamage.TryGetValue(scpId, out ScpDamageState? damageState))
        {
            damageState = new ScpDamageState();
            scpDamage.Add(scpId, damageState);
        }

        double previousDamage = damageState.TotalDamage;
        damageState.TotalDamage += damage;
        List<SupportThresholdAward> awards = new List<SupportThresholdAward>();
        int previousThreshold = (int)Math.Floor(previousDamage / maxHealth * 10d) * 10;
        int currentThreshold = Math.Min(100, (int)Math.Floor(damageState.TotalDamage / maxHealth * 10d) * 10);

        for (int threshold = Math.Max(10, previousThreshold + 10); threshold <= currentThreshold; threshold += 10)
        {
            if (!damageState.AwardedThresholds.Add(threshold))
            {
                continue;
            }

            int score = AddScore(faction, 2);
            awards.Add(new SupportThresholdAward(threshold, faction, score));
        }

        return awards.AsReadOnly();
    }

    public bool TryScoreItem(
        ushort itemInstanceId,
        SupportItemKind itemKind,
        SupportFaction faction,
        bool createdByScp914,
        out int score)
    {
        score = 0;
        if (itemInstanceId == 0
            || itemKind == SupportItemKind.None
            || createdByScp914
            || faction == SupportFaction.None
            || !scoredItemInstances.Add(itemInstanceId))
        {
            return false;
        }

        score = itemKind == SupportItemKind.UniqueScp ? 2 : 1;
        AddScore(faction, score);
        return true;
    }

    public void Clear()
    {
        FoundationScore = 0;
        ChaosScore = 0;
        scpDamage.Clear();
        scoredScpDeaths.Clear();
        scoredItemInstances.Clear();
    }

    private int AddScore(SupportFaction faction, int score)
    {
        if (score <= 0)
        {
            return 0;
        }

        if (faction == SupportFaction.Foundation)
        {
            FoundationScore += score;
            return score;
        }

        if (faction == SupportFaction.Chaos)
        {
            ChaosScore += score;
            return score;
        }

        return 0;
    }

    private sealed class ScpDamageState
    {
        public double TotalDamage { get; set; }

        public HashSet<int> AwardedThresholds { get; } = new HashSet<int>();
    }
}
