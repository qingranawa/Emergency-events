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
        bool isActive = zombieCount >= thresholds.ActivationThreshold;
        return new CrisisDetectionResult(
            CrisisTag.BIO,
            isActive,
            isActive ? "ZombieCount >= ActivationThreshold" : "ZombieCount below ActivationThreshold",
            new Dictionary<string, double>
            {
                ["ZombieCount"] = zombieCount,
                ["ActivationThreshold"] = thresholds.ActivationThreshold,
            });
    }
}
