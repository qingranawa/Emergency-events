using System;

namespace EmergencyEvents.O4;

/// <summary>
/// M06 O4 面板和选择会话配置。
/// </summary>
public sealed class O4PanelConfig
{
    public bool Enabled { get; set; } = true;

    public bool ShowNormalPanel { get; set; } = true;

    public bool EnableEventSelection { get; set; } = true;

    public double RefreshIntervalSeconds { get; set; } = 1d;

    public double HintDurationSeconds { get; set; } = 1.3d;

    public int VoteDurationSeconds { get; set; } = 20;

    public int MaxCandidates { get; set; } = 2;

    public bool ShowFdi { get; set; } = true;

    public bool ShowCrisis { get; set; } = true;

    public bool ShowNextEvaluation { get; set; } = true;

    public bool ShowControlState { get; set; } = true;

    public int HistoryCapacity { get; set; } = 256;

    public O4PanelConfig Normalize()
    {
        RefreshIntervalSeconds = Clamp(RefreshIntervalSeconds, 0.5d, 5d, 1d);
        HintDurationSeconds = Clamp(HintDurationSeconds, 0.5d, 5d, 1.3d);
        VoteDurationSeconds = Math.Max(5, Math.Min(120, VoteDurationSeconds));
        MaxCandidates = Math.Max(1, Math.Min(2, MaxCandidates));
        HistoryCapacity = HistoryCapacity <= 0 ? 256 : Math.Min(256, HistoryCapacity);
        return this;
    }

    private static double Clamp(double value, double minimum, double maximum, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Min(maximum, Math.Max(minimum, value));
    }
}
