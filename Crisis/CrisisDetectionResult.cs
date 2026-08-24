using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 单个危机判定器的只读输出。
/// </summary>
public sealed class CrisisDetectionResult
{
    public CrisisDetectionResult(
        CrisisTag tag,
        bool isActive,
        CrisisSeverity severity,
        string? reason,
        IDictionary<string, double>? metrics = null)
    {
        Tag = tag;
        IsActive = isActive;
        Severity = isActive ? severity : CrisisSeverity.Inactive;
        Reason = reason?.Trim() ?? string.Empty;
        Metrics = new ReadOnlyDictionary<string, double>(
            metrics is null
                ? new Dictionary<string, double>(StringComparer.Ordinal)
                : new Dictionary<string, double>(metrics, StringComparer.Ordinal));
    }

    public CrisisTag Tag { get; }

    public bool IsActive { get; }

    public CrisisSeverity Severity { get; }

    public string Reason { get; }

    public IReadOnlyDictionary<string, double> Metrics { get; }
}
