using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Crisis;
using EmergencyEvents.Evaluation;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;

namespace EmergencyEvents.Disorder;

/// <summary>
/// FDI 运行时事实录制器。它只接收游戏事件和已完成评估，不解析文本日志。
/// </summary>
public sealed class FacilityDisorderRuntimeManager
{
    private const int EvaluationIdCapacity = 512;
    private const float ReconcileDelaySeconds = 0.2f;

    private readonly FacilityDisorderConfig config;
    private readonly FacilityDisorderService service;
    private readonly HashSet<long> evaluationIds = new HashSet<long>();
    private readonly Queue<long> evaluationIdOrder = new Queue<long>();
    private readonly List<CoroutineHandle> scheduledHandles = new List<CoroutineHandle>();
    private long highestEvaluationId;
    private ForceSnapshot? previousForces;
    private CrisisAssessment? previousAssessment;
    private bool hasObserved079;
    private bool previous079Present;
    private int previous079Tier;
    private bool hasObservedWarhead;
    private bool previousWarheadUnlocked;
    private bool previousWarheadActive;
    private bool previousWarheadDetonated;
    private long eventSequence;
    private long roundId;

    private readonly struct ForceSnapshot
    {
        public ForceSnapshot(int mtf, int chaos, int zombies)
        {
            Mtf = mtf;
            Chaos = chaos;
            Zombies = zombies;
        }

        public int Mtf { get; }

        public int Chaos { get; }

        public int Zombies { get; }
    }

    public FacilityDisorderRuntimeManager(FacilityDisorderConfig? config = null)
    {
        this.config = config ?? new FacilityDisorderConfig();
        service = new FacilityDisorderService(this.config);
    }

    public FacilityDisorderService Service => service;

    public FacilityDisorderState State => service.State;

    public IReadOnlyList<DisorderEvent> Events => service.Events;

    public IReadOnlyList<FacilityDisorderSettlement> History => service.History;

    public void StartRound(DateTime roundStartedAt, int openingPopulation, long currentRoundId = 0L)
    {
        StopScheduledCallbacks();
        ResetTransientFacts();
        roundId = currentRoundId;
        service.StartRound(roundStartedAt, openingPopulation, currentRoundId);
        LogInfo($"RoundStarted; RoundId={currentRoundId}; OpeningPopulation={openingPopulation}; MinimumPlayers={config.MinimumPlayers}; Active={State.IsActive}; Settlement=PERIODIC_ONLY; InitialLookbackSeconds={config.InitialLookbackSeconds}");
    }

    public void ObservePopulation(int currentPopulation)
    {
        if (service.ObservePopulation(currentPopulation))
        {
            StopScheduledCallbacks();
            LogInfo($"Suspended; CurrentPopulation={currentPopulation}; MinimumPlayers={config.MinimumPlayers}; IrreversibleForRound=true");
        }
    }

    public void ScheduleOpeningForceBaseline()
    {
        ScheduleReconcile(recordChanges: false, delaySeconds: 1f);
    }

    public void HandlePlayerJoined()
    {
        ScheduleReconcile(recordChanges: false, ReconcileDelaySeconds);
    }

    public void HandlePlayerLeft()
    {
        ScheduleReconcile(recordChanges: false, ReconcileDelaySeconds);
    }

    public void HandlePlayerDied(DiedEventArgs ev)
    {
        if (!State.IsActive)
        {
            return;
        }

        DateTime timestamp = DateTime.UtcNow;
        RoleTypeId targetRole = ev.TargetOldRole;
        Player? attacker = ev.Attacker;
        RoleTypeId attackerRole = attacker?.Role.Type ?? RoleTypeId.None;
        RecordDeathFacts(timestamp, ev.Player.Id, targetRole, attackerRole);
        ScheduleReconcile(recordChanges: false, ReconcileDelaySeconds);
    }

    public void HandleChangingRole(ChangingRoleEventArgs ev)
    {
        if (!State.IsActive)
        {
            return;
        }

        // 049-2 的生成是新增僵尸事实；普通职业分配和普通复活不作为 FDI 变化。
        if (ev.NewRole == RoleTypeId.Scp0492)
        {
            ScheduleReconcile(recordChanges: true, ReconcileDelaySeconds);
        }
    }

