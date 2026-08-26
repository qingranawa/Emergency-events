using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// SEC 危机判定器。
/// </summary>
public sealed class SecCrisisDetector : ICrisisDetector
{
    private readonly CrisisOptions options;

    public SecCrisisDetector(CrisisOptions? configuredOptions = null)
    {
        options = configuredOptions ?? CrisisOptions.Default;
    }

    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        CrisisTierThresholds thresholds = options.GetSecurityThresholds(snapshot.PopulationTier);
        bool hostileThreatPresent = snapshot.MainScpAlive > 0
            || snapshot.ChaosCombatants > 0
            || snapshot.HostileThirdPartyCombatants > 0;
        int foundationCombatants = snapshot.FoundationCombatants;
        bool isActive = hostileThreatPresent && foundationCombatants <= thresholds.ActivationThreshold;
        return new CrisisDetectionResult(
            CrisisTag.SEC,
            isActive,
            hostileThreatPresent
                ? $"FoundationCombatants={foundationCombatants}; HostileThreatPresent=true"
                : "HostileThreatPresent=false",
            new Dictionary<string, double>
            {
                ["FoundationCombatants"] = foundationCombatants,
                ["HostileThreatPresent"] = hostileThreatPresent ? 1d : 0d,
                ["ActivationThreshold"] = thresholds.ActivationThreshold,
            });
    }

}
