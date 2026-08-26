using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Crisis.Detectors;

/// <summary>
/// CON 危机判定器。
/// </summary>
public sealed class ConCrisisDetector : ICrisisDetector
{
    private readonly CrisisOptions options;

    public ConCrisisDetector(CrisisOptions? configuredOptions = null)
    {
        options = configuredOptions ?? CrisisOptions.Default;
    }

    public CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)
    {
        if (snapshot is null || state is null)
        {
            throw new ArgumentNullException(snapshot is null ? nameof(snapshot) : nameof(state));
        }

        MajorWaveSnapshot? secondWave = snapshot.MajorWaveHistory
            .Where(wave => wave.StartingCount > 0 && IsFoundationWave(wave))
            .OrderBy(wave => wave.CompletedAt)
            .Skip(1)
            .FirstOrDefault();
        if (secondWave is null)
        {
            state.ResetContainment();
            return CreateInactive(snapshot, "SecondMajorWaveUnavailable");
        }

        if (!secondWave.ScpCombatEquivalentAtCompletion.HasValue)
        {
            state.ResetContainment();
            return CreateInactive(snapshot, "SecondMajorWaveBaselineUnavailable");
        }

        double currentEquivalent = snapshot.MainScpAlive + snapshot.Scp0492Count / 3d;
        DateTime expectedCheckpoint = secondWave.CompletedAt.AddSeconds(options.ContainmentCheckpointSeconds);
        if (state.SecondMajorWaveCompletedAt != secondWave.CompletedAt
            || !state.ContainmentBaselineEquivalent.HasValue
            || !state.NextContainmentCheckpointAt.HasValue)
        {
            state.StartContainmentTracking(
                secondWave.CompletedAt,
                secondWave.ScpCombatEquivalentAtCompletion.Value,
                expectedCheckpoint);
        }

        DateTime nextCheckpointAt = state.NextContainmentCheckpointAt
            ?? throw new InvalidOperationException("Containment checkpoint state was not initialized.");
        if (snapshot.Timestamp >= nextCheckpointAt)
        {
            double baseline = state.ContainmentBaselineEquivalent
                ?? throw new InvalidOperationException("Containment baseline state was not initialized.");
            bool wasContained = currentEquivalent <= baseline - options.ContainmentEquivalentReduction;
            state.RecordContainmentCheckpoint(
                currentEquivalent,
                snapshot.Timestamp.AddSeconds(options.ContainmentCheckpointSeconds),
                wasContained);
        }

        bool isActive = state.ContainmentFailureStreak > 0;
        return new CrisisDetectionResult(
            CrisisTag.CON,
            isActive,
            isActive ? $"ContainmentFailureStreak={state.ContainmentFailureStreak}" : "Containment checkpoint passed or pending",
            new Dictionary<string, double>
            {
                ["CurrentEquivalent"] = currentEquivalent,
                ["BaselineEquivalent"] = state.ContainmentBaselineEquivalent
                    ?? secondWave.ScpCombatEquivalentAtCompletion.Value,
                ["FailureStreak"] = state.ContainmentFailureStreak,
                ["SecondMajorWaveCompletedAt"] = secondWave.CompletedAt.Ticks,
            });
    }

    private static CrisisDetectionResult CreateInactive(RoundSnapshot snapshot, string reason)
    {
        return new CrisisDetectionResult(
            CrisisTag.CON,
            false,
            reason,
            new Dictionary<string, double>
            {
                ["CurrentEquivalent"] = snapshot.MainScpAlive + snapshot.Scp0492Count / 3d,
            });
    }

    private static bool IsFoundationWave(MajorWaveSnapshot wave)
    {
        return string.Equals(wave.Faction, "NtfWave", StringComparison.OrdinalIgnoreCase)
            || string.Equals(wave.Faction, "NTF", StringComparison.OrdinalIgnoreCase)
            || string.Equals(wave.Faction, "MTF", StringComparison.OrdinalIgnoreCase);
    }
}