    public void HandleRespawnedTeam(RespawnedTeamEventArgs _)
    {
        if (State.IsActive)
        {
            ScheduleReconcile(recordChanges: true, ReconcileDelaySeconds);
        }
    }

    public void HandleEvaluation(DlrcEvaluationCompletedEvent completedEvent, CrisisAssessment? assessment)
    {
        if (!State.IsActive || completedEvent is null)
        {
            return;
        }

        FacilityDisorderEvaluationContext context = new FacilityDisorderEvaluationContext(completedEvent, assessment);
        if (!FacilityDisorderUpstreamValidationGuard.IsValid(context, roundId))
        {
            if (completedEvent.Trigger == DlrcEvaluationTrigger.PERIODIC)
            {
                service.SettlePeriodic(context, null);
            }

            LogInfo($"EvaluationRejected; EvaluationId={completedEvent.EvaluationId}; Trigger={completedEvent.Trigger}; Reason=UpstreamValidationFailed; EventsRetained=true");
            return;
        }

        if (completedEvent.EvaluationId <= highestEvaluationId
            || !evaluationIds.Add(completedEvent.EvaluationId))
        {
            return;
        }

        evaluationIdOrder.Enqueue(completedEvent.EvaluationId);
        highestEvaluationId = Math.Max(highestEvaluationId, completedEvent.EvaluationId);
        while (evaluationIdOrder.Count > EvaluationIdCapacity)
        {
            evaluationIds.Remove(evaluationIdOrder.Dequeue());
        }

        RecordSnapshotFacts(completedEvent, assessment);
        if (completedEvent.Trigger == DlrcEvaluationTrigger.PERIODIC)
        {
            FacilityDisorderSettlement? settlement = service.SettlePeriodic(context, CaptureCurrentStock(completedEvent.Snapshot, assessment!));
            if (settlement is not null)
            {
                LogInfo($"PeriodicSettlement; WindowStart={settlement.WindowStart:O}; WindowEnd={settlement.WindowEnd:O}; Previous={settlement.PreviousValue:0.####}; CurrentStockAdjustment={settlement.CurrentStockAdjustment:0.####}; RecentTransientDelta={settlement.RecentTransientDelta:0.####}; Delta={settlement.Delta:0.####}; Current={settlement.CurrentValue:0.####}; Band={State.DisorderBand}; EventCount={settlement.ProcessedEvents.Count}");
                LogInfo($"ORDER_RECOVERY_CHECK; RoundId={roundId}; EvaluationId={completedEvent.EvaluationId}; Timestamp={completedEvent.Snapshot.Timestamp:O}; CurrentFDI={settlement.PreviousValue:0.####}; CurrentBandBefore={ResolveBandForLog(settlement.PreviousValue)}; QuietWindowSeconds={config.OrderRecoveryQuietWindowSeconds}; LastPositiveDisorderAt={State.LastPositiveDisorderAt:O}; LastRecoveryAt={State.LastRecoveryAt:O}; ActiveCrises={string.Join(",", assessment!.ActiveTags)}; FacilityDestroyed={completedEvent.Snapshot.WarheadDetonated}; OrdinaryDeltaThisCycle={settlement.RecentTransientDelta:0.####}; RecoveryDelta={settlement.OrderRecoveryDelta:0.####}; AfterFDI={settlement.CurrentValue:0.####}; Result={settlement.RecoveryResult}");
            }
        }
    }

    public bool RecordFactionAdvantageChanged(string previous, string current, DateTime timestamp)
    {
        return service.Record(new DisorderEvent(
            NextEventId("advantage"),
            timestamp,
            DisorderEventCategory.FactionAdvantageChanged,
            config.FactionAdvantageChanged,
            $"Previous={previous};Current={current};Reason=HistoryOnly"));
    }

    public bool TryDryRunEvent(string eventName, int amount, out string response)
    {
        if (amount < 1 || string.IsNullOrWhiteSpace(eventName))
        {
            response = "用法：ee test disorder event mtf-loss <人数>。";
            return false;
        }

        double delta = eventName.Trim().ToLowerInvariant() switch
        {
            "mtf-loss" => config.MtfLossPerCombatant * amount,
            "mtf-gain" => config.MtfGainPerCombatant * amount,
            "scp-eliminated" => config.ScpEliminated,
            "warhead" => config.WarheadDetonated,
            _ => double.NaN,
        };
        if (double.IsNaN(delta))
        {
            response = "支持的 dry-run 事件：mtf-loss、mtf-gain、scp-eliminated、warhead。";
            return false;
        }

        response = $"FDI DRY-RUN; Event={eventName}; Amount={amount}; Delta={delta:0.####}; Current={State.CurrentFacilityDisorder:0.####}; Mutated=false";
        return true;
    }

