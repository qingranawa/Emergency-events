using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// GOI 危机判定器。
/// </summary>
public sealed class GoiCrisisDetector : ICrisisDetector
{
    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        bool foundationDisadvantaged = result.ControlAssessment.FoundationStrength == FoundationStrength.WEAK
            || result.ControlAssessment.FoundationStrength == FoundationStrength.CRITICAL;
        bool isActive = snapshot.HostileThirdPartyActive
            && snapshot.HostileThirdPartyCombatants > 0
            && result.FinalLevel >= 3
            && foundationDisadvantaged;
        return new CrisisDetectionResult(
            CrisisTag.GOI,
            isActive,
            isActive
                ? "Registered hostile third party with Foundation disadvantaged"
                : "GOI activation prerequisites not met",
            new Dictionary<string, double>
            {
                ["HostileThirdPartyActive"] = snapshot.HostileThirdPartyActive ? 1d : 0d,
                ["HostileThirdPartyCombatants"] = snapshot.HostileThirdPartyCombatants,
                ["GlobalLevel"] = result.FinalLevel,
                ["FoundationDisadvantaged"] = foundationDisadvantaged ? 1d : 0d,
            });
    }
}
