using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 保存最近评估结果的内存 Ring Buffer。
/// </summary>
public sealed class EvaluationHistory
{
    private readonly int capacity;
    private readonly List<DlrcEvaluationResult> items = new List<DlrcEvaluationResult>();
    private readonly ReadOnlyCollection<DlrcEvaluationResult> readOnlyItems;

    public EvaluationHistory(int capacity = 20)
    {
        this.capacity = capacity > 0 ? capacity : 20;
        readOnlyItems = new ReadOnlyCollection<DlrcEvaluationResult>(items);
    }

    public int Count => items.Count;

    public IReadOnlyList<DlrcEvaluationResult> Items => readOnlyItems;

    public DlrcEvaluationResult? LatestValid
    {
        get
        {
            DlrcEvaluationResult? latest = null;
            foreach (DlrcEvaluationResult item in items)
            {
                if (item.IsValid && (latest is null || item.Timestamp >= latest.Timestamp))
                {
                    latest = item;
                }
            }

            return latest;
        }
    }

    public void Add(DlrcEvaluationResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }
        if (!result.IsValid)
        {
            return;
        }

        if (items.Count >= capacity)
        {
            items.RemoveAt(0);
        }

        items.Add(result);
    }

    public bool TryGetThreatAtOrBefore(
        DateTime target,
        out DlrcEvaluationResult? result)
    {
        DlrcEvaluationResult? nearest = null;
        foreach (DlrcEvaluationResult item in items)
        {
            if (!item.IsValid || item.Timestamp > target)
            {
                continue;
            }

            if (nearest is null || item.Timestamp >= nearest.Timestamp)
            {
                nearest = item;
            }
        }

        result = nearest;
        return nearest is not null;
    }

    public void Clear()
    {
        items.Clear();
    }
}