    public void CleanupRound()
    {
        StopScheduledCallbacks();
        service.CleanupRound();
        ResetTransientFacts();
    }

    public void DisableForRound()
    {
        StopScheduledCallbacks();
        service.DisableForRound();
        ResetTransientFacts();
    }

    private void ScheduleReconcile(bool recordChanges, float delaySeconds)
    {
        if (!State.IsActive || roundId <= 0L)
        {
            return;
        }

        long scheduledRoundId = roundId;
        CoroutineHandle handle = default(CoroutineHandle);
        try
        {
            handle = Timing.CallDelayed(
                delaySeconds,
                () =>
                {
                    try
                    {
                        if (IsActiveRound(scheduledRoundId))
                        {
                            ReconcileForceSnapshot(recordChanges);
                        }
                    }
                    finally
                    {
                        RemoveScheduledHandle(scheduledRoundId, handle);
                    }
                });
            scheduledHandles.Add(handle);
        }
        catch (Exception exception)
        {
            LogInfo($"ReconcileScheduleFailed; RoundId={scheduledRoundId}; RecordChanges={recordChanges}; Reason={exception.GetType().Name}");
        }
    }

    private void RemoveScheduledHandle(long scheduledRoundId, CoroutineHandle handle)
    {
        if (roundId == scheduledRoundId)
        {
            scheduledHandles.Remove(handle);
        }
    }

    private void StopScheduledCallbacks()
    {
        foreach (CoroutineHandle handle in scheduledHandles)
        {
            try
            {
                Timing.KillCoroutines(handle);
            }
            catch (Exception exception)
            {
                LogInfo($"ReconcileCancelFailed; RoundId={roundId}; Reason={exception.GetType().Name}");
            }
        }

        scheduledHandles.Clear();
    }

    private void RecordDeathFacts(DateTime timestamp, int playerId, RoleTypeId targetRole, RoleTypeId attackerRole)
    {
        string prefix = $"death:{playerId}:{timestamp.Ticks}";
        if (IsMainScp(targetRole))
        {
            service.Record(new DisorderEvent(prefix + ":scp", timestamp, DisorderEventCategory.ScpEliminated, config.ScpEliminated, $"Target={targetRole};Reason=ImmediateThreatReduced"));
        }
        else if (targetRole == RoleTypeId.Scp0492)
        {
            service.Record(new DisorderEvent(prefix + ":zombie", timestamp, DisorderEventCategory.ZombieForceChanged, config.LossPerZombie, "Target=Scp0492", isRepresentedByCurrentStock: true));
        }

        if (IsFoundation(targetRole))
        {
            double? delta = IsMainScp(attackerRole)
                ? config.FoundationKilledByScp
                : IsChaos(attackerRole) ? config.FoundationKilledByChaos : null;
            if (delta.HasValue)
            {
                service.Record(new DisorderEvent(prefix + ":foundation", timestamp, DisorderEventCategory.CombatDeath, delta.Value, $"Target=Foundation;Attacker={attackerRole};Reason=ImmediateCombatDeath"));
            }
        }
        else if (IsChaos(targetRole) && IsFoundation(attackerRole))
        {
            service.Record(new DisorderEvent(prefix + ":hostile", timestamp, DisorderEventCategory.CombatDeath, config.FoundationKillsHostileHuman, $"Target=Chaos;Attacker={attackerRole};Reason=ImmediateCombatDeath"));
        }
    }

