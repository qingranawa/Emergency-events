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
        CrisisSeverity severity = hostileThreatPresent
            ? ResolveSeverity(foundationCombatants, thresholds)
            : CrisisSeverity.Inactive;
        return new CrisisDetectionResult(
            CrisisTag.SEC,
            severity != CrisisSeverity.Inactive,
            severity,
            hostileThreatPresent
                ? $"FoundationCombatants={foundationCombatants}; HostileThreatPresent=true"
                : "HostileThreatPresent=false",
            new Dictionary<string, double>
            {
                ["FoundationCombatants"] = foundationCombatants,
                ["HostileThreatPresent"] = hostileThreatPresent ? 1d : 0d,
                ["L3Threshold"] = thresholds.Level3,
                ["L4Threshold"] = thresholds.Level4,
                ["L5Threshold"] = thresholds.Level5,
            });
    }

    private static CrisisSeverity ResolveSeverity(int foundationCombatants, CrisisTierThresholds thresholds)
    {
        if (foundationCombatants <= thresholds.Level5)
        {
            return CrisisSeverity.Level5;
        }

        if (thresholds.Level4 == thresholds.Level3
            && foundationCombatants == thresholds.Level3)
        {
            return CrisisSeverity.Level3;
        }

        if (foundationCombatants <= thresholds.Level4)
        {
            return CrisisSeverity.Level4;
        }

        return foundationCombatants <= thresholds.Level3
            ? CrisisSeverity.Level3
            : CrisisSeverity.Inactive;
    }
}
