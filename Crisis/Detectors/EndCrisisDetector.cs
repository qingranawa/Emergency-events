using System;
using System.Collections.Generic;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// END 危机判定器。
/// </summary>
public sealed class EndCrisisDetector : ICrisisDetector
{
    private readonly CrisisOptions options;

    public EndCrisisDetector(CrisisOptions? configuredOptions = null)
    {
        options = configuredOptions ?? CrisisOptions.Default;
    }

    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        if (snapshot is null || state is null)
        {
            throw new ArgumentNullException(snapshot is null ? nameof(snapshot) : nameof(state));
        }

        if (!snapshot.WarheadDetonated)
        {
            state.ResetEndgame();
            return CreateInactive(snapshot, "WarheadDetonated=false", 0d);
        }

        if (!snapshot.WarheadDetonatedAt.HasValue)
        {
            state.ResetEndgame();
            return CreateInactive(snapshot, "WarheadDetonatedAtUnavailable", 0d);
        }

        if (!HasSurfaceHostileStalemate(snapshot))
        {
            state.ResetSurfaceStalemate();
            return CreateInactive(snapshot, "SurfaceHostileStalemate=false", 0d);
        }

        state.StartSurfaceStalemate(snapshot.Timestamp);
        DateTime surfaceStalemateStartedAt = state.SurfaceStalemateStartedAt
            ?? throw new InvalidOperationException("Surface stalemate state was not initialized.");
        double durationSeconds = (snapshot.Timestamp - surfaceStalemateStartedAt).TotalSeconds;
        bool isActive = durationSeconds >= options.EndActivationSeconds;
        return new CrisisDetectionResult(
            CrisisTag.END,
            isActive,
            isActive ? $"SurfaceHostileStalemateSeconds={durationSeconds:0.###}" : "SurfaceHostileStalemate duration below ActivationThreshold",
            CreateMetrics(snapshot, durationSeconds));
    }

    private static bool HasSurfaceHostileStalemate(RoundSnapshot snapshot)
    {
        bool foundation = snapshot.SurfaceFoundationCombatants > 0;
        bool chaos = snapshot.SurfaceChaosCombatants > 0;
        bool scp = snapshot.SurfaceMainScp > 0;
        bool otherHostiles = snapshot.SurfaceOtherHostiles > 0;
        return (foundation && (chaos || scp || otherHostiles))
            || (chaos && (scp || otherHostiles))
            || (scp && otherHostiles);
    }

    private static CrisisDetectionResult CreateInactive(RoundSnapshot snapshot, string reason, double durationSeconds)
    {
        return new CrisisDetectionResult(
            CrisisTag.END,
            false,
            reason,
            CreateMetrics(snapshot, durationSeconds));
    }

    private static Dictionary<string, double> CreateMetrics(RoundSnapshot snapshot, double durationSeconds)
    {
        return new Dictionary<string, double>
        {
            ["SurfaceFoundationCombatants"] = snapshot.SurfaceFoundationCombatants,
            ["SurfaceChaosCombatants"] = snapshot.SurfaceChaosCombatants,
            ["SurfaceMainScp"] = snapshot.SurfaceMainScp,
            ["SurfaceOtherHostiles"] = snapshot.SurfaceOtherHostiles,
            ["WarheadDetonatedAtTicks"] = snapshot.WarheadDetonatedAt?.Ticks ?? 0d,
            ["ContinuousStalemateSeconds"] = durationSeconds,
        };
    }
}
