using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// BIO 危机判定器。
/// </summary>
public sealed class BioCrisisDetector : ICrisisDetector
{
    private readonly CrisisOptions options;

    public BioCrisisDetector(CrisisOptions? configuredOptions = null)
    {
        options = configuredOptions ?? CrisisOptions.Default;
    }

    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        CrisisTierThresholds thresholds = options.GetBioThresholds(snapshot.PopulationTier);
        int zombieCount = snapshot.Scp0492Count;
        CrisisSeverity severity = ResolveSeverity(zombieCount, thresholds);
        return new CrisisDetectionResult(
            CrisisTag.BIO,
            severity != CrisisSeverity.Inactive,
            severity,
            severity == CrisisSeverity.Inactive
                ? "ZombieCount below L3Threshold"
                : $"ZombieCount >= L{(int)severity}Threshold",
            new Dictionary<string, double>
            {
                ["ZombieCount"] = zombieCount,
                ["L3Threshold"] = thresholds.Level3,
                ["L4Threshold"] = thresholds.Level4,
                ["L5Threshold"] = thresholds.Level5,
            });
    }

    private static CrisisSeverity ResolveSeverity(int zombieCount, CrisisTierThresholds thresholds)
    {
        if (zombieCount >= thresholds.Level5)
        {
            return CrisisSeverity.Level5;
        }

        if (zombieCount >= thresholds.Level4)
        {
            return CrisisSeverity.Level4;
        }

        return zombieCount >= thresholds.Level3
            ? CrisisSeverity.Level3
            : CrisisSeverity.Inactive;
    }
}