    private void RecordSnapshotFacts(DlrcEvaluationCompletedEvent completedEvent, CrisisAssessment? assessment)
    {
        RoundSnapshot snapshot = completedEvent.Snapshot;
        bool sysChanged = previousAssessment is not null
            && previousAssessment.IsActive(CrisisTag.SYS) != (assessment?.IsActive(CrisisTag.SYS) == true);
        if (!hasObserved079)
        {
            hasObserved079 = true;
        }
        else if (previous079Present && !snapshot.Scp079Present)
        {
            service.Record(new DisorderEvent(
                $"eval:{completedEvent.EvaluationId}:079-removed",
                snapshot.Timestamp,
                DisorderEventCategory.Scp079TierChanged,
                sysChanged ? 0d : config.Scp079Removed,
                sysChanged ? "SCP-079 removed;Reason=ExpressedBySYS" : "SCP-079 removed;Reason=Independent079Fact",
                isRepresentedByCurrentStock: true));
        }
        else if (!previous079Present && snapshot.Scp079Present)
        {
            service.Record(new DisorderEvent(
                $"eval:{completedEvent.EvaluationId}:079-reappeared",
                snapshot.Timestamp,
                DisorderEventCategory.Scp079TierChanged,
                sysChanged ? 0d : snapshot.Scp079Tier * config.Scp079TierIncreasePerLevel,
                sysChanged ? "SCP-079 reappeared;Reason=ExpressedBySYS" : "SCP-079 reappeared;Reason=Independent079Fact",
                isRepresentedByCurrentStock: true));
        }
        else if (previous079Present && snapshot.Scp079Present && snapshot.Scp079Tier != previous079Tier)
        {
            int difference = snapshot.Scp079Tier - previous079Tier;
            double perLevel = difference > 0 ? config.Scp079TierIncreasePerLevel : config.Scp079TierDecreasePerLevel;
            service.Record(new DisorderEvent(
                $"eval:{completedEvent.EvaluationId}:079-tier",
                snapshot.Timestamp,
                DisorderEventCategory.Scp079TierChanged,
                sysChanged ? 0d : Math.Abs(difference) * perLevel,
                sysChanged
                    ? $"Previous={previous079Tier};Current={snapshot.Scp079Tier};Reason=ExpressedBySYS"
                    : $"Previous={previous079Tier};Current={snapshot.Scp079Tier};Reason=Independent079Fact",
                isRepresentedByCurrentStock: true));
        }

        previous079Present = snapshot.Scp079Present;
        previous079Tier = snapshot.Scp079Tier;
        if (!hasObservedWarhead)
        {
            hasObservedWarhead = true;
        }
        else
        {
            RecordWarheadTransitions(completedEvent.EvaluationId, snapshot);
        }

        previousWarheadUnlocked = snapshot.WarheadUnlocked;
        previousWarheadActive = snapshot.WarheadActive;
        previousWarheadDetonated = snapshot.WarheadDetonated;
        RecordCrisisTransitions(completedEvent.EvaluationId, snapshot.Timestamp, assessment);
        previousAssessment = assessment;
    }

    private void RecordWarheadTransitions(long evaluationId, RoundSnapshot snapshot)
    {
        if (previousWarheadActive && !snapshot.WarheadActive && !snapshot.WarheadDetonated)
        {
            RecordWarheadEvent(evaluationId, snapshot.Timestamp, "cancelled", config.WarheadCancelled);
        }
    }

    private void RecordWarheadEvent(long evaluationId, DateTime timestamp, string state, double delta)
    {
        service.Record(new DisorderEvent($"eval:{evaluationId}:warhead-{state}", timestamp, DisorderEventCategory.WarheadChanged, delta, $"State={state};Reason=IndependentWarheadCancellation"));
    }

    private FacilityDisorderStockSnapshot CaptureCurrentStock(RoundSnapshot snapshot, CrisisAssessment assessment)
    {
        return new FacilityDisorderStockSnapshot(
            Player.Enumerable.Count(player => player.IsConnected && IsMtf(player.Role.Type)),
            Player.Enumerable.Count(player => player.IsConnected && IsChaos(player.Role.Type)),
            Player.Enumerable.Count(player => player.IsConnected && player.Role.Type == RoleTypeId.Scp0492),
            snapshot.MainScpAlive + snapshot.OtherHostileCombatants + snapshot.HostileThirdPartyCombatants,
            snapshot.Scp079Present,
            snapshot.Scp079Tier,
            assessment,
            snapshot.WarheadUnlocked,
            snapshot.WarheadActive,
            snapshot.WarheadDetonated);
    }

