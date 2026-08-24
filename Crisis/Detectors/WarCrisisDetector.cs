using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// WAR 危机判定器，只消费可由核弹事实可靠表达的状态。
/// </summary>
public sealed class WarCrisisDetector : ICrisisDetector
{
    public CrisisDetectionResult Detect(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisState state,
        CrisisContext context)
    {
        if (snapshot.WarheadDetonated)
        {
            return CreateInactive("WarheadDetonated=true", snapshot);
        }

        if (!snapshot.WarheadUnlocked)
        {
            return CreateInactive("WarheadUnlocked=false", snapshot);
        }

        CrisisSeverity severity = snapshot.WarheadActive
            ? CrisisSeverity.Level4
            : CrisisSeverity.Level3;
        string reason = snapshot.WarheadActive
            ? "WarheadUnlocked=true; CountdownActive=true"
            : "WarheadUnlocked=true; CountdownActive=false";
        return new CrisisDetectionResult(
            CrisisTag.WAR,
            true,
            severity,
            reason,
            CreateMetrics(snapshot));
    }

    private static CrisisDetectionResult CreateInactive(string reason, RoundSnapshot snapshot)
    {
        return new CrisisDetectionResult(
            CrisisTag.WAR,
            false,
            CrisisSeverity.Inactive,
            reason,
            CreateMetrics(snapshot));
    }

    private static Dictionary<string, double> CreateMetrics(RoundSnapshot snapshot)
    {
        return new Dictionary<string, double>
        {
            ["WarheadUnlocked"] = snapshot.WarheadUnlocked ? 1d : 0d,
            ["WarheadActive"] = snapshot.WarheadActive ? 1d : 0d,
            ["WarheadDetonated"] = snapshot.WarheadDetonated ? 1d : 0d,
            ["ReliableLevel5Fact"] = 0d,
        };
    }
}
