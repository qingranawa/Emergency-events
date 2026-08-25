using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using EmergencyEvents.Crisis;
using EmergencyEvents.Director;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.RuntimeHarness;

/// <summary>
/// 仅隔离服使用的 Module 05 dry-run 命令，不生成真实游戏事件。
/// </summary>
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class DirectorRuntimeHarnessCommand : ICommand
{
    public string Command => "director_runtime_probe";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "隔离服 Module 05 Event Director dry-run 集成测试。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 0)
        {
            response = "用法：director_runtime_probe";
            return false;
        }

        List<string> evidence = new List<string>();
        EmergencyEvents.Plugin? plugin = EmergencyEvents.Plugin.Instance;
        bool pluginLoaded = plugin?.EventDirector is not null;
        evidence.Add($"PLUGIN_ADAPTER Loaded={pluginLoaded};ProductionDefinitions=0;EnabledByDefault=false");

        EventDefinition support = new EventDefinition(
            "harness-support",
            "Harness Support",
            EventCategory.Support,
            EventSource.Chaos,
            EventResponseLevel.L0,
            Array.Empty<CrisisTag>(),
            CrisisSeverity.Inactive,
            TierPersonnelPlan.Uniform(2),
            TierPersonnelPlan.Uniform(1),
            isEnabled: true,
            priority: 2,
            weight: 1d,
            requiresUndergroundFacility: false);
        EventDefinition nonSupport = new EventDefinition(
            "harness-nonsupport",
            "Harness NonSupport",
            EventCategory.NonSupport,
            EventSource.Internal,
            EventResponseLevel.L0,
            Array.Empty<CrisisTag>(),
            CrisisSeverity.Inactive,
            TierPersonnelPlan.Uniform(2),
            TierPersonnelPlan.Uniform(1),
            isEnabled: true,
            priority: 1,
            weight: 1d,
            requiresUndergroundFacility: false);
        EventDirector director = new EventDirector(
            new[] { support, nonSupport },
            new EventDirectorConfig { Enabled = true, CadenceSeconds = 0 });
        EventDirectorRuntimeManager runtime = new EventDirectorRuntimeManager(director, 16);
        DateTime timestamp = DateTime.UtcNow;
        RoundSnapshot snapshot = new RoundSnapshot(
            91005,
            timestamp,
            TimeSpan.Zero,
            PopulationTier.E,
            16,
            startingScpCount: 0,
            currentOnlinePlayers: 20,
            eligibleSpectators: 4);
        DlrcEvaluationResult result = EmergencyEvents.Evaluation.DlrcEvaluator.Evaluate(
            snapshot,
            new EvaluationHistory(),
            new EvaluationOptions(),
            0d);
        CrisisAssessment assessment = new CrisisAssessment(
            1,
            DlrcEvaluationTrigger.PERIODIC,
            snapshot,
            result,
            Array.Empty<CrisisDetectionResult>());

        runtime.StartRound(snapshot.RoundId, snapshot.PopulationTier);
        bool periodicCreated = runtime.HandleEvaluation(
            new DlrcEvaluationCompletedEvent(1, DlrcEvaluationTrigger.PERIODIC, snapshot, result),
            assessment,
            null,
            Array.Empty<Reinforcement.MajorWaveRecord>(),
            new DirectorPersonnelFacts(0, 2, 0, 4, 0, 20),
            FacilityState.Normal,
            hasO4Selector: false);
        bool postCreated = runtime.HandleEvaluation(
            new DlrcEvaluationCompletedEvent(2, DlrcEvaluationTrigger.POST_MAJOR_WAVE, snapshot, result),
            new CrisisAssessment(2, DlrcEvaluationTrigger.POST_MAJOR_WAVE, snapshot, result, Array.Empty<CrisisDetectionResult>()),
            null,
            Array.Empty<Reinforcement.MajorWaveRecord>(),
            new DirectorPersonnelFacts(0, 2, 0, 4, 0, 20),
            FacilityState.Normal,
            hasO4Selector: false);
        bool manualCreated = runtime.HandleEvaluation(
            new DlrcEvaluationCompletedEvent(3, DlrcEvaluationTrigger.MANUAL_RA, snapshot, result),
            new CrisisAssessment(3, DlrcEvaluationTrigger.MANUAL_RA, snapshot, result, Array.Empty<CrisisDetectionResult>()),
            null,
            Array.Empty<Reinforcement.MajorWaveRecord>(),
            new DirectorPersonnelFacts(0, 2, 0, 4, 0, 20),
            FacilityState.Normal,
            hasO4Selector: false);
        bool explicitCreated = runtime.TryCreateExplicitCycle();
        bool duplicateExplicitCreated = runtime.TryCreateExplicitCycle();
        DirectorCycle? cycle = director.CurrentCycle;
        DateTime actualSpawnAt = timestamp.AddSeconds(17);
        bool prepared = cycle is not null && director.Advance(EventLifecycleState.Prepared, true);
        bool started = cycle is not null && director.Advance(EventLifecycleState.Started, true, actualSpawnAt);
        bool committed = cycle?.SelectedSupport is not null
            && director.Commit(cycle.SelectedSupport, DirectorSlot.Support, actualSpawnAt);
        DateTime dueAt = director.Scheduler.SecondSlotDueAt ?? DateTime.MinValue;
        evidence.Add($"PERIODIC Cadence=0;Context={runtime.LastContext is not null};ProductionCycleCreated={periodicCreated};CreatedCycleCount={runtime.CreatedCycleCount}");
        evidence.Add($"SPECIAL POST_MAJOR_WAVE_Created={postCreated};MANUAL_RA_Created={manualCreated}");
        evidence.Add($"EXPLICIT Created={explicitCreated};DuplicateCreated={duplicateExplicitCreated};Count={runtime.CreatedCycleCount}");
        evidence.Add($"EVENT2 Scheduled={committed};ActualSpawnAt={actualSpawnAt:O};DueAt={dueAt:O};ExpectedDueAt={actualSpawnAt.AddSeconds(60):O}");

        EventDirector onceDirector = new EventDirector(
            new[] { support, nonSupport },
            new EventDirectorConfig { Enabled = true, CadenceSeconds = 0 },
            randomSource: new SeededRandomSource(2005));
        DirectorCycle? onceCycle = runtime.LastContext is null ? null : onceDirector.SelectCycle(runtime.LastContext);
        bool oncePrepared = onceCycle is not null && onceDirector.Advance(EventLifecycleState.Prepared, true);
        bool onceStarted = onceCycle is not null && onceDirector.Advance(EventLifecycleState.Started, true, actualSpawnAt);
        bool onceCommitted = onceCycle?.SelectedSupport is not null
            && onceDirector.Commit(onceCycle.SelectedSupport, DirectorSlot.Support, actualSpawnAt);
        DateTime onceDueAt = onceDirector.Scheduler.SecondSlotDueAt ?? DateTime.MinValue;
        bool firstEvent2 = oncePrepared && onceStarted && onceCommitted && onceDirector.TryBeginSecondSlot(onceDueAt.AddSeconds(1));
        bool secondEvent2 = onceDirector.TryBeginSecondSlot(onceDueAt.AddSeconds(2));
        bool thirdEvent2 = onceDirector.TryBeginSecondSlot(onceDueAt.AddSeconds(3));
        evidence.Add($"EVENT2_EXECUTE_ONCE First={firstEvent2};Second={secondEvent2};Third={thirdEvent2}");

        EventDefinition foundationDefinition = new EventDefinition(
            "harness-foundation",
            "Harness Foundation",
            EventCategory.Support,
            EventSource.Foundation,
            EventResponseLevel.L0,
            Array.Empty<CrisisTag>(),
            CrisisSeverity.Inactive,
            TierPersonnelPlan.Uniform(2),
            TierPersonnelPlan.Uniform(1),
            isEnabled: true,
            priority: 1,
            weight: 1d,
            requiresUndergroundFacility: false);
        EventCandidate chaosCandidate = new EventCandidate(support, true, "Eligible", 2, 2, 1, 2);
        EventCandidate foundationCandidate = new EventCandidate(foundationDefinition, true, "Eligible", 2, 2, 1, 2);
        EventCandidate[] sourceCandidates = { foundationCandidate, chaosCandidate };
        SupportSourceArbitrator seededFirst = new SupportSourceArbitrator(new EventDirectorConfig(), new SeededRandomSource(2005));
        SupportSourceArbitrator seededSecond = new SupportSourceArbitrator(new EventDirectorConfig(), new SeededRandomSource(2005));
        EventSource?[] seededFirstResults = Enumerable.Range(0, 12).Select(_ => seededFirst.SelectOrdinarySource(runtime.LastContext!, sourceCandidates)).ToArray();
        EventSource?[] seededSecondResults = Enumerable.Range(0, 12).Select(_ => seededSecond.SelectOrdinarySource(runtime.LastContext!, sourceCandidates)).ToArray();
        bool seededReproducible = seededFirstResults.SequenceEqual(seededSecondResults)
            && seededFirstResults.All(source => source is EventSource.Foundation or EventSource.Chaos);
        evidence.Add($"SEEDED_RNG Reproducible={seededReproducible};Samples={string.Join(",", seededFirstResults)}");

        int createdCycleCountBeforeCleanup = runtime.CreatedCycleCount;
        runtime.CleanupRound();
        bool event2AfterCleanup = director.TryBeginSecondSlot(dueAt.AddSeconds(1));
        evidence.Add($"CLEANUP Active={runtime.IsActive};Suspended={runtime.IsSuspended};Context={runtime.LastContext is not null};DueAt={director.Scheduler.SecondSlotDueAt};Event2Executed={event2AfterCleanup}");

        EventDirector lowDirector = new EventDirector(
            new[] { support, nonSupport },
            new EventDirectorConfig { Enabled = true, CadenceSeconds = 0 });
        EventDirectorRuntimeManager lowRuntime = new EventDirectorRuntimeManager(lowDirector, 16);
        lowRuntime.StartRound(snapshot.RoundId, snapshot.PopulationTier);
        lowRuntime.HandleEvaluation(
            new DlrcEvaluationCompletedEvent(4, DlrcEvaluationTrigger.PERIODIC, snapshot, result),
            new CrisisAssessment(4, DlrcEvaluationTrigger.PERIODIC, snapshot, result, Array.Empty<CrisisDetectionResult>()),
            null,
            Array.Empty<Reinforcement.MajorWaveRecord>(),
            new DirectorPersonnelFacts(0, 2, 0, 4, 0, 20),
            FacilityState.Normal,
            hasO4Selector: false);
        lowRuntime.TryCreateExplicitCycle();
        DirectorCycle? lowCycle = lowDirector.CurrentCycle;
        lowDirector.Advance(EventLifecycleState.Prepared, true);
        lowDirector.Advance(EventLifecycleState.Started, true, actualSpawnAt);
        if (lowCycle?.SelectedSupport is not null)
        {
            lowDirector.Commit(lowCycle.SelectedSupport, DirectorSlot.Support, actualSpawnAt);
        }
        DateTime lowDueAt = lowDirector.Scheduler.SecondSlotDueAt ?? DateTime.MinValue;
        bool suspended = lowRuntime.ObservePopulation(15);
        bool event2AfterLowPopulation = lowDirector.TryBeginSecondSlot(lowDueAt.AddSeconds(1));
        bool restartedSameRound = lowRuntime.StartRound(snapshot.RoundId, snapshot.PopulationTier);
        evidence.Add($"LOW_POPULATION_SUSPENDED Suspended={suspended};Event2Executed={event2AfterLowPopulation};RestartedSameRound={restartedSameRound};Active={lowRuntime.IsActive};SuspendedState={lowRuntime.IsSuspended}");

        bool pass = pluginLoaded
            && !periodicCreated
            && !postCreated
            && !manualCreated
            && explicitCreated
            && !duplicateExplicitCreated
            && createdCycleCountBeforeCleanup == 1
            && prepared
            && started
            && committed
            && dueAt == actualSpawnAt.AddSeconds(60)
            && firstEvent2
            && !secondEvent2
            && !thirdEvent2
            && seededReproducible
            && !event2AfterCleanup
            && suspended
            && !event2AfterLowPopulation
            && !restartedSameRound
            && !lowRuntime.IsActive
            && lowRuntime.IsSuspended;
        response = (pass ? "PASS" : "FAIL") + " DIRECTOR_RUNTIME_PROBE\n" + string.Join("\n", evidence);
        return pass;
    }
}
