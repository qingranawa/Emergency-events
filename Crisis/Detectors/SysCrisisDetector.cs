using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// SYS 危机判定器。
/// </summary>
public sealed class SysCrisisDetector : ICrisisDetector
{
    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        int tier = snapshot.Scp079Tier;
        bool isActive = snapshot.Scp079Present && snapshot.Scp079TierIsValid && tier >= 3;
        return new CrisisDetectionResult(
            CrisisTag.SYS,
            isActive,
            isActive
                ? $"SCP079 Tier={tier}"
                : snapshot.Scp079Present && !snapshot.Scp079TierIsValid
                    ? "SCP079 TierUnavailable"
                    : "SCP079 unavailable or below Tier3"
                ,
            new Dictionary<string, double>
            {
                ["Scp079Present"] = snapshot.Scp079Present ? 1d : 0d,
                ["Scp079Tier"] = tier,
                ["Scp079TierIsValid"] = snapshot.Scp079TierIsValid ? 1d : 0d,
            });
    }
}
