using System;
using EmergencyEvents.Crisis;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 仅在 D-LRC 与危机结果来自同一次评估时生成完整展示代码。
/// </summary>
public static class DlrcDisplayCodeFormatter
{
    public static bool TryFormat(
        DlrcEvaluationResult result,
        long evaluationId,
        CrisisAssessment? assessment,
        out string code,
        out string reason)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (assessment is null)
        {
            code = result.Code;
            reason = "危机评估不可用。";
            return false;
        }

        if (assessment.EvaluationId != evaluationId || !ReferenceEquals(assessment.Result, result))
        {
            code = result.Code;
            reason = "危机评估与本次 D-LRC 结果不同步。";
            return false;
        }

        code = assessment.Code;
        reason = string.Empty;
        return true;
    }
}
