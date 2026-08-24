using System;
using System.Collections.Generic;
using System.Text;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 将危机评估格式化为可审计日志，不写入任何游戏状态。
/// </summary>
public static class CrisisLogFormatter
{
    public static string FormatDetailed(CrisisAssessment assessment)
    {
        if (assessment is null)
        {
            throw new ArgumentNullException(nameof(assessment));
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"[Crisis/Evaluation] EvaluationId={assessment.EvaluationId}; Trigger={assessment.Trigger}; BaseDLRC={assessment.Result.Code}; FinalTags={string.Join("+", assessment.ActiveTags)}; FinalCode={assessment.Code}");
        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            if (!assessment.Detections.TryGetValue(tag, out CrisisDetectionResult? detection))
            {
                continue;
            }

            builder.Append($"; {detection.Tag}:Active={detection.IsActive};Severity={(int)detection.Severity};Reason={detection.Reason};Metrics=");
            AppendMetrics(builder, detection.Metrics);
        }

        return builder.ToString();
    }

    public static string FormatChange(CrisisAssessment? previous, CrisisAssessment current)
    {
        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        return $"[Crisis/Changed] PreviousCode={previous?.Code ?? "NONE"}; CurrentCode={current.Code}; PreviousTags={string.Join("+", previous?.ActiveTags ?? Array.Empty<CrisisTag>())}; CurrentTags={string.Join("+", current.ActiveTags)}";
    }

    private static void AppendMetrics(StringBuilder builder, IReadOnlyDictionary<string, double> metrics)
    {
        bool isFirst = true;
        foreach (KeyValuePair<string, double> metric in metrics)
        {
            if (!isFirst)
            {
                builder.Append(',');
            }

            builder.Append($"{metric.Key}={metric.Value:0.####}");
            isFirst = false;
        }
    }
}
