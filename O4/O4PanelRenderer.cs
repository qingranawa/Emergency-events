using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.O4;

/// <summary>
/// O4 单 Hint 文本渲染器，只负责展示，不判断资格或候选合法性。
/// </summary>
public sealed class O4PanelRenderer
{
    private readonly O4PanelConfig config;

    public O4PanelRenderer(O4PanelConfig? config = null)
    {
        this.config = (config ?? new O4PanelConfig()).Normalize();
    }

    public string RenderNormal(O4PanelViewModel model, DateTime now)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("O4 COMMAND");
        string control = config.ShowControlState ? $" · {FormatControlState(model.ControlState)}" : string.Empty;
        builder.AppendLine($"{model.DlrcCode} · 响应 L{model.ResponseLevel}{control}");
        List<string> status = new List<string>();
        if (config.ShowFdi)
        {
            status.Add($"FDI {FormatFdi(model.FdiBand)}");
        }

        if (config.ShowCrisis)
        {
            status.Add($"危机 {FormatCrisis(model.ActiveCrisisTags)}");
        }

        builder.AppendLine(status.Count == 0 ? "状态已隐藏" : string.Join(" · ", status));
        builder.Append(config.ShowNextEvaluation
            ? $"下次评估 {FormatCountdown(model.NextEvaluationAt, now)}"
            : "下次评估 已隐藏");
        return builder.ToString();
    }

    public string RenderSelection(
        O4PanelViewModel model,
        IReadOnlyList<O4CandidateView> candidates,
        int remainingSeconds,
        int votesReceived,
        int electorateCount)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("O4 COMMAND · EVENT SELECTION");
        string fdi = config.ShowFdi ? $" · FDI {FormatFdi(model.FdiBand)}" : string.Empty;
        builder.AppendLine($"{model.DlrcCode}{fdi}");
        IReadOnlyList<O4CandidateView> safeCandidates = candidates ?? Array.Empty<O4CandidateView>();
        for (int index = 0; index < safeCandidates.Count; index++)
        {
            O4CandidateView candidate = safeCandidates[index];
            string identity = string.Equals(candidate.DisplayName, candidate.EventId, StringComparison.Ordinal)
                ? candidate.EventId
                : $"{candidate.DisplayName} ({candidate.EventId})";
            string kind = candidate.IsProfessionalResponse ? "专业响应" : candidate.Category.ToString();
            builder.AppendLine($"[{index + 1}] {identity} · {kind} · {candidate.Source}");
        }

        builder.AppendLine("输入 o4vote 1 / 2");
        builder.Append($"剩余 {Math.Max(0, remainingSeconds)} 秒 · 已投 {Math.Max(0, votesReceived)} / {Math.Max(0, electorateCount)}");
        return builder.ToString();
    }

    public string RenderSuspended()
    {
        return "O4 COMMAND\n系统已暂停 · 人数低于运行阈值";
    }

    public string RenderUnavailable()
    {
        return "O4 COMMAND\nSYSTEM UNAVAILABLE";
    }

    private static string FormatControlState(ControlState state)
    {
        return state switch
        {
            ControlState.ADVANTAGE => "优势",
            ControlState.CONTROLLED => "受控",
            ControlState.UNCONTROLLED => "失控",
            ControlState.COLLAPSE => "崩溃",
            _ => "未知",
        };
    }

    private static string FormatFdi(FacilityDisorderBand band)
    {
        return band switch
        {
            FacilityDisorderBand.LOW => "低",
            FacilityDisorderBand.MEDIUM => "中",
            FacilityDisorderBand.HIGH => "高",
            _ => "未知",
        };
    }

    private static string FormatCrisis(IEnumerable<CrisisTag> tags)
    {
        string value = string.Join(" · ", (tags ?? Array.Empty<CrisisTag>()).Distinct().OrderBy(tag => tag));
        return string.IsNullOrEmpty(value) ? "无" : value;
    }

    private static string FormatCountdown(DateTime? nextEvaluationAt, DateTime now)
    {
        if (!nextEvaluationAt.HasValue)
        {
            return "--";
        }

        TimeSpan remaining = nextEvaluationAt.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            return "00:00";
        }

        int totalSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
