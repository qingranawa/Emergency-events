using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis;

/// <summary>
/// 仅根据同一时刻的 D-LRC 输入判定一种危机。
/// </summary>
public interface ICrisisDetector
{
    CrisisDetectionResult Detect(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisState state,
        CrisisContext context);
}