    private void RecordCrisisTransitions(long evaluationId, DateTime timestamp, CrisisAssessment? assessment)
    {
        if (assessment is null || previousAssessment is null)
        {
            return;
        }

        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            bool previous = previousAssessment.IsActive(tag);
            bool current = assessment.IsActive(tag);
            if (previous == current)
            {
                continue;
            }

            double delta = ResolveCrisisDelta(previous, current);
            service.Record(new DisorderEvent(
                $"eval:{evaluationId}:crisis-{tag}",
                timestamp,
                DisorderEventCategory.CrisisTransition,
                delta,
                $"Tag={tag};Previous={previous};Current={current};Reason={(tag == CrisisTag.CON ? "LongTermContainmentState" : "CrisisTransition")}",
                isRepresentedByCurrentStock: true));
        }
    }

    private double ResolveCrisisDelta(bool previous, bool current)
    {
        if (!current)
        {
            return config.CrisisResolved;
        }

        return config.CrisisActivated;
    }

    private void ReconcileForceSnapshot(bool recordChanges)
    {
        if (!State.IsActive)
        {
            return;
        }

        ForceSnapshot current = new ForceSnapshot(
            Player.Enumerable.Count(player => IsMtf(player.Role.Type) && player.IsConnected),
            Player.Enumerable.Count(player => IsChaos(player.Role.Type) && player.IsConnected),
            Player.Enumerable.Count(player => player.Role.Type == RoleTypeId.Scp0492 && player.IsConnected));
        if (previousForces is not ForceSnapshot previous)
        {
            previousForces = current;
            return;
        }

        if (recordChanges)
        {
            RecordForceDelta("mtf", DisorderEventCategory.MtfForceChanged, previous.Mtf, current.Mtf, config.MtfGainPerCombatant, config.MtfLossPerCombatant);
            RecordForceDelta("chaos", DisorderEventCategory.ChaosForceChanged, previous.Chaos, current.Chaos, config.ChaosGainPerCombatant, config.ChaosLossPerCombatant);
            RecordForceDelta("zombies", DisorderEventCategory.ZombieForceChanged, previous.Zombies, current.Zombies, config.GainPerZombie, config.LossPerZombie);
        }

        previousForces = current;
    }

    private void RecordForceDelta(string name, DisorderEventCategory category, int previous, int current, double gain, double loss)
    {
        int difference = current - previous;
        if (difference == 0)
        {
            return;
        }

        double delta = difference > 0 ? difference * gain : -difference * loss;
        service.Record(new DisorderEvent(
            NextEventId("force-" + name),
            DateTime.UtcNow,
            category,
            delta,
            $"Previous={previous};Current={current}",
            isRepresentedByCurrentStock: true));
    }

    private string NextEventId(string prefix)
    {
        eventSequence++;
        return $"{prefix}:{eventSequence}";
    }

    private void ResetTransientFacts()
    {
        evaluationIds.Clear();
        evaluationIdOrder.Clear();
        highestEvaluationId = 0L;
        previousForces = null;
        previousAssessment = null;
        hasObserved079 = false;
        previous079Present = false;
        previous079Tier = 0;
        hasObservedWarhead = false;
        previousWarheadUnlocked = false;
        previousWarheadActive = false;
        previousWarheadDetonated = false;
        eventSequence = 0L;
        roundId = 0L;
    }

    private bool IsActiveRound(long currentRoundId)
    {
        return State.IsActive && roundId > 0L && roundId == currentRoundId;
    }

    private string ResolveBandForLog(double value)
    {
        if (value < config.MediumMinimum)
        {
            return "LOW";
        }

        return value < config.HighMinimum ? "MEDIUM" : "HIGH";
    }

    private static bool IsFoundation(RoleTypeId role)
    {
        return role is RoleTypeId.FacilityGuard or RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfCaptain or RoleTypeId.NtfSpecialist;
    }

    private static bool IsMtf(RoleTypeId role)
    {
        return role is RoleTypeId.NtfPrivate or RoleTypeId.NtfSergeant or RoleTypeId.NtfCaptain or RoleTypeId.NtfSpecialist;
    }

    private static bool IsChaos(RoleTypeId role)
    {
        return role is RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor;
    }

    private static bool IsMainScp(RoleTypeId role)
    {
        string name = role.ToString();
        return name.StartsWith("Scp", StringComparison.OrdinalIgnoreCase)
            && role != RoleTypeId.Scp0492
            && role != RoleTypeId.Scp079;
    }

    private static void LogInfo(string message)
    {
        Log.Info($"[EmergencyEvents][FDI][{DateTime.UtcNow:O}] {message}");
    }
}
