using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandSystem;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RoundCore;
using PlayerRoles;

namespace EmergencyEvents.RuntimeHarness;

/// <summary>
/// 仅隔离服测试使用的 Game Console Harness，不属于正式插件功能。
/// </summary>
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class FdiRuntimeHarnessCommand : ICommand
{
    public string Command => "fdi_runtime_probe";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "隔离服 FDI Runtime 集成测试入口。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 0)
        {
            response = "用法：fdi_runtime_probe";
            return false;
        }

        EmergencyEvents.Plugin? plugin = EmergencyEvents.Plugin.Instance;
        FacilityDisorderRuntimeManager? manager = plugin?.FacilityDisorder;
        if (manager is null)
        {
            response = "FAIL Plugin 或 FacilityDisorderRuntimeManager 不可用。";
            return false;
        }

        List<string> evidence = new List<string>();
        RunRuntimeProducerChecks(evidence);
        DateTime start = new DateTime(2026, 8, 25, 4, 0, 0, DateTimeKind.Utc);
        const long roundId = 880045;
        manager.StartRound(start, 16, roundId);

        DateTime initialAt = start.AddMinutes(151);
        RoundSnapshot initialSnapshot = CreateSnapshot(roundId, initialAt, scp079Present: true, scp079Tier: 3, warheadUnlocked: true, warheadActive: true);
        CrisisAssessment initialAssessment = CreateAssessment(initialSnapshot, 1, (CrisisTag.SYS, CrisisSeverity.Level3), (CrisisTag.WAR, CrisisSeverity.Level4));
        manager.HandleEvaluation(CreateEvaluation(initialSnapshot, initialAssessment, 1), initialAssessment);
        evidence.Add($"INITIAL_0631 Current={manager.State.CurrentFacilityDisorder:0.####};CurrentStockAdjustment=7;TransientDelta=0;LastProcessedAt={manager.State.LastProcessedAt:O}");

        DateTime secondAt = initialAt.AddSeconds(30);
        manager.Service.Record(new DisorderEvent(
            "runtime:mtf-loss:063130",
            initialAt.AddSeconds(15),
            DisorderEventCategory.MtfForceChanged,
            2d,
            "06:31:15 injected transient; not represented by current stock"));
        RoundSnapshot secondSnapshot = CreateSnapshot(roundId, secondAt, scp079Present: true, scp079Tier: 3, warheadUnlocked: true, warheadActive: true);
        CrisisAssessment secondAssessment = CreateAssessment(secondSnapshot, 2, (CrisisTag.SYS, CrisisSeverity.Level3), (CrisisTag.WAR, CrisisSeverity.Level4));
        manager.HandleEvaluation(CreateEvaluation(secondSnapshot, secondAssessment, 2), secondAssessment);
        double afterSys = manager.State.CurrentFacilityDisorder;
        evidence.Add($"INCREMENT_30S Current={afterSys:0.####};CurrentStockAdjustment=0;TransientDelta=2;LastProcessedAt={manager.State.LastProcessedAt:O}");

        DateTime thirdAt = initialAt.AddMinutes(1);
        RoundSnapshot thirdSnapshot = CreateSnapshot(roundId, thirdAt, scp079Present: true, scp079Tier: 4, warheadUnlocked: true, warheadActive: true);
        CrisisAssessment thirdAssessment = CreateAssessment(thirdSnapshot, 3, (CrisisTag.SYS, CrisisSeverity.Level4), (CrisisTag.WAR, CrisisSeverity.Level4));
        DlrcEvaluationCompletedEvent thirdEvaluation = CreateEvaluation(thirdSnapshot, thirdAssessment, 3);
        manager.HandleEvaluation(thirdEvaluation, thirdAssessment);
        double afterWar = manager.State.CurrentFacilityDisorder;
        manager.HandleEvaluation(thirdEvaluation, thirdAssessment);
        evidence.Add($"SYS_079 Current={afterWar:0.####};079Delta=0;SYSDelta=4;WARDelta=0;UnderlyingWarheadDelta=0;DuplicateCurrent={manager.State.CurrentFacilityDisorder:0.####}");

        DateTime endAt = thirdAt.AddSeconds(30);
        RoundSnapshot endSnapshot = CreateSnapshot(
            roundId,
            endAt,
            scp079Present: true,
            scp079Tier: 4,
            warheadUnlocked: true,
            warheadActive: false,
            warheadDetonated: true,
            warheadDetonatedAt: endAt.AddSeconds(-1));
        CrisisAssessment endAssessment = CreateAssessment(endSnapshot, 5, (CrisisTag.END, CrisisSeverity.Level5));
        DlrcEvaluationCompletedEvent endEvaluation = CreateEvaluation(endSnapshot, endAssessment, 5);
        manager.HandleEvaluation(endEvaluation, endAssessment);
        double afterEnd = manager.State.CurrentFacilityDisorder;
        manager.HandleEvaluation(endEvaluation, endAssessment);
        evidence.Add($"END Current={afterEnd:0.####};WARTransitionDelta=-4;ENDTransitionDelta=5;UnderlyingWarheadDelta=0;DuplicateCurrent={manager.State.CurrentFacilityDisorder:0.####}");

        DateTime invalidAt = initialAt.AddSeconds(150);
        manager.Service.Record(new DisorderEvent(
            "runtime:recovery:transient",
            invalidAt.AddSeconds(-10),
            DisorderEventCategory.ZombieForceChanged,
            1d,
            "06:32:20 retained until valid recovery"));
        RoundSnapshot invalidSnapshot = CreateSnapshot(roundId, invalidAt, scp079Present: true, scp079Tier: 4, warheadUnlocked: true, warheadActive: true);
        DlrcEvaluationCompletedEvent invalidEvaluation = CreateEvaluation(invalidSnapshot, CreateAssessment(invalidSnapshot, 4), 4);
        DateTime? beforeInvalid = manager.State.LastProcessedAt;
        manager.HandleEvaluation(invalidEvaluation, null);
        evidence.Add($"INVALID_ASSESSMENT LastProcessedUnchanged={beforeInvalid == manager.State.LastProcessedAt};Current={manager.State.CurrentFacilityDisorder:0.####}");

        CrisisAssessment recoveredAssessment = CreateAssessment(invalidSnapshot, 4, (CrisisTag.SYS, CrisisSeverity.Level4), (CrisisTag.WAR, CrisisSeverity.Level4));
        manager.HandleEvaluation(invalidEvaluation, recoveredAssessment);
        evidence.Add($"RECOVERY LastProcessedAt={manager.State.LastProcessedAt:O};Current={manager.State.CurrentFacilityDisorder:0.####}");

        for (int index = 0; index < 200; index++)
        {
            DateTime timestamp = invalidAt.AddSeconds(index + 1);
            RoundSnapshot snapshot = CreateSnapshot(roundId, timestamp, scp079Present: true, scp079Tier: 4, warheadUnlocked: true, warheadActive: true);
            CrisisAssessment assessment = CreateAssessment(snapshot, 1000 + index, (CrisisTag.SYS, CrisisSeverity.Level4), (CrisisTag.WAR, CrisisSeverity.Level4));
            manager.HandleEvaluation(CreateEvaluation(snapshot, assessment, 1000 + index), assessment);
        }

        evidence.Add($"LONG_RUN Evaluations=200;History={manager.History.Count};Current={manager.State.CurrentFacilityDisorder:0.####}");
        FacilityDisorderRuntimeManager lowPopulationManager = new FacilityDisorderRuntimeManager();
        lowPopulationManager.StartRound(start, 16, roundId + 1);
        lowPopulationManager.ObservePopulation(15);
        bool suspended = lowPopulationManager.State.IsSuspended && !lowPopulationManager.State.IsActive;
        lowPopulationManager.ObservePopulation(16);
        bool remainedSuspended = !lowPopulationManager.State.IsActive
            && lowPopulationManager.State.IsSuspended;
        evidence.Add($"LOW_POP Suspended={suspended};Irreversible={remainedSuspended};Active={lowPopulationManager.State.IsActive}");
        lowPopulationManager.CleanupRound();
        manager.CleanupRound();
        evidence.Add($"CLEANUP Active={manager.State.IsActive};Initialized={manager.State.IsInitialized};Events={manager.Events.Count};History={manager.History.Count};LastProcessedAt={manager.State.LastProcessedAt}");
        response = "PASS FDI_RUNTIME_PROBE\n" + string.Join("\n", evidence);
        return true;
    }

    private static void RunRuntimeProducerChecks(List<string> evidence)
    {
        const long roundId = 880046;
        DateTime start = new DateTime(2026, 8, 25, 4, 0, 0, DateTimeKind.Utc);
        FacilityDisorderRuntimeManager manager = new FacilityDisorderRuntimeManager();
        manager.StartRound(start, 16, roundId);

        InvokePrivate(
            manager,
            "RecordForceDelta",
            "mtf",
            DisorderEventCategory.MtfForceChanged,
            0,
            6,
            -1d,
            2d);
        InvokePrivate(manager, "RecordForceDelta", "chaos", DisorderEventCategory.ChaosForceChanged, 0, 6, 1d, -1d);
        InvokePrivate(manager, "RecordForceDelta", "zombies", DisorderEventCategory.ZombieForceChanged, 2, 5, 1d, -1d);
        Require(manager.Events.Count == 3 && manager.Events.All(eventFact => eventFact.IsRepresentedByCurrentStock), "all pure stock ForceChanged facts must be stock-represented");
        evidence.Add($"PRODUCER_FORCE StockRepresented={manager.Events.All(eventFact => eventFact.IsRepresentedByCurrentStock)};Count={manager.Events.Count}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 1);
        InvokePrivate(manager, "RecordDeathFacts", start.AddMinutes(2), 1, RoleTypeId.NtfPrivate, RoleTypeId.Scp173);
        DisorderEvent combatEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.CombatDeath);
        Require(!combatEvent.IsRepresentedByCurrentStock, "CombatDeath must remain a transient event");
        evidence.Add($"PRODUCER_COMBAT StockRepresented={combatEvent.IsRepresentedByCurrentStock};Delta={combatEvent.Delta:0.####}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 2);
        RoundSnapshot previousSnapshot = CreateSnapshot(roundId + 2, start.AddMinutes(150), scp079Present: true, scp079Tier: 2);
        CrisisAssessment previousAssessment = CreateAssessment(previousSnapshot, 1, (CrisisTag.SYS, CrisisSeverity.Level3));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(previousSnapshot, previousAssessment, 1), previousAssessment);
        RoundSnapshot currentSnapshot = CreateSnapshot(roundId + 2, start.AddMinutes(151), scp079Present: true, scp079Tier: 3);
        CrisisAssessment currentAssessment = CreateAssessment(currentSnapshot, 2, (CrisisTag.SYS, CrisisSeverity.Level4));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(currentSnapshot, currentAssessment, 2), currentAssessment);
        DisorderEvent tierEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.Scp079TierChanged);
        DisorderEvent crisisEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.CrisisTransition);
        Require(tierEvent.Delta == 0d && crisisEvent.Delta == 4d, "079/SYS upgrade chain must have one non-zero Delta");
        Require(tierEvent.IsRepresentedByCurrentStock && crisisEvent.IsRepresentedByCurrentStock, "079/SYS initialization facts must be stock-represented");
        evidence.Add($"PRODUCER_079_SYS TierDelta={tierEvent.Delta:0.####};CrisisDelta={crisisEvent.Delta:0.####};NonZero={manager.Events.Count(eventFact => eventFact.Delta != 0d)}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 3);
        RoundSnapshot levelFourPreviousSnapshot = CreateSnapshot(roundId + 3, start.AddMinutes(150), scp079Present: true, scp079Tier: 3);
        CrisisAssessment levelFourPreviousAssessment = CreateAssessment(levelFourPreviousSnapshot, 5, (CrisisTag.SYS, CrisisSeverity.Level3));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(levelFourPreviousSnapshot, levelFourPreviousAssessment, 5), levelFourPreviousAssessment);
        RoundSnapshot levelFourCurrentSnapshot = CreateSnapshot(roundId + 3, start.AddMinutes(151), scp079Present: true, scp079Tier: 4);
        CrisisAssessment levelFourCurrentAssessment = CreateAssessment(levelFourCurrentSnapshot, 6, (CrisisTag.SYS, CrisisSeverity.Level4));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(levelFourCurrentSnapshot, levelFourCurrentAssessment, 6), levelFourCurrentAssessment);
        DisorderEvent levelFourTierEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.Scp079TierChanged);
        DisorderEvent levelFourCrisisEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.CrisisTransition);
        Require(levelFourTierEvent.Delta == 0d && levelFourCrisisEvent.Delta == 4d, "079 T3->T4/SYS L3->L4 chain must have one non-zero Delta");
        evidence.Add($"PRODUCER_079_T3_T4 TierDelta={levelFourTierEvent.Delta:0.####};CrisisDelta={levelFourCrisisEvent.Delta:0.####};NonZero={manager.Events.Count(eventFact => eventFact.Delta != 0d)}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 4);
        RoundSnapshot removalPreviousSnapshot = CreateSnapshot(roundId + 4, start.AddMinutes(150), scp079Present: true, scp079Tier: 3);
        CrisisAssessment removalPreviousAssessment = CreateAssessment(removalPreviousSnapshot, 10, (CrisisTag.SYS, CrisisSeverity.Level3));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(removalPreviousSnapshot, removalPreviousAssessment, 10), removalPreviousAssessment);
        RoundSnapshot removalCurrentSnapshot = CreateSnapshot(roundId + 4, start.AddMinutes(151), scp079Present: false, scp079Tier: 0);
        CrisisAssessment removalCurrentAssessment = CreateAssessment(removalCurrentSnapshot, 11);
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(removalCurrentSnapshot, removalCurrentAssessment, 11), removalCurrentAssessment);
        DisorderEvent removed079Event = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.Scp079TierChanged);
        DisorderEvent resolvedSysEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.CrisisTransition);
        Require(removed079Event.Delta == 0d && resolvedSysEvent.Delta == -4d, "079 removal/SYS resolution chain must have one non-zero Delta");
        evidence.Add($"PRODUCER_079_REMOVE TierDelta={removed079Event.Delta:0.####};CrisisDelta={resolvedSysEvent.Delta:0.####};NonZero={manager.Events.Count(eventFact => eventFact.Delta != 0d)}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 5);
        RoundSnapshot reappearPreviousSnapshot = CreateSnapshot(roundId + 5, start.AddMinutes(150), scp079Present: false, scp079Tier: 0);
        CrisisAssessment reappearPreviousAssessment = CreateAssessment(reappearPreviousSnapshot, 20);
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(reappearPreviousSnapshot, reappearPreviousAssessment, 20), reappearPreviousAssessment);
        RoundSnapshot reappearCurrentSnapshot = CreateSnapshot(roundId + 5, start.AddMinutes(151), scp079Present: true, scp079Tier: 3);
        CrisisAssessment reappearCurrentAssessment = CreateAssessment(reappearCurrentSnapshot, 21, (CrisisTag.SYS, CrisisSeverity.Level3));
        InvokePrivate(manager, "RecordSnapshotFacts", CreateEvaluation(reappearCurrentSnapshot, reappearCurrentAssessment, 21), reappearCurrentAssessment);
        DisorderEvent reappeared079Event = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.Scp079TierChanged);
        DisorderEvent activatedSysEvent = manager.Events.Single(eventFact => eventFact.Category == DisorderEventCategory.CrisisTransition);
        Require(reappeared079Event.Delta == 0d && activatedSysEvent.Delta == 3d, "079 reappearance/SYS activation chain must have one non-zero Delta");
        evidence.Add($"PRODUCER_079_REAPPEAR TierDelta={reappeared079Event.Delta:0.####};CrisisDelta={activatedSysEvent.Delta:0.####};NonZero={manager.Events.Count(eventFact => eventFact.Delta != 0d)}");

        manager.CleanupRound();
        manager.StartRound(start, 16, roundId + 3);
        InvokePrivate(manager, "RecordDeathFacts", start.AddMinutes(3), 2, RoleTypeId.Scp0492, RoleTypeId.None);
        InvokePrivate(manager, "ReconcileForceSnapshot", false);
        int zombieEvents = manager.Events.Count(eventFact => eventFact.Category == DisorderEventCategory.ZombieForceChanged);
        Require(zombieEvents == 1, "049-2 death path must create one ZombieForceChanged");
        evidence.Add($"PRODUCER_ZOMBIE ZombieForceChanged={zombieEvents}");
        manager.CleanupRound();
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method is null)
        {
            throw new InvalidOperationException($"Missing private method: {methodName}");
        }

        method.Invoke(target, arguments);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static DlrcEvaluationCompletedEvent CreateEvaluation(
        RoundSnapshot snapshot,
        CrisisAssessment assessment,
        long evaluationId)
    {
        return new DlrcEvaluationCompletedEvent(evaluationId, DlrcEvaluationTrigger.PERIODIC, snapshot, assessment.Result);
    }

    private static CrisisAssessment CreateAssessment(
        RoundSnapshot snapshot,
        long evaluationId,
        params (CrisisTag Tag, CrisisSeverity Severity)[] activeTags)
    {
        DlrcEvaluationResult result = EmergencyEvents.Evaluation.DlrcEvaluator.Evaluate(
            snapshot,
            new EvaluationHistory(),
            new EvaluationOptions(),
            0d);
        List<CrisisDetectionResult> detections = new List<CrisisDetectionResult>();
        foreach ((CrisisTag tag, CrisisSeverity severity) in activeTags)
        {
            detections.Add(new CrisisDetectionResult(tag, true, severity, "RuntimeHarness"));
        }

        return new CrisisAssessment(evaluationId, DlrcEvaluationTrigger.PERIODIC, snapshot, result, detections);
    }

    private static RoundSnapshot CreateSnapshot(
        long roundId,
        DateTime timestamp,
        bool scp079Present,
        int scp079Tier,
        bool warheadUnlocked = false,
        bool warheadActive = false,
        bool warheadDetonated = false,
        DateTime? warheadDetonatedAt = null)
    {
        return new RoundSnapshot(
            roundId,
            timestamp,
            timestamp - new DateTime(2026, 8, 25, 4, 0, 0, DateTimeKind.Utc),
            PopulationTier.E,
            16,
            1,
            scp079Present: scp079Present,
            scp079Tier: scp079Tier,
            warheadUnlocked: warheadUnlocked,
            warheadActive: warheadActive,
            warheadDetonated: warheadDetonated,
            warheadDetonatedAt: warheadDetonatedAt);
    }
}
