using System;
using System.Collections.Generic;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Evaluation;

/// <summary>
/// 解析人口档位对应的理论响应等级。
/// </summary>
public static class LevelResolver
{
    public static int ResolveTheoreticalLevel(
        PopulationTier tier,
        double effectiveResponseScore,
        EvaluationOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (!Enum.IsDefined(typeof(PopulationTier), tier))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tier),
                tier,
                "PopulationTier 必须是已定义的 A、B、C、D 或 E。");
        }

        IReadOnlyList<double> thresholds = options.GetThresholds(tier);
        for (int level = thresholds.Count - 1; level >= 0; level--)
        {
            if (effectiveResponseScore >= thresholds[level])
            {
                return level;
            }
        }

        return 0;
    }
}
