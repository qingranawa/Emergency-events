using System;
using System.Collections.Generic;
using EmergencyEvents.Evaluation;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Evaluation.Tests;

internal static class Program
{
    private static int Main()
    {
        (string Name, Action Body)[] tests =
        {
            ("RoundSnapshot 保留锁定回合起始状态", RoundSnapshotRetainsLockedStartState),
            ("EvaluationOptions 暴露固定默认值和五组阈值", DefaultOptionsExposeFixedValues),
            ("非法 PopulationTier 会被理论等级解析器拒绝", InvalidPopulationTierIsRejected),
            ("非法阈值数组回退到默认六项契约", InvalidThresholdArraysFallBackToDefaults),
            ("快照集合会复制输入并保持只读", SnapshotsDefensivelyCopyCollections),
            ("负值和空值回退到安全默认值", InvalidInputsUseSafeDefaults),
            ("理论等级覆盖五档六级阈值上下边界", TheoreticalLevelsRespectAllTierThresholdBoundaries),
            ("SCP Presence 按开局数量计算并限制范围", ScpPresenceUsesStartingCount),
            ("SCP Health 只计算有效主要 SCP", ScpHealthUsesValidMainScpData),
            ("SCP Health 覆盖死亡、Hume 和零最大值", ScpHealthHandlesDeadAndZeroMaximum),
            ("049-2 压力按满压数量计算并封顶", ZombiePressureUsesConfiguredFullPressure),
            ("049-2 压力覆盖每个数量点", ZombiePressureCoversEveryCount),
            ("SCP-079 压力按等级映射", Scp079PressureUsesTierMapping),
            ("非法 SCP-079 等级采用安全 Clamp 策略", InvalidScp079TierIsClampedSafely),
            ("Foundation Combat 压力覆盖精确占比边界", FoundationCombatPressureUsesExactShareBoundaries),
            ("SCP Combat Equivalent 使用浮点僵尸折算", ScpCombatEquivalentUsesFloatingPoint),
            ("Spectator 压力覆盖精确比例边界", SpectatorPressureUsesExactRatioBoundaries),
            ("支援失败按已完成波次和严格存活率边界计算", ReinforcementFailureUsesCompletedWavesAndStrictBoundaries),
            ("连续两次高失败波次增加 Bonus", ReinforcementFailureAddsConsecutiveFailureBonus),
            ("未成熟波次不遮蔽最近成熟波次", ImmatureWaveDoesNotOverrideMatureWave),
            ("时间压力覆盖全部精确时间边界", TimePressureUsesExactBoundaries),
            ("战略风险只计算核弹取消次数", StrategicHazardCountsOnlyCancellations),
            ("总分限制范围且不重复计算持久调整", ResponseScoreClampsAndPreservesPersistentAdjustment),
            ("Threat Trend 覆盖历史不足和全部边界", ThreatTrendUsesHistoryBoundaries),
            ("Foundation Strength 覆盖全部占比边界", FoundationStrengthUsesExactBoundaries),
            ("Wave Performance 覆盖表现和团灭优先级", WavePerformanceUsesCompletedWaveBoundaries),
            ("Recent Battlefield Momentum 覆盖正负和一比零", BattlefieldMomentumUsesRecentLossBoundaries),
            ("Control State 覆盖六个固定场景", ControlStateUsesFixedScenarios),
            ("Collapse B 可独立命中且不依赖 A/C", CollapseConditionBIsTriggeredInIsolation),
            ("Collapse B 需要满足 Natural Score 条件", CollapseBRequiresNaturalScore),
            ("Threat Improving 时阻止 Collapse C", ImprovingThreatBlocksCollapseC),
            ("高分结果受 Control 上限限制", HighScoresRespectControlCaps),
            ("低理论等级不会被 Collapse 抬高", LowTheoreticalLevelIsNotRaisedByCollapse),
            ("连续评估不产生等级滞后", SequentialEvaluationHasNoLevelLag),
            ("EvaluationHistory 保持 Ring Buffer 和只读集合", EvaluationHistoryMaintainsRingBufferContract),
            ("EvaluationHistory 容量 20 且 25 次后仍可查询", EvaluationHistoryRetainsExactlyTwentyEntries),
            ("评估结果不会偷偷写入历史", EvaluationDoesNotPublishToHistory),
            ("结果代码使用锁定档位且不带危机标签", ResultCodeUsesLockedTierWithoutCrisisTag),
            ("D-LRC 调度从 391 秒开始并使用 30 秒步长", EvaluationScheduleUses391SecondStart),
            ("战场动量只保留窗口内死亡并支持清理", BattlefieldMomentumTracksWindowAndCleanup),
            ("评估日志包含最终代码和核心分数", EvaluationLogContainsCodeAndScore),
            ("异常数值不会污染 Response Score", InvalidHealthCannotPoisonScore),
            ("详细日志包含人工复算所需的分项和原始数据", EvaluationLogContainsEveryRecalculationComponent),
            ("死亡清除 Badge 后不再保留旧玩家映射", BadgeRegistryRemovesBadgeAfterDeath),
            ("首波截止无人时跳过并保持固定普通波次窗口", FirstWavePolicySkipsAtDeadline),
            ("固定波次窗口不会随实际刷新时间漂移", FixedWaveWindowsDoNotDrift),
            ("Support Score 账本覆盖 SCP 死亡和重复保护", SupportScoreLedgerScoresScpDeath),
            ("Support Score 账本覆盖伤害阈值和治疗后重复保护", SupportScoreLedgerScoresDamageThresholds),
            ("Support Score 账本覆盖物品实例和 SCP-914 排除", SupportScoreLedgerScoresItemInstances),
            ("高人口 SCP 角色不再强制第二只 SCP-939", ScpRolePolicyUsesOne939),
            ("插件波次从 05:00 到点且不提前触发", WaveTimerUsesFiveMinuteDueTime),
            ("只有插件发起的正常大波允许进入刷新管线", WaveGateRejectsNativeAndMiniWaves),
            ("插件 ForceWave 请求即使跳过选择事件也能进入刷新管线", PluginWaveRequestAllowsRespawnWhenSelectingEventSkipped),
            ("暂停原版计时器后允许 RA 手动正常大波", ManualWaveUsesPausedNativeTimer),
            ("晚加入的 None dummy 可以进入观察者候选池", LateDummyIsEligibleObserver),
            ("强制重启按顺序清理全部回合状态", RoundRestartResetsAllRoundState),
        };

        int total = tests.Length;
        int failed = 0;

        foreach ((string name, Action body) in tests)
        {
            failed += RunTest(name, body);
        }

        Console.WriteLine($"Total: {total}");
        Console.WriteLine($"Failed: {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static int RunTest(string name, Action body)
    {
        try
        {
            body();
            Console.WriteLine($"[PASS] {name}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[FAIL] {name}: {exception.Message}");
            return 1;
        }
    }

    private static void BadgeRegistryRemovesBadgeAfterDeath()
    {
        BadgeRegistry registry = new BadgeRegistry();
        registry.Remember(12, "Dummy");

        AssertTrue(registry.TryGet(12, out string? originalBadge), "应能取回玩家原始 Badge");
        AssertEqual("Dummy", originalBadge, "原始 Badge 内容错误");

        registry.Remove(12);

        AssertTrue(!registry.TryGet(12, out _), "死亡恢复后不应继续保留玩家 Badge 映射");
    }

    private static void FirstWavePolicySkipsAtDeadline()
    {
        AssertTrue(
            FirstWavePolicy.ShouldSkip(true, 390f, 390f, 0),
            "截止时间无普通观察者时应跳过首波");
        AssertTrue(
            !FirstWavePolicy.ShouldSkip(true, 390f, 390f, 1),
            "截止时间仍有普通观察者时不应跳过首波");
        AssertTrue(
            !FirstWavePolicy.ShouldSkip(false, 390f, 390f, 0),
            "首波已不在等待状态时不应重复跳过");
        AssertEqual(
            600f,
            FirstWavePolicy.GetNextNormalWaveDueAfterSkip(300f, 300f),
            "首波跳过后下一次普通波次应保持 10:00 固定窗口");
    }

    private static void FixedWaveWindowsDoNotDrift()
    {
        AssertEqual(600f, FirstWavePolicy.GetNextFixedWaveDue(300f, 300f), "第一波实际晚到不应改变第二个固定窗口");
        AssertEqual(900f, FirstWavePolicy.GetNextFixedWaveDue(600f, 300f), "第二窗口后应保持 15:00");
    }

    private static void SupportScoreLedgerScoresScpDeath()
    {
        SupportScoreLedger ledger = new SupportScoreLedger();
        AssertTrue(ledger.TryScoreScpDeath("scp-1", SupportFaction.Foundation, out int first), "首次 SCP 死亡应计分");
        AssertEqual(15, first, "SCP 死亡应增加 15 分");
        AssertTrue(!ledger.TryScoreScpDeath("scp-1", SupportFaction.Chaos, out _), "同一 SCP 死亡不得重复计分");
        AssertEqual(15, ledger.FoundationScore, "Foundation 死亡积分错误");
        AssertEqual(0, ledger.ChaosScore, "重复死亡不应给 Chaos 计分");
    }

    private static void SupportScoreLedgerScoresDamageThresholds()
    {
        SupportScoreLedger ledger = new SupportScoreLedger();
        AssertEqual(0, ledger.RecordScpDamage("scp-1", 0d, 100d, SupportFaction.Foundation).Count, "零伤害不得制造 0% 阈值");
        AssertEqual(1, ledger.RecordScpDamage("scp-1", 15d, 100d, SupportFaction.Foundation).Count, "15 点伤害应只跨过 10% 阈值");
        AssertEqual(2, ledger.RecordScpDamage("scp-1", 15d, 100d, SupportFaction.Chaos).Count, "累计 30 点伤害应补齐 20% 和 30% 阈值");
        AssertEqual(2, ledger.FoundationScore, "Foundation 首个阈值分数错误");
        AssertEqual(4, ledger.ChaosScore, "Chaos 多阈值分数错误");
        AssertEqual(0, ledger.RecordScpDamage("scp-1", 1d, 100d, SupportFaction.Foundation).Count, "累计损伤未跨新阈值时不得重复计分");
    }

    private static void SupportScoreLedgerScoresItemInstances()
    {
        SupportScoreLedger ledger = new SupportScoreLedger();
        AssertTrue(ledger.TryScoreItem(11, SupportItemKind.UniqueScp, SupportFaction.Foundation, false, out int unique), "自然唯一 SCP 物品应计分");
        AssertEqual(2, unique, "唯一 SCP 物品应增加 2 分");
        AssertTrue(!ledger.TryScoreItem(11, SupportItemKind.UniqueScp, SupportFaction.Foundation, false, out _), "丢弃重捡同一实例不得重复计分");
        AssertTrue(!ledger.TryScoreItem(12, SupportItemKind.UniqueScp, SupportFaction.Foundation, true, out _), "SCP-914 产物不得计入唯一物品分数");
        AssertTrue(ledger.TryScoreItem(13, SupportItemKind.ConsumableScp, SupportFaction.Chaos, false, out int consumable), "自然消耗品 SCP 物品应计分");
        AssertEqual(1, consumable, "消耗品 SCP 物品应增加 1 分");
        AssertEqual(2, ledger.FoundationScore, "唯一物品总分错误");
        AssertEqual(1, ledger.ChaosScore, "消耗品总分错误");
    }

    private static void ScpRolePolicyUsesOne939()
    {
        string[] pool =
        {
            "Scp049",
            "Scp079",
            "Scp106",
            "Scp3114",
            "Scp939",
        };
        List<string> roles = ScpRolePolicy.BuildRoles(3, pool, "Scp939", new Random(7));
        int scp939Count = 0;
        foreach (string role in roles)
        {
            if (role == "Scp939")
            {
                scp939Count++;
            }
        }

        AssertEqual(3, roles.Count, "SCP 角色数量错误");
        AssertEqual(1, scp939Count, "高人口回合不得无条件生成第二只 SCP-939");
    }

    private static void WaveTimerUsesFiveMinuteDueTime()
    {
        AssertTrue(!WaveControlPolicy.IsDue(299.99f, 300f), "05:00 前不应触发插件波次");
        AssertTrue(WaveControlPolicy.IsDue(300f, 300f), "05:00 应触发插件波次");
        AssertTrue(WaveControlPolicy.IsDue(330f, 300f), "超过到点时间仍应允许补触发插件波次");
    }

    private static void WaveGateRejectsNativeAndMiniWaves()
    {
        AssertTrue(WaveControlPolicy.ShouldAllowRespawn(true, false), "插件发起的正常大波应允许进入刷新管线");
        AssertTrue(!WaveControlPolicy.ShouldAllowRespawn(false, false), "原版自行触发的正常波次必须拦截");
        AssertTrue(!WaveControlPolicy.ShouldAllowRespawn(true, true), "插件也不得放行小波");
    }

    private static void PluginWaveRequestAllowsRespawnWhenSelectingEventSkipped()
    {
        AssertTrue(
            WaveControlPolicy.ShouldAllowTriggeredRespawn(true, false, false),
            "插件已发起 ForceWave 请求但没有经过选择事件时仍应允许正常波次");
        AssertTrue(
            WaveControlPolicy.ShouldAllowTriggeredRespawn(false, true, false),
            "已进入插件波次状态的正常波次应继续允许");
        AssertTrue(
            !WaveControlPolicy.ShouldAllowTriggeredRespawn(true, false, true),
            "插件请求的小波仍必须禁止");
        AssertTrue(
            !WaveControlPolicy.ShouldAllowTriggeredRespawn(false, false, false),
            "没有插件请求的原版正常波次仍必须禁止");
    }

    private static void ManualWaveUsesPausedNativeTimer()
    {
        AssertTrue(WaveControlPolicy.ShouldAllowManualNormalWave(true, false), "原版计时器暂停后应允许 RA 手动正常大波");
        AssertTrue(!WaveControlPolicy.ShouldAllowManualNormalWave(false, false), "原版计时器未暂停时不得放行未知原版波次");
        AssertTrue(!WaveControlPolicy.ShouldAllowManualNormalWave(true, true), "RA 手动 mini wave 仍必须禁止");
    }

    private static void LateDummyIsEligibleObserver()
    {
        AssertTrue(WaveControlPolicy.IsEligibleObserver(true, false, false, true), "晚加入且角色为 None 的 dummy 应可参与刷新");
        AssertTrue(WaveControlPolicy.IsEligibleObserver(true, false, true, false), "普通 Spectator 应可参与刷新");
        AssertTrue(!WaveControlPolicy.IsEligibleObserver(true, true, false, true), "Overwatch 不得参与刷新");
        AssertTrue(!WaveControlPolicy.IsEligibleObserver(false, false, false, true), "断开连接的 dummy 不得参与刷新");
    }

    private static void RoundRestartResetsAllRoundState()
    {
        List<string> calls = new List<string>();

        RoundRestartResetter.Reset(
            reason => calls.Add("DLRC:" + reason),
            () => calls.Add("Reinforcement"),
            () => calls.Add("RoundCore"));

        AssertSequence(
            new[] { "DLRC:RestartingRound", "Reinforcement", "RoundCore" },
            calls,
            "强制重启必须按完整顺序清理上一局状态");
    }

    private static void RoundSnapshotRetainsLockedStartState()
    {
        DateTime timestamp = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        RoundSnapshot snapshot = new RoundSnapshot(
            roundId: 7,
            timestamp: timestamp,
            roundElapsedTime: TimeSpan.FromSeconds(391),
            populationTier: PopulationTier.C,
            roundStartPopulation: 42,
            startingScpCount: 2);

        AssertEqual(PopulationTier.C, snapshot.PopulationTier, "RoundSnapshot 应保留锁定人口档位");
        AssertEqual(42, snapshot.RoundStartPopulation, "RoundSnapshot 应保留开局人数");
        AssertEqual(2, snapshot.StartingScpCount, "RoundSnapshot 应保留开局 SCP 数量");
        AssertEqual(timestamp, snapshot.Timestamp, "RoundSnapshot 应保留评估时间");
        AssertEqual(TimeSpan.FromSeconds(391), snapshot.RoundElapsedTime, "RoundSnapshot 应保留回合时间");
    }

    private static void DefaultOptionsExposeFixedValues()
    {
        EvaluationOptions options = new EvaluationOptions();

        AssertEqual(6, options.ZombieFullPressureCount, "僵尸满压数量默认值错误");
        AssertEqual(300, options.ThreatTrendWindowSeconds, "威胁趋势窗口默认值错误");
        AssertEqual(120, options.MomentumWindowSeconds, "动量窗口默认值错误");
        AssertEqual(5d, options.WarheadCancelScore, "核弹取消单次分数默认值错误");
        AssertEqual(10d, options.WarheadCancelMaxScore, "核弹取消最高分默认值错误");
        AssertEqual(391, options.EvaluationStartTimeSeconds, "首次评估时间默认值错误");
        AssertEqual(30, options.EvaluationIntervalSeconds, "评估间隔默认值错误");
        AssertEqual(20, options.HistoryCapacity, "历史容量默认值错误");
        AssertSequence(new[] { 0d, 18d, 32d, 48d, 65d, 82d }, options.EThresholds, "E 档阈值错误");
        AssertSequence(new[] { 0d, 20d, 34d, 50d, 67d, 84d }, options.DThresholds, "D 档阈值错误");
        AssertSequence(new[] { 0d, 22d, 36d, 52d, 69d, 86d }, options.CThresholds, "C 档阈值错误");
        AssertSequence(new[] { 0d, 24d, 38d, 54d, 71d, 88d }, options.BThresholds, "B 档阈值错误");
        AssertSequence(new[] { 0d, 26d, 40d, 56d, 73d, 90d }, options.AThresholds, "A 档阈值错误");
    }

    private static void InvalidPopulationTierIsRejected()
    {
        EvaluationOptions options = new EvaluationOptions();
        foreach (PopulationTier invalidTier in new[] { (PopulationTier)(-1), (PopulationTier)999 })
        {
            ArgumentOutOfRangeException exception = AssertThrows<ArgumentOutOfRangeException>(
                () => LevelResolver.ResolveTheoreticalLevel(invalidTier, 22d, options),
                "非法 PopulationTier 应抛出 ArgumentOutOfRangeException");
            AssertEqual("tier", exception.ParamName, "非法 PopulationTier 的参数名错误");
        }
    }

    private static void InvalidThresholdArraysFallBackToDefaults()
    {
        EvaluationOptions options = new EvaluationOptions(
            eThresholds: new[] { 0d, 18d },
            dThresholds: new[] { 0d, 20d, 19d, 50d, 67d, 84d },
            cThresholds: new[] { 0d, 22d, double.NaN, 52d, 69d, 86d },
            bThresholds: new[] { 0d, 24d, 38d, 54d, 71d, double.PositiveInfinity },
            aThresholds: new[] { 0d, 26d, 40d, 56d, 73d, -1d });

        AssertSequence(new[] { 0d, 18d, 32d, 48d, 65d, 82d }, options.EThresholds, "非法 E 档阈值未回退");
        AssertSequence(new[] { 0d, 20d, 34d, 50d, 67d, 84d }, options.DThresholds, "非法 D 档阈值未回退");
        AssertSequence(new[] { 0d, 22d, 36d, 52d, 69d, 86d }, options.CThresholds, "非法 C 档阈值未回退");
        AssertSequence(new[] { 0d, 24d, 38d, 54d, 71d, 88d }, options.BThresholds, "非法 B 档阈值未回退");
        AssertSequence(new[] { 0d, 26d, 40d, 56d, 73d, 90d }, options.AThresholds, "非法 A 档阈值未回退");
    }

    private static void SnapshotsDefensivelyCopyCollections()
    {
        DateTime timestamp = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        ScpSnapshot scp = new ScpSnapshot("SCP-173", true, 100, 100, 0, 0);
        ScpSnapshot[] scpStates = { scp };
        MajorWaveSnapshot wave = new MajorWaveSnapshot(
            name: "NTF",
            startingCount: 3,
            survivingCountAtEvaluation: 2,
            isEvaluationComplete: true,
            baseFailureScore: 4,
            isCatastrophic: false,
            startedAt: timestamp,
            evaluatedAt: timestamp.AddSeconds(120),
            memberIds: new[] { 10, 11 });
        List<MajorWaveSnapshot> waves = new List<MajorWaveSnapshot> { wave };
        List<int> activePlayerIds = new List<int> { 1, 2 };

        RoundSnapshot snapshot = new RoundSnapshot(
            roundId: 7,
            timestamp: timestamp,
            roundElapsedTime: TimeSpan.FromSeconds(391),
            populationTier: PopulationTier.C,
            roundStartPopulation: 42,
            startingScpCount: 1,
            scpStates: scpStates,
            majorWaveHistory: waves,
            activePlayerIds: activePlayerIds);

        scpStates[0] = new ScpSnapshot("SCP-096", false, 0, 0, 0, 0);
        waves.Clear();
        activePlayerIds.Add(3);

        AssertEqual(1, snapshot.ScpStates.Count, "SCP 集合不应受调用者数组修改影响");
        AssertEqual("SCP-173", snapshot.ScpStates[0].RoleName, "SCP 快照副本内容错误");
        AssertEqual(1, snapshot.MajorWaveHistory.Count, "波次集合不应受调用者列表修改影响");
        AssertEqual(2, snapshot.ActivePlayerIds.Count, "在线玩家 ID 集合不应受调用者列表修改影响");
        AssertReadOnly(snapshot.ScpStates, "SCP 集合应为只读");
        AssertReadOnly(snapshot.MajorWaveHistory, "波次集合应为只读");
        AssertReadOnly(snapshot.ActivePlayerIds, "在线玩家 ID 集合应为只读");
        AssertReadOnly(snapshot.MajorWaveHistory[0].MemberIds, "波次成员集合应为只读");
        AssertReadOnly(new EvaluationOptions().EThresholds, "阈值集合应为只读");
    }

    private static void InvalidInputsUseSafeDefaults()
    {
        RoundSnapshot snapshot = new RoundSnapshot(
            roundId: -1,
            timestamp: DateTime.MinValue,
            roundElapsedTime: TimeSpan.FromSeconds(-1),
            populationTier: (PopulationTier)999,
            roundStartPopulation: -10,
            startingScpCount: -2,
            currentOnlinePlayers: -3,
            foundationCombatants: -4,
            chaosCombatants: -5,
            otherHostileCombatants: -6,
            classDAlive: -7,
            scientistsAlive: -8,
            eligibleSpectators: -9,
            overwatchCount: -10,
            mainScpAlive: -11,
            scp0492Count: -12,
            scp079Present: false,
            scp079Tier: 5,
            warheadCancellationCount: -13,
            scpStates: null,
            majorWaveHistory: null,
            activePlayerIds: null);

        AssertEqual(0L, snapshot.RoundId, "负回合 ID 应回退为零");
        AssertEqual(TimeSpan.Zero, snapshot.RoundElapsedTime, "负回合时间应回退为零");
        AssertEqual(PopulationTier.E, snapshot.PopulationTier, "非法人口档位应回退为 E");
        AssertEqual(0, snapshot.RoundStartPopulation, "负开局人数应回退为零");
        AssertEqual(0, snapshot.StartingScpCount, "负开局 SCP 数量应回退为零");
        AssertEqual(0, snapshot.CurrentOnlinePlayers, "负在线人数应回退为零");
        AssertEqual(0, snapshot.Scp079Tier, "不存在的 SCP-079 等级应回退为零");
        AssertEqual(0, snapshot.ScpStates.Count, "空 SCP 集合应变为空只读集合");
        AssertEqual(0, snapshot.MajorWaveHistory.Count, "空波次集合应变为空只读集合");
        AssertEqual(0, snapshot.ActivePlayerIds.Count, "空玩家集合应变为空只读集合");

        ScpSnapshot invalidScp = new ScpSnapshot(
            roleName: null,
            isAlive: true,
            currentHealth: -1,
            maxHealth: -2,
            currentHume: double.NaN,
            maxHume: double.PositiveInfinity,
            isScp079: true);
        AssertEqual(string.Empty, invalidScp.RoleName, "空 SCP 角色名应回退为空字符串");
        AssertEqual(0d, invalidScp.CurrentHealth, "负 SCP 生命值应回退为零");
        AssertTrue(invalidScp.IsHealthDataUnavailable, "异常生命值应标记为不可用");

        EvaluationOptions options = new EvaluationOptions(
            zombieFullPressureCount: -1,
            threatTrendWindowSeconds: -2,
            momentumWindowSeconds: -3,
            warheadCancelScore: -4,
            warheadCancelMaxScore: -5,
            evaluationStartTimeSeconds: -6,
            evaluationIntervalSeconds: -7,
            historyCapacity: -8,
            eThresholds: null,
            dThresholds: null,
            cThresholds: null,
            bThresholds: null,
            aThresholds: null);
        AssertEqual(6, options.ZombieFullPressureCount, "负僵尸数量应回退到默认值");
        AssertEqual(300, options.ThreatTrendWindowSeconds, "负趋势窗口应回退到默认值");
        AssertEqual(120, options.MomentumWindowSeconds, "负动量窗口应回退到默认值");
        AssertEqual(5d, options.WarheadCancelScore, "负核弹取消分数应回退到默认值");
        AssertEqual(10d, options.WarheadCancelMaxScore, "负核弹最高分应回退到默认值");
        AssertEqual(391, options.EvaluationStartTimeSeconds, "负首次评估时间应回退到默认值");
        AssertEqual(30, options.EvaluationIntervalSeconds, "负评估间隔应回退到默认值");
        AssertEqual(20, options.HistoryCapacity, "负历史容量应回退到默认值");
    }

    private static void TheoreticalLevelsRespectAllTierThresholdBoundaries()
    {
        EvaluationOptions options = new EvaluationOptions();
        PopulationTier[] tiers =
        {
            PopulationTier.A,
            PopulationTier.B,
            PopulationTier.C,
            PopulationTier.D,
            PopulationTier.E,
        };

        foreach (PopulationTier tier in tiers)
        {
            IReadOnlyList<double> thresholds = options.GetThresholds(tier);
            for (int level = 0; level < thresholds.Count; level++)
            {
                double threshold = thresholds[level];
                AssertEqual(
                    level,
                    LevelResolver.ResolveTheoreticalLevel(tier, threshold, options),
                    $"{tier} 档达到 L{level} 阈值时等级错误");

                double belowThreshold = level == 0 ? -0.01d : threshold - 0.01d;
                int expectedBelowLevel = level == 0 ? 0 : level - 1;
                AssertEqual(
                    expectedBelowLevel,
                    LevelResolver.ResolveTheoreticalLevel(tier, belowThreshold, options),
                    $"{tier} 档低于 L{level} 阈值时等级错误");
            }
        }
    }

    private static void ScpPresenceUsesStartingCount()
    {
        (int Alive, double Expected)[] cases =
        {
            (4, 20d),
            (3, 15d),
            (2, 10d),
            (1, 5d),
            (0, 0d),
        };

        foreach ((int alive, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(
                CreateSnapshot(startingScpCount: 4, mainScpAlive: alive));
            AssertNear(expected, breakdown.ScpPresence, "SCP Presence 分数错误");
        }

        ResponseBreakdown zeroStart = CalculateBreakdown(
            CreateSnapshot(startingScpCount: 0, mainScpAlive: 4));
        AssertNear(0d, zeroStart.ScpPresence, "开局 SCP 数为零时 Presence 应为零");
    }

    private static void ScpHealthUsesValidMainScpData()
    {
        ResponseBreakdown full = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-173", true, 100, 100) }));
        AssertNear(10d, full.ScpHealth, "满血 SCP Health 应为满分");

        ResponseBreakdown half = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-096", true, 50, 100) }));
        AssertNear(5d, half.ScpHealth, "半血 SCP Health 分数错误");

        ResponseBreakdown critical = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-049", true, 1, 100) }));
        AssertNear(0.1d, critical.ScpHealth, "极残 SCP Health 分数错误");

        ResponseBreakdown hume = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-939", true, 50, 100, 25, 50) }));
        AssertNear(5d, hume.ScpHealth, "Hume 应计入 SCP Health");

        ResponseBreakdown excludes079 = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 2,
            mainScpAlive: 2,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, 100, 100),
                new ScpSnapshot("SCP-079", true, 100, 100, isScp079: true, healthDataUnavailable: false),
            }));
        AssertNear(5d, excludes079.ScpHealth, "SCP-079 不应计入普通 Health");

        ResponseBreakdown skipsInvalidEntries = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 3,
            mainScpAlive: 3,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, 100, 100),
                new ScpSnapshot("SCP-096", true, 100, 0, 0, 0),
                new ScpSnapshot("SCP-049", true, 100, 100, healthDataUnavailable: true),
            }));
        AssertNear(10d / 3d, skipsInvalidEntries.ScpHealth, "无效或不可用 Health 数据应跳过单项");
    }

    private static void ScpHealthHandlesDeadAndZeroMaximum()
    {
        ResponseBreakdown deadScp = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 4,
            mainScpAlive: 3,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", false, 100d, 100d),
                new ScpSnapshot("SCP-096", true, 100d, 100d),
                new ScpSnapshot("SCP-049", true, 100d, 100d),
                new ScpSnapshot("SCP-939", true, 100d, 100d),
            }));
        AssertNear(7.5d, deadScp.ScpHealth, "已死亡 SCP 不应继续贡献 Health 分数");

        ResponseBreakdown hpOnly = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, 100d, 100d, 0d, 0d),
            }));
        AssertNear(10d, hpOnly.ScpHealth, "Hume 为零时 HP 应正常参与 Health 计算");

        ResponseBreakdown zeroMaximum = CalculateBreakdown(CreateSnapshot(
            startingScpCount: 2,
            mainScpAlive: 2,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, 0d, 0d, 0d, 0d, healthDataUnavailable: true),
                new ScpSnapshot("SCP-096", true, 100d, 100d),
            }));
        AssertNear(5d, zeroMaximum.ScpHealth, "最大生命和 Hume 均为零的实体应跳过且不除零");
        AssertFinite(zeroMaximum.ScpHealth, "零最大值不能产生 NaN 或 Infinity");
    }

    private static void ZombiePressureUsesConfiguredFullPressure()
    {
        (int Count, double Expected)[] cases =
        {
            (0, 0d),
            (3, 2d),
            (6, 4d),
            (12, 4d),
        };

        foreach ((int count, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(
                CreateSnapshot(scp0492Count: count));
            AssertNear(expected, breakdown.ZombiePressure, "049-2 压力分数错误");
        }
    }

    private static void ZombiePressureCoversEveryCount()
    {
        double[] expected =
        {
            0d,
            2d / 3d,
            4d / 3d,
            2d,
            8d / 3d,
            10d / 3d,
            4d,
            4d,
            4d,
            4d,
            4d,
            4d,
            4d,
        };

        for (int count = 0; count < expected.Length; count++)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(scp0492Count: count));
            AssertNear(expected[count], breakdown.ZombiePressure, $"049-2 数量 {count} 的压力分数错误");
        }
    }

    private static void Scp079PressureUsesTierMapping()
    {
        (bool Present, int Tier, double Expected)[] cases =
        {
            (false, 0, 0d),
            (true, 1, 0d),
            (true, 2, 1.5d),
            (true, 3, 3d),
            (true, 4, 4.5d),
            (true, 5, 6d),
        };

        foreach ((bool present, int tier, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                scp079Present: present,
                scp079Tier: tier));
            AssertNear(expected, breakdown.Scp079Pressure, "SCP-079 压力分数错误");
        }
    }

    private static void InvalidScp079TierIsClampedSafely()
    {
        RoundSnapshot highTier = CreateSnapshot(
            scp079Present: true,
            scp079Tier: 99);
        ResponseBreakdown highTierScore = CalculateBreakdown(highTier);
        AssertEqual(5, highTier.Scp079Tier, "超过 5 的 SCP-079 Tier 应 Clamp 到 5");
        AssertNear(6d, highTierScore.Scp079Pressure, "Clamp 到 5 后 SCP-079 压力应为 6");

        RoundSnapshot lowTier = CreateSnapshot(
            scp079Present: true,
            scp079Tier: -1);
        ResponseBreakdown lowTierScore = CalculateBreakdown(lowTier);
        AssertEqual(0, lowTier.Scp079Tier, "低于 0 的 SCP-079 Tier 应 Clamp 到 0");
        AssertNear(0d, lowTierScore.Scp079Pressure, "Clamp 到 0 的 SCP-079 压力应为零");
    }

    private static void FoundationCombatPressureUsesExactShareBoundaries()
    {
        (int Foundation, int Hostile, double ExpectedShare, double ExpectedPressure)[] cases =
        {
            (5000, 5000, 0.5d, 0d),
            (4999, 5001, 0.4999d, 3d),
            (4000, 6000, 0.4d, 3d),
            (3999, 6001, 0.3999d, 6d),
            (3000, 7000, 0.3d, 6d),
            (2999, 7001, 0.2999d, 10d),
            (2000, 8000, 0.2d, 10d),
            (1999, 8001, 0.1999d, 12d),
            (1000, 9000, 0.1d, 12d),
            (999, 9001, 0.0999d, 14d),
        };

        foreach ((int foundation, int hostile, double expectedShare, double expectedPressure) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                foundationCombatants: foundation,
                chaosCombatants: hostile));
            AssertNear(expectedShare, breakdown.FoundationCombatShare, "Foundation Combat 占比错误");
            AssertNear(expectedPressure, breakdown.CombatPressure, "Foundation Combat 压力边界错误");
        }

        ResponseBreakdown noCombat = CalculateBreakdown(CreateSnapshot());
        AssertNear(1d, noCombat.FoundationCombatShare, "无 Combatant 时 Foundation 占比应按一处理");
        AssertNear(0d, noCombat.CombatPressure, "无 Combatant 时 Combat 压力应为零");
    }

    private static void ScpCombatEquivalentUsesFloatingPoint()
    {
        (int MainScp, int Zombies, double Expected)[] cases =
        {
            (3, 0, 3d),
            (3, 3, 4d),
            (3, 6, 5d),
            (2, 4, 2d + 4d / 3d),
        };

        foreach ((int mainScp, int zombies, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                mainScpAlive: mainScp,
                scp0492Count: zombies));
            AssertNear(expected, breakdown.ScpCombatEquivalent, "SCP Combat Equivalent 浮点折算错误");
        }
    }

    private static void SpectatorPressureUsesExactRatioBoundaries()
    {
        (int Eligible, int Online, double ExpectedRatio, double ExpectedPressure)[] cases =
        {
            (999, 10000, 0.0999d, 0d),
            (1, 10, 0.1d, 1d),
            (1999, 10000, 0.1999d, 1d),
            (1, 5, 0.2d, 2d),
            (2999, 10000, 0.2999d, 2d),
            (3, 10, 0.3d, 3d),
            (3999, 10000, 0.3999d, 3d),
            (2, 5, 0.4d, 4d),
            (4999, 10000, 0.4999d, 4d),
            (1, 2, 0.5d, 6d),
        };

        foreach ((int eligible, int online, double expectedRatio, double expectedPressure) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                currentOnlinePlayers: online,
                eligibleSpectators: eligible));
            AssertNear(expectedRatio, breakdown.SpectatorRatio, "Spectator 比例错误");
            AssertNear(expectedPressure, breakdown.SpectatorPressure, "Spectator 压力边界错误");
        }

        ResponseBreakdown noOnline = CalculateBreakdown(CreateSnapshot(eligibleSpectators: 5));
        AssertNear(0d, noOnline.SpectatorRatio, "在线人数为零时 Spectator 比例应为零");
    }

    private static void ReinforcementFailureUsesCompletedWavesAndStrictBoundaries()
    {
        (int Surviving, double Expected)[] cases =
        {
            (100, 0d),
            (76, 0d),
            (75, 4d),
            (51, 4d),
            (50, 8d),
            (26, 8d),
            (25, 12d),
            (1, 12d),
            (0, 15d),
        };

        foreach ((int surviving, double expected) in cases)
        {
            MajorWaveSnapshot wave = CreateWave(
                startingCount: 100,
                survivingCount: surviving,
                isEvaluationComplete: true,
                baseFailureScore: 20d);
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                majorWaveHistory: new[] { wave }));
            AssertNear(expected, breakdown.ReinforcementFailure, "波次存活率基础失败分数错误");
        }

        MajorWaveSnapshot incompleteWave = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: false,
            baseFailureScore: 20d);
        ResponseBreakdown incomplete = CalculateBreakdown(CreateSnapshot(
            majorWaveHistory: new[] { incompleteWave }));
        AssertNear(0d, incomplete.ReinforcementFailure, "未完成波次不得提前计入失败分");

        MajorWaveSnapshot zeroStartingWave = CreateWave(
            startingCount: 0,
            survivingCount: 0,
            isEvaluationComplete: true,
            baseFailureScore: 20d);
        ResponseBreakdown noAvailableWave = CalculateBreakdown(CreateSnapshot(
            majorWaveHistory: new[] { zeroStartingWave }));
        AssertNear(0d, noAvailableWave.ReinforcementFailure, "无有效起始人数波次应视为不可用");
    }

    private static void ReinforcementFailureAddsConsecutiveFailureBonus()
    {
        DateTime start = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        MajorWaveSnapshot previous = CreateWave(
            startingCount: 100,
            survivingCount: 25,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: start,
            evaluatedAt: start.AddSeconds(120));
        MajorWaveSnapshot current = CreateWave(
            startingCount: 100,
            survivingCount: 50,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: start.AddMinutes(2),
            evaluatedAt: start.AddMinutes(4));

        ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
            majorWaveHistory: new[] { current, previous }));
        AssertNear(13d, breakdown.ReinforcementFailure, "连续高失败波次 Bonus 应加到当前基础失败分");
        AssertNear(0.5d, breakdown.EvaluatedWaveSurvivalRatio!.Value, "当前波次存活率审计字段错误");
        AssertNear(8d, breakdown.EvaluatedWaveBaseFailure!.Value, "当前波次基础失败审计字段错误");
        AssertNear(12d, breakdown.PreviousEvaluatedWaveBaseFailure!.Value, "上一波基础失败审计字段错误");
        AssertEqual(100, breakdown.EvaluatedWaveStartingCount!.Value, "当前波次起始人数审计字段错误");
        AssertEqual(50, breakdown.EvaluatedWaveSurvivingCount!.Value, "当前波次存活人数审计字段错误");

        MajorWaveSnapshot catastrophicPrevious = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: start,
            evaluatedAt: start.AddSeconds(120));
        MajorWaveSnapshot catastrophicCurrent = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: start.AddMinutes(2),
            evaluatedAt: start.AddMinutes(4));
        ResponseBreakdown capped = CalculateBreakdown(CreateSnapshot(
            majorWaveHistory: new[] { catastrophicPrevious, catastrophicCurrent }));
        AssertNear(20d, capped.ReinforcementFailure, "连续团灭失败分应封顶到 20");
    }

    private static void ImmatureWaveDoesNotOverrideMatureWave()
    {
        DateTime now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        MajorWaveSnapshot matureWave = CreateWave(
            startingCount: 100,
            survivingCount: 100,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: now.AddMinutes(-4),
            evaluatedAt: now.AddMinutes(-2));
        MajorWaveSnapshot immatureWave = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: false,
            baseFailureScore: 15d,
            startedAt: now.AddMinutes(-1));

        ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
            timestamp: now,
            majorWaveHistory: new[] { matureWave, immatureWave }));
        AssertNear(0d, breakdown.ReinforcementFailure, "未成熟波次不得覆盖最近已成熟波次");
        AssertEqual(100, breakdown.EvaluatedWaveStartingCount!.Value, "应使用成熟波次的起始人数");
        AssertEqual(100, breakdown.EvaluatedWaveSurvivingCount!.Value, "应使用成熟波次的存活人数");
    }

    private static void TimePressureUsesExactBoundaries()
    {
        (int Seconds, double Expected)[] cases =
        {
            (9 * 60 + 59, 0d),
            (10 * 60, 2d),
            (15 * 60 - 1, 2d),
            (15 * 60, 4d),
            (20 * 60 - 1, 4d),
            (20 * 60, 6d),
            (25 * 60 - 1, 6d),
            (25 * 60, 8d),
            (30 * 60 - 1, 8d),
            (30 * 60, 10d),
        };

        foreach ((int seconds, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                roundElapsedTime: TimeSpan.FromSeconds(seconds)));
            AssertNear(expected, breakdown.TimePressure, "时间压力边界错误");
        }
    }

    private static void StrategicHazardCountsOnlyCancellations()
    {
        (int Cancellations, double Expected)[] cases =
        {
            (0, 0d),
            (1, 5d),
            (2, 10d),
        };

        foreach ((int cancellations, double expected) in cases)
        {
            ResponseBreakdown breakdown = CalculateBreakdown(CreateSnapshot(
                warheadUnlocked: true,
                warheadActive: true,
                warheadDetonated: true,
                warheadCancellationCount: cancellations));
            AssertNear(expected, breakdown.StrategicHazard, "核弹战略风险分数错误");
        }
    }

    private static void ResponseScoreClampsAndPreservesPersistentAdjustment()
    {
        MajorWaveSnapshot previous = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc),
            evaluatedAt: new DateTime(2026, 8, 23, 12, 2, 0, DateTimeKind.Utc));
        MajorWaveSnapshot current = CreateWave(
            startingCount: 100,
            survivingCount: 0,
            isEvaluationComplete: true,
            baseFailureScore: 0d,
            startedAt: new DateTime(2026, 8, 23, 12, 2, 0, DateTimeKind.Utc),
            evaluatedAt: new DateTime(2026, 8, 23, 12, 4, 0, DateTimeKind.Utc));
        RoundSnapshot maximumSnapshot = CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-173", true, 100, 100) },
            scp0492Count: 6,
            scp079Present: true,
            scp079Tier: 5,
            currentOnlinePlayers: 2,
            eligibleSpectators: 1,
            roundElapsedTime: TimeSpan.FromMinutes(30),
            warheadCancellationCount: 2,
            majorWaveHistory: new[] { previous, current });

        ResponseScoreResult aboveMaximum = ResponseScoreCalculator.Calculate(
            maximumSnapshot,
            new EvaluationOptions(),
            persistentAdjustment: 25d);
        AssertNear(100d, aboveMaximum.NaturalResponseScore, "最大场景 Natural 总分应为 100");
        AssertNear(25d, aboveMaximum.PersistentAdjustment, "结果应保留原始持久调整值");
        AssertNear(100d, aboveMaximum.EffectiveResponseScore, "有效总分应封顶到 100");
        AssertNear(100d, aboveMaximum.Breakdown.NaturalTotal, "Breakdown NaturalTotal 应与结果一致");
        AssertNear(25d, aboveMaximum.Breakdown.PersistentAdjustment, "Breakdown 调整值应与结果一致");
        AssertNear(100d, aboveMaximum.Breakdown.EffectiveTotal, "Breakdown EffectiveTotal 应与结果一致");

        ResponseScoreResult belowMinimum = ResponseScoreCalculator.Calculate(
            maximumSnapshot,
            new EvaluationOptions(),
            persistentAdjustment: -200d);
        AssertNear(0d, belowMinimum.EffectiveResponseScore, "有效总分应封底到 0");

        ResponseScoreResult noDoubleAdjustment = ResponseScoreCalculator.Calculate(
            CreateSnapshot(roundElapsedTime: TimeSpan.FromMinutes(10)),
            new EvaluationOptions(),
            persistentAdjustment: 3d);
        AssertNear(2d, noDoubleAdjustment.NaturalResponseScore, "基础总分错误");
        AssertNear(3d, noDoubleAdjustment.PersistentAdjustment, "持久调整原始值错误");
        AssertNear(5d, noDoubleAdjustment.EffectiveResponseScore, "持久调整不应被重复相加");
    }

    private static void ThreatTrendUsesHistoryBoundaries()
    {
        EvaluationOptions options = new EvaluationOptions();
        DateTime now = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);

        ControlAssessment insufficient = AssessControl(
            CreateThreatSnapshot(now, 0d),
            new EvaluationHistory(),
            options);
        AssertEqual(ThreatTrend.INSUFFICIENT, insufficient.ThreatTrend, "历史不足时 Threat Trend 错误");
        AssertEqual(null, insufficient.FiveMinutesAgoThreat, "历史不足时旧 Threat 应为空");
        AssertNear(0d, insufficient.ThreatDelta, "历史不足时 Threat Delta 应为零");

        ControlAssessment improving = AssessThreatTrend(now, target, 0d, 50d);
        AssertEqual(ThreatTrend.IMPROVING, improving.ThreatTrend, "delta=-5 边界应为 IMPROVING");
        AssertNear(-5d, improving.ThreatDelta, "IMPROVING 的 Threat Delta 错误");

        ControlAssessment worsening = AssessThreatTrend(now, target, 50d, 0d);
        AssertEqual(ThreatTrend.WORSENING, worsening.ThreatTrend, "delta=+5 边界应为 WORSENING");
        AssertNear(5d, worsening.ThreatDelta, "WORSENING 的 Threat Delta 错误");

        ControlAssessment stalledHigh = AssessThreatTrend(now, target, 100d, 60d);
        AssertEqual(ThreatTrend.STALLED_HIGH, stalledHigh.ThreatTrend, "高 Threat 且小幅变化应为 STALLED_HIGH");

        ControlAssessment stable = AssessThreatTrend(now, target, 0d, 0d);
        AssertEqual(ThreatTrend.STABLE, stable.ThreatTrend, "小幅低 Threat 变化应为 STABLE");
    }

    private static void FoundationStrengthUsesExactBoundaries()
    {
        (int Foundation, int Hostile, FoundationStrength Expected)[] cases =
        {
            (45, 55, FoundationStrength.STRONG),
            (4499, 5501, FoundationStrength.ADEQUATE),
            (30, 70, FoundationStrength.ADEQUATE),
            (2999, 7001, FoundationStrength.WEAK),
            (15, 85, FoundationStrength.WEAK),
            (1499, 8501, FoundationStrength.CRITICAL),
        };

        foreach ((int foundation, int hostile, FoundationStrength expected) in cases)
        {
            RoundSnapshot snapshot = CreateSnapshot(
                foundationCombatants: foundation,
                chaosCombatants: hostile);
            ControlAssessment assessment = AssessControl(snapshot);
            AssertEqual(expected, assessment.FoundationStrength, "Foundation Strength 占比边界错误");
        }
    }

    private static void WavePerformanceUsesCompletedWaveBoundaries()
    {
        DateTime now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        (MajorWaveSnapshot Wave, WavePerformance Expected)[] cases =
        {
            (CreateWave(100, 100, true, 0d, now), WavePerformance.GOOD),
            (CreateWave(100, 50, true, 8d, now), WavePerformance.NEUTRAL),
            (CreateWave(100, 25, true, 12d, now), WavePerformance.POOR),
            (CreateWave(100, 80, true, 0d, now, isCatastrophic: true), WavePerformance.CATASTROPHIC),
            (CreateWave(100, 0, true, 15d, now, isCatastrophic: false), WavePerformance.CATASTROPHIC),
        };

        foreach ((MajorWaveSnapshot wave, WavePerformance expected) in cases)
        {
            ControlAssessment assessment = AssessControl(CreateSnapshot(
                majorWaveHistory: new[] { wave }));
            AssertEqual(expected, assessment.WavePerformance, "Wave Performance 边界错误");
        }

        ControlAssessment noCompletedWave = AssessControl(CreateSnapshot(
            majorWaveHistory: new[] { CreateWave(100, 0, false, 15d, now) }));
        AssertEqual(WavePerformance.NEUTRAL, noCompletedWave.WavePerformance, "无已完成波次应为 NEUTRAL");
    }

    private static void BattlefieldMomentumUsesRecentLossBoundaries()
    {
        ControlAssessment positive = AssessControl(CreateSnapshot(
            recentFoundationDeaths120s: 1,
            recentHostileDeaths120s: 3));
        AssertEqual(
            BattlefieldMomentum.FOUNDATION_POSITIVE,
            positive.BattlefieldMomentum,
            "敌方损失达到 3 且领先 2 时应为正面动量");

        ControlAssessment negative = AssessControl(CreateSnapshot(
            recentFoundationDeaths120s: 3,
            recentHostileDeaths120s: 1));
        AssertEqual(
            BattlefieldMomentum.FOUNDATION_NEGATIVE,
            negative.BattlefieldMomentum,
            "Foundation 损失达到 3 且领先 2 时应为负面动量");

        ControlAssessment oneToZero = AssessControl(CreateSnapshot(
            recentFoundationDeaths120s: 0,
            recentHostileDeaths120s: 1));
        AssertEqual(
            BattlefieldMomentum.NEUTRAL,
            oneToZero.BattlefieldMomentum,
            "1 比 0 不得产生正面动量");
    }

    private static void ControlStateUsesFixedScenarios()
    {
        DateTime now = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        EvaluationOptions options = new EvaluationOptions();
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);

        EvaluationHistory advantageHistory = new EvaluationHistory();
        advantageHistory.Add(CreateResult(CreateThreatSnapshot(target, 100d)));
        ControlAssessment advantage = AssessControl(
            CreateSnapshot(
                timestamp: now,
                startingScpCount: 1,
                mainScpAlive: 1,
                scpStates: new[] { new ScpSnapshot("SCP-173", true, 0d, 100d) },
                foundationCombatants: 45,
                chaosCombatants: 54,
                recentFoundationDeaths120s: 1,
                recentHostileDeaths120s: 3,
                majorWaveHistory: new[] { CreateWave(100, 100, true, 0d, now.AddSeconds(-1)) }),
            advantageHistory,
            options);
        AssertEqual(ControlState.ADVANTAGE, advantage.ControlState, "明显优势场景应为 ADVANTAGE");
        AssertEqual(4, advantage.PositiveSignals, "明显优势场景正面信号数量错误");
        AssertEqual(0, advantage.NegativeSignals, "明显优势场景不应有负面信号");
        AssertEqual(2, advantage.ControlLevelCap, "ADVANTAGE 上限错误");

        ControlAssessment controlled = AssessControl(CreateSnapshot(
            foundationCombatants: 40,
            chaosCombatants: 60));
        AssertEqual(ControlState.CONTROLLED, controlled.ControlState, "普通拉锯场景应为 CONTROLLED");
        AssertEqual(0, controlled.PositiveSignals, "普通拉锯场景正面信号数量错误");
        AssertEqual(0, controlled.NegativeSignals, "普通拉锯场景负面信号数量错误");

        ControlAssessment uncontrolled = AssessControl(CreateSnapshot(
            foundationCombatants: 20,
            chaosCombatants: 80,
            majorWaveHistory: new[] { CreateWave(100, 25, true, 12d, now) }));
        AssertEqual(ControlState.UNCONTROLLED, uncontrolled.ControlState, "开始失控场景应为 UNCONTROLLED");
        AssertEqual(2, uncontrolled.NegativeSignals, "开始失控场景负面信号数量错误");

        EvaluationHistory severeHistory = new EvaluationHistory();
        severeHistory.Add(CreateResult(CreateSnapshot(
            timestamp: target,
            startingScpCount: 2,
            mainScpAlive: 1)));
        ControlAssessment severe = AssessControl(CreateSnapshot(
            timestamp: now,
            startingScpCount: 2,
            mainScpAlive: 2,
            foundationCombatants: 9,
            chaosCombatants: 91,
            majorWaveHistory: new[] { CreateWave(100, 25, true, 12d, now) }), severeHistory, options);
        AssertEqual(ControlState.UNCONTROLLED, severe.ControlState, "严重失控但未满足硬条件时仍应为 UNCONTROLLED");
        AssertTrue(!severe.CollapseConditionA, "严重失控场景不应满足 Collapse A");
        AssertTrue(!severe.CollapseConditionB, "Natural Score 不足时不应满足 Collapse B");
        AssertTrue(!severe.CollapseConditionC, "单次失败波次不应满足 Collapse C");

        ControlAssessment noFoundation = AssessControl(CreateSnapshot(
            startingScpCount: 1,
            mainScpAlive: 1));
        AssertEqual(ControlState.COLLAPSE, noFoundation.ControlState, "Foundation=0 且 Threat>0 应为 COLLAPSE");
        AssertTrue(noFoundation.CollapseConditionA, "Foundation=0 且 Threat>0 应命中 Collapse A");

        ControlAssessment consecutiveCatastrophic = AssessControl(CreateSnapshot(
            foundationCombatants: 50,
            chaosCombatants: 50,
            majorWaveHistory: new[]
            {
                CreateWave(100, 0, true, 15d, now.AddMinutes(-4)),
                CreateWave(100, 0, true, 15d, now.AddMinutes(-2)),
            }));
        AssertEqual(ControlState.COLLAPSE, consecutiveCatastrophic.ControlState, "连续两波团灭且 Threat 非改善应为 COLLAPSE");
        AssertTrue(consecutiveCatastrophic.CollapseConditionC, "连续两波团灭应命中 Collapse C");
    }

    private static void CollapseConditionBIsTriggeredInIsolation()
    {
        EvaluationOptions options = new EvaluationOptions();
        DateTime now = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);
        EvaluationHistory history = new EvaluationHistory();
        history.Add(CreateResult(CreateSnapshot(
            timestamp: target,
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-173", true, 100d, 100d) },
            foundationCombatants: 1,
            chaosCombatants: 10)));

        RoundSnapshot snapshot = CreateSnapshot(
            timestamp: now,
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-173", true, 100d, 100d) },
            scp0492Count: 6,
            scp079Present: true,
            scp079Tier: 5,
            roundElapsedTime: TimeSpan.FromMinutes(30),
            foundationCombatants: 1,
            chaosCombatants: 10,
            warheadCancellationCount: 2,
            majorWaveHistory: new[] { CreateWave(100, 0, true, 15d, now) });
        ResponseScoreResult score = ResponseScoreCalculator.Calculate(snapshot, options);
        ControlAssessment assessment = ControlEvaluator.Assess(snapshot, score, history, options);

        AssertTrue(snapshot.FoundationCombatants > 0, "Collapse B 回归场景必须让 Collapse A 失效");
        AssertTrue(score.Breakdown.FoundationCombatShare < 0.10d, "Collapse B 回归场景 Foundation 占比应低于 10%");
        AssertTrue(score.NaturalResponseScore >= 65d, "Collapse B 回归场景 Natural Score 应达到 65");
        AssertEqual(ThreatTrend.WORSENING, assessment.ThreatTrend, "Collapse B 回归场景应为 WORSENING");
        AssertTrue(!assessment.CollapseConditionA, "Collapse B 回归场景不得命中 Collapse A");
        AssertTrue(assessment.CollapseConditionB, "Collapse B 回归场景必须命中 Collapse B");
        AssertTrue(!assessment.CollapseConditionC, "Collapse B 回归场景不得命中 Collapse C");
        AssertEqual(ControlState.COLLAPSE, assessment.ControlState, "Collapse B 回归场景应为 COLLAPSE");
    }

    private static void CollapseBRequiresNaturalScore()
    {
        EvaluationOptions options = new EvaluationOptions();
        DateTime now = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);
        EvaluationHistory history = new EvaluationHistory();
        history.Add(CreateResult(CreateSnapshot(
            timestamp: target,
            foundationCombatants: 10,
            chaosCombatants: 0)));

        ResponseScoreResult score = CreateSyntheticScore(
            naturalScore: 60d,
            effectiveScore: 60d,
            scpThreatTotal: 10d,
            foundationCombatShare: 0.09d);
        ControlAssessment assessment = ControlEvaluator.Assess(
            CreateSnapshot(
                timestamp: now,
                foundationCombatants: 1,
                chaosCombatants: 10),
            score,
            history,
            options);

        AssertEqual(ThreatTrend.WORSENING, assessment.ThreatTrend, "低 Foundation 回归场景应为 WORSENING");
        AssertTrue(!assessment.CollapseConditionA, "Foundation 非零时不得命中 Collapse A");
        AssertTrue(!assessment.CollapseConditionB, "Natural Score 低于 65 时不得命中 Collapse B");
        AssertTrue(assessment.ControlState != ControlState.COLLAPSE, "低 Natural Score 不得单凭 Foundation 低判定 COLLAPSE");
    }

    private static void ImprovingThreatBlocksCollapseC()
    {
        EvaluationOptions options = new EvaluationOptions();
        DateTime now = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);
        EvaluationHistory history = new EvaluationHistory();
        DlrcEvaluationResult previous = DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(
                timestamp: target,
                foundationCombatants: 50,
                chaosCombatants: 50),
            new EvaluationHistory(),
            options,
            CreateSyntheticScore(10d, 10d, 10d, 0.50d));
        history.Add(previous);

        ControlAssessment assessment = ControlEvaluator.Assess(
            CreateSnapshot(
                timestamp: now,
                foundationCombatants: 50,
                chaosCombatants: 50,
                majorWaveHistory: new[]
                {
                    CreateWave(100, 0, true, 15d, now.AddMinutes(-4)),
                    CreateWave(100, 0, true, 15d, now.AddMinutes(-2)),
                }),
            CreateSyntheticScore(10d, 10d, 0d, 0.50d),
            history,
            options);

        AssertEqual(ThreatTrend.IMPROVING, assessment.ThreatTrend, "Threat 下降至少 5 时应为 IMPROVING");
        AssertTrue(!assessment.CollapseConditionC, "Threat Improving 时不得用 Collapse C 判定崩溃");
        AssertTrue(assessment.ControlState != ControlState.COLLAPSE, "Threat Improving 应阻止 Collapse C 造成崩溃");
    }

    private static void HighScoresRespectControlCaps()
    {
        EvaluationOptions options = new EvaluationOptions();
        ResponseScoreResult controlledScore = CreateSyntheticScore(95d, 95d, 0d, 0.50d);
        RoundSnapshot controlledSnapshot = CreateSnapshot(
            foundationCombatants: 50,
            chaosCombatants: 50);
        DlrcEvaluationResult controlled = DlrcEvaluator.EvaluateWithScore(
            controlledSnapshot,
            new EvaluationHistory(),
            options,
            controlledScore);
        AssertNear(95d, controlled.NaturalResponseScore, "Controlled 高分场景 NaturalScore 错误");
        AssertEqual(ControlState.CONTROLLED, controlled.ControlState, "高分 Controlled 场景状态错误");
        AssertEqual(3, controlled.FinalLevel, "Theoretical=5 且 CONTROLLED 时 Final 应为 L3");

        DateTime now = controlledSnapshot.Timestamp;
        DateTime target = now.AddSeconds(-options.ThreatTrendWindowSeconds);
        EvaluationHistory advantageHistory = new EvaluationHistory();
        advantageHistory.Add(DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(timestamp: target, foundationCombatants: 50, chaosCombatants: 50),
            new EvaluationHistory(),
            options,
            CreateSyntheticScore(10d, 10d, 10d, 0.50d)));
        ResponseScoreResult advantageScore = CreateSyntheticScore(95d, 95d, 0d, 0.50d);
        DlrcEvaluationResult advantage = DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(
                timestamp: now,
                foundationCombatants: 50,
                chaosCombatants: 50,
                recentFoundationDeaths120s: 1,
                recentHostileDeaths120s: 3),
            advantageHistory,
            options,
            advantageScore);
        AssertNear(95d, advantage.NaturalResponseScore, "Advantage 高分场景 NaturalScore 错误");
        AssertEqual(ControlState.ADVANTAGE, advantage.ControlState, "高分 Advantage 场景状态错误");
        AssertEqual(2, advantage.FinalLevel, "Theoretical=5 且 ADVANTAGE 时 Final 应为 L2");

        RoundSnapshot uncontrolledSnapshot = CreateSnapshot(
            foundationCombatants: 10,
            chaosCombatants: 90,
            majorWaveHistory: new[] { CreateWave(100, 25, true, 12d, now) });
        DlrcEvaluationResult uncontrolled = DlrcEvaluator.EvaluateWithScore(
            uncontrolledSnapshot,
            new EvaluationHistory(),
            options,
            CreateSyntheticScore(95d, 95d, 0d, 0.10d));
        AssertEqual(ControlState.UNCONTROLLED, uncontrolled.ControlState, "高分失控场景状态错误");
        AssertEqual(4, uncontrolled.FinalLevel, "Theoretical=5 且 UNCONTROLLED 时 Final 应为 L4");

        DlrcEvaluationResult collapse = DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(foundationCombatants: 0),
            new EvaluationHistory(),
            options,
            CreateSyntheticScore(95d, 95d, 10d, 0.50d));
        AssertEqual(ControlState.COLLAPSE, collapse.ControlState, "高分崩溃场景状态错误");
        AssertEqual(5, collapse.FinalLevel, "Theoretical=5 且 COLLAPSE 时 Final 应为 L5");

        DlrcEvaluationResult lowUncontrolled = DlrcEvaluator.EvaluateWithScore(
            uncontrolledSnapshot,
            new EvaluationHistory(),
            options,
            CreateSyntheticScore(22d, 22d, 0d, 0.10d));
        AssertEqual(1, lowUncontrolled.TheoreticalLevel, "Effective=22 的 C 档理论等级应为 L1");
        AssertEqual(1, lowUncontrolled.FinalLevel, "Theoretical=1 且 UNCONTROLLED 时 Final 应仍为 L1");
    }

    private static void LowTheoreticalLevelIsNotRaisedByCollapse()
    {
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(
            CreateSnapshot(
                startingScpCount: 1,
                mainScpAlive: 1,
                roundElapsedTime: TimeSpan.FromMinutes(15)),
            new EvaluationHistory(),
            new EvaluationOptions());

        AssertEqual(2, result.TheoreticalLevel, "低分 Collapse 场景理论等级应为 L2");
        AssertEqual(ControlState.COLLAPSE, result.ControlState, "低分场景应仍为 COLLAPSE");
        AssertEqual(2, result.FinalLevel, "Control 不得抬高低理论等级");
    }

    private static void SequentialEvaluationHasNoLevelLag()
    {
        EvaluationOptions options = new EvaluationOptions();
        DateTime firstTimestamp = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        EvaluationHistory history = new EvaluationHistory();
        DlrcEvaluationResult first = DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(
                timestamp: firstTimestamp,
                foundationCombatants: 10,
                chaosCombatants: 90,
                majorWaveHistory: new[] { CreateWave(100, 25, true, 12d, firstTimestamp) }),
            history,
            options,
            CreateSyntheticScore(75d, 75d, 0d, 0.10d));
        history.Add(first);

        DlrcEvaluationResult second = DlrcEvaluator.EvaluateWithScore(
            CreateSnapshot(
                timestamp: firstTimestamp.AddSeconds(30),
                foundationCombatants: 40,
                chaosCombatants: 60),
            history,
            options,
            CreateSyntheticScore(50d, 50d, 0d, 0.50d));

        AssertEqual("DLRC-C4", first.Code, "第一次评估应得到 C4");
        AssertEqual(ControlState.UNCONTROLLED, first.ControlState, "第一次评估应为 UNCONTROLLED");
        AssertEqual("DLRC-C2", second.Code, "第二次评估应直接从 C4 降到 C2");
        AssertEqual(2, second.FinalLevel, "第二次评估不得插入等级滞后");
    }

    private static void EvaluationHistoryMaintainsRingBufferContract()
    {
        DateTime start = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        EvaluationHistory history = new EvaluationHistory(2);
        history.Add(CreateResult(CreateSnapshot(roundId: 1, timestamp: start)));
        history.Add(CreateResult(CreateSnapshot(roundId: 2, timestamp: start.AddMinutes(1))));
        history.Add(CreateResult(CreateSnapshot(roundId: 3, timestamp: start.AddMinutes(2))));

        AssertEqual(2, history.Count, "Ring Buffer 超容量后 Count 错误");
        AssertEqual(2L, history.Items[0].RoundId, "Ring Buffer 未淘汰最旧结果");
        AssertEqual(3L, history.Items[1].RoundId, "Ring Buffer 最新结果顺序错误");
        AssertEqual(3L, history.LatestValid!.RoundId, "LatestValid 未返回最新有效结果");
        AssertReadOnly(history.Items, "EvaluationHistory.Items 应为只读集合");

        DlrcEvaluationResult? selected;
        AssertTrue(
            history.TryGetThreatAtOrBefore(start.AddMinutes(1).AddSeconds(30), out selected),
            "历史应能查询目标时间之前的最近结果");
        AssertTrue(selected is not null, "历史查询成功时结果不得为空");
        AssertEqual(2L, selected!.RoundId, "历史时间查询返回了错误结果");

        EvaluationHistory fallbackCapacity = new EvaluationHistory(0);
        for (int index = 0; index < 21; index++)
        {
            fallbackCapacity.Add(CreateResult(CreateSnapshot(
                roundId: index + 1,
                timestamp: start.AddSeconds(index))));
        }

        AssertEqual(20, fallbackCapacity.Count, "capacity<=0 时应回退到 20");
        history.Clear();
        AssertEqual(0, history.Count, "Clear 后 Count 应为零");
        AssertEqual(0, history.Items.Count, "Clear 后 Items 应为空");
        AssertEqual(null, history.LatestValid, "Clear 后 LatestValid 应为空");

        EvaluationHistory invalidOnly = new EvaluationHistory();
        DlrcEvaluationResult valid = CreateResult(CreateSnapshot());
        invalidOnly.Add(CreateInvalidResult(valid));
        AssertEqual(0, invalidOnly.Count, "失败结果不得发布到历史");
        invalidOnly.Add(valid);
        invalidOnly.Add(CreateInvalidResult(valid));
        AssertEqual(1, invalidOnly.Count, "失败结果不得覆盖有效历史");
    }

    private static void EvaluationHistoryRetainsExactlyTwentyEntries()
    {
        DateTime start = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        EvaluationHistory history = new EvaluationHistory(20);
        for (int index = 0; index < 25; index++)
        {
            history.Add(CreateResult(CreateSnapshot(
                roundId: index + 1,
                timestamp: start.AddSeconds(index * 30))));
        }

        AssertEqual(20, history.Count, "连续 25 次评估后历史必须保持 20 条");
        AssertEqual(6L, history.Items[0].RoundId, "Ring Buffer 应移除最老的 5 条记录");
        AssertEqual(25L, history.Items[19].RoundId, "Ring Buffer 应保留最新记录");

        DlrcEvaluationResult? selected;
        AssertTrue(
            history.TryGetThreatAtOrBefore(start.AddMinutes(5), out selected),
            "保留的历史应能查询五分钟附近记录");
        AssertTrue(selected is not null, "五分钟附近查询结果不得为空");
        AssertEqual(11L, selected!.RoundId, "五分钟查询应返回最近且不晚于目标时间的记录");
    }

    private static void EvaluationDoesNotPublishToHistory()
    {
        EvaluationHistory history = new EvaluationHistory();
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(
            CreateSnapshot(),
            history,
            new EvaluationOptions());

        AssertTrue(result.IsValid, "正常评估结果应标记为有效");
        AssertEqual(0, history.Count, "纯逻辑 Evaluate 不得偷偷写入 history");
    }

    private static void ResultCodeUsesLockedTierWithoutCrisisTag()
    {
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(
            CreateSnapshot(
                populationTier: PopulationTier.B,
                roundStartPopulation: 999),
            new EvaluationHistory(),
            new EvaluationOptions());

        AssertEqual("DLRC-B0", result.Code, "结果代码格式或锁定 PopulationTier 错误");
        AssertEqual(PopulationTier.B, result.PopulationTier, "结果未使用 Round Core 锁定档位");
        AssertEqual(7, result.Code.Length, "结果代码长度错误");
        AssertTrue(!result.Code.Contains("-BIO", StringComparison.Ordinal), "结果代码不得带危机标签");
    }

    private static void EvaluationScheduleUses391SecondStart()
    {
        AssertNear(1d, EvaluationSchedule.GetInitialDelaySeconds(TimeSpan.FromSeconds(390), 391), "390 秒时应等待 1 秒");
        AssertNear(0d, EvaluationSchedule.GetInitialDelaySeconds(TimeSpan.FromSeconds(391), 391), "391 秒时应立即评估");
        AssertNear(30d, EvaluationSchedule.GetIntervalSeconds(30), "评估间隔应保持 30 秒");
        AssertTrue(EvaluationSchedule.IsDue(TimeSpan.FromSeconds(391), 391), "391 秒应达到首次评估时间");
        AssertTrue(!EvaluationSchedule.IsDue(TimeSpan.FromSeconds(390.99), 391), "390.99 秒不应提前评估");
        AssertNear(30d, EvaluationSchedule.GetNextDelaySeconds(TimeSpan.FromSeconds(391), 391, 30), "391 秒后的下一次目标应为 421 秒");
        AssertNear(29.8d, EvaluationSchedule.GetNextDelaySeconds(TimeSpan.FromSeconds(421.2), 391, 30), "评估耗时不应把后续目标整体向后漂移");
        AssertNear(30d, EvaluationSchedule.GetNextDelaySeconds(TimeSpan.FromSeconds(451), 391, 30), "451 秒后的下一次目标应为 481 秒");
    }

    private static void BattlefieldMomentumTracksWindowAndCleanup()
    {
        DateTime now = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        BattlefieldMomentumTracker tracker = new BattlefieldMomentumTracker();
        tracker.RecordDeath(now.AddSeconds(-121), BattlefieldDeathCategory.Foundation);
        tracker.RecordDeath(now.AddSeconds(-120), BattlefieldDeathCategory.Foundation);
        tracker.RecordDeath(now.AddSeconds(-30), BattlefieldDeathCategory.HostileHuman);
        tracker.RecordDeath(now.AddSeconds(-10), BattlefieldDeathCategory.MainScp);

        BattlefieldMomentumSnapshot snapshot = tracker.GetSnapshot(now, 120);
        AssertEqual(1, snapshot.FoundationDeaths, "窗口外 Foundation 死亡不应计入");
        AssertEqual(1, snapshot.HostileHumanDeaths, "窗口内敌对人类死亡数量错误");
        AssertEqual(1, snapshot.MainScpDeaths, "窗口内主要 SCP 死亡数量错误");

        tracker.Clear();
        BattlefieldMomentumSnapshot cleared = tracker.GetSnapshot(now, 120);
        AssertEqual(0, cleared.FoundationDeaths, "清理后 Foundation 动量应为空");
        AssertEqual(0, cleared.HostileHumanDeaths, "清理后敌对人类动量应为空");
        AssertEqual(0, cleared.MainScpDeaths, "清理后 SCP 动量应为空");
    }

    private static void EvaluationLogContainsCodeAndScore()
    {
        DlrcEvaluationResult result = CreateResult(CreateSnapshot());
        string log = EvaluationLogFormatter.FormatDetailed(result, result.RoundId);
        AssertTrue(log.Contains(result.Code, StringComparison.Ordinal), "详细日志应包含最终代码");
        AssertTrue(log.Contains("EffectiveResponseScore", StringComparison.Ordinal), "详细日志应包含有效分数字段");
        AssertTrue(log.Contains("ControlState", StringComparison.Ordinal), "详细日志应包含控制状态字段");

        string activation = EvaluationLogFormatter.FormatActivation(391, 30, 0d);
        AssertTrue(activation.Contains("D-LRC EVALUATOR ACTIVATED", StringComparison.Ordinal), "启动日志必须包含验收要求的激活标记");
    }

    private static void InvalidHealthCannotPoisonScore()
    {
        ResponseScoreResult score = ResponseScoreCalculator.Calculate(CreateSnapshot(
            startingScpCount: 2,
            mainScpAlive: 2,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, double.PositiveInfinity, double.PositiveInfinity),
                new ScpSnapshot("SCP-096", true, 50d, 100d),
            }), new EvaluationOptions());

        AssertNear(2.5d, score.Breakdown.ScpHealth, "异常 Health 数据应被跳过而保留有效实体分数");
        AssertFinite(score.NaturalResponseScore, "Natural Response Score 不得为 NaN 或 Infinity");
        AssertFinite(score.EffectiveResponseScore, "Effective Response Score 不得为 NaN 或 Infinity");
        AssertFinite(score.Breakdown.ScpThreatTotal, "SCP Threat 不得为 NaN 或 Infinity");
        AssertFinite(score.Breakdown.FoundationPressureTotal, "Foundation Pressure 不得为 NaN 或 Infinity");
    }

    private static void EvaluationLogContainsEveryRecalculationComponent()
    {
        DateTime timestamp = new DateTime(2026, 8, 23, 12, 10, 0, DateTimeKind.Utc);
        RoundSnapshot snapshot = CreateSnapshot(
            roundId: 7,
            timestamp: timestamp,
            roundElapsedTime: TimeSpan.FromMinutes(20),
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[]
            {
                new ScpSnapshot("SCP-173", true, 80d, 100d, 20d, 50d),
            },
            scp0492Count: 3,
            scp079Present: true,
            scp079Tier: 4,
            currentOnlinePlayers: 10,
            foundationCombatants: 4,
            chaosCombatants: 3,
            otherHostileCombatants: 1,
            eligibleSpectators: 2,
            warheadCancellationCount: 1,
            majorWaveHistory: new[]
            {
                CreateWave(10, 8, true, 4d, timestamp.AddMinutes(-2), timestamp),
            },
            recentFoundationDeaths120s: 1,
            recentHostileDeaths120s: 3);
        DlrcEvaluationResult result = CreateResult(snapshot);
        string snapshotLog = EvaluationLogFormatter.FormatSnapshot(snapshot);
        string detailedLog = EvaluationLogFormatter.FormatDetailed(result, result.RoundId);

        string[] snapshotTokens =
        {
            "ScpStates=",
            "CurrentHP=80",
            "MaxHP=100",
            "CurrentHume=20",
            "MaxHume=50",
            "Scp079Tier=4",
            "MajorWaveHistory=",
            "StartingCount=10",
            "SurvivingCount=8",
            "WarheadCancellationCount=1",
        };
        foreach (string token in snapshotTokens)
        {
            AssertTrue(snapshotLog.Contains(token, StringComparison.Ordinal), $"Snapshot 日志缺少 {token}");
        }

        string[] detailedTokens =
        {
            "ScpPresence=",
            "ScpHealth=",
            "ZombiePressure=",
            "Scp079Pressure=",
            "ScpThreatTotal=",
            "FoundationCombatShare=",
            "ScpCombatEquivalent=",
            "CombatTotal=",
            "CombatPressure=",
            "SpectatorRatio=",
            "SpectatorPressure=",
            "FoundationPressureTotal=",
            "ReinforcementFailure=",
            "EvaluatedWaveSurvivalRatio=",
            "EvaluatedWaveBaseFailure=",
            "PreviousEvaluatedWaveBaseFailure=",
            "TimePressure=",
            "StrategicHazard=",
            "NaturalTotal=",
            "PersistentAdjustment=",
            "EffectiveTotal=",
            "ThreatTrend=",
            "ThreatDelta=",
            "FoundationStrength=",
            "WavePerformance=",
            "BattlefieldMomentum=",
            "PositiveSignals=",
            "NegativeSignals=",
            "CollapseConditionA=",
            "CollapseConditionB=",
            "CollapseConditionC=",
            "TheoreticalLevel=",
            "ControlLevelCap=",
            "FinalLevel=",
        };
        foreach (string token in detailedTokens)
        {
            AssertTrue(detailedLog.Contains(token, StringComparison.Ordinal), $"详细日志缺少 {token}");
        }
    }

    private static ControlAssessment AssessControl(
        RoundSnapshot snapshot,
        EvaluationHistory? history = null,
        EvaluationOptions? options = null)
    {
        EvaluationOptions actualOptions = options ?? new EvaluationOptions();
        ResponseScoreResult score = ResponseScoreCalculator.Calculate(snapshot, actualOptions);
        return ControlEvaluator.Assess(
            snapshot,
            score,
            history ?? new EvaluationHistory(),
            actualOptions);
    }

    private static ControlAssessment AssessThreatTrend(
        DateTime now,
        DateTime target,
        double currentHealth,
        double previousHealth)
    {
        EvaluationHistory history = new EvaluationHistory();
        history.Add(CreateResult(CreateThreatSnapshot(target, previousHealth)));
        return AssessControl(
            CreateThreatSnapshot(now, currentHealth),
            history);
    }

    private static RoundSnapshot CreateThreatSnapshot(DateTime timestamp, double health)
    {
        return CreateSnapshot(
            timestamp: timestamp,
            startingScpCount: 1,
            mainScpAlive: 1,
            scpStates: new[] { new ScpSnapshot("SCP-173", true, health, 100d) });
    }

    private static DlrcEvaluationResult CreateResult(
        RoundSnapshot snapshot,
        EvaluationHistory? history = null,
        EvaluationOptions? options = null,
        double persistentAdjustment = 0d)
    {
        return DlrcEvaluator.Evaluate(
            snapshot,
            history ?? new EvaluationHistory(),
            options ?? new EvaluationOptions(),
            persistentAdjustment);
    }

    private static ResponseScoreResult CreateSyntheticScore(
        double naturalScore,
        double effectiveScore,
        double scpThreatTotal,
        double foundationCombatShare)
    {
        ResponseBreakdown breakdown = new ResponseBreakdown(
            scpPresence: 0d,
            scpHealth: 0d,
            zombiePressure: 0d,
            scp079Pressure: 0d,
            scpThreatTotal: scpThreatTotal,
            foundationCombatShare: foundationCombatShare,
            scpCombatEquivalent: 0d,
            combatTotal: 100d,
            combatPressure: 0d,
            spectatorRatio: 0d,
            spectatorPressure: 0d,
            foundationPressureTotal: 0d,
            reinforcementFailure: 0d,
            timePressure: 0d,
            strategicHazard: 0d,
            naturalTotal: naturalScore,
            persistentAdjustment: 0d,
            effectiveTotal: effectiveScore,
            evaluatedWaveSurvivalRatio: null,
            evaluatedWaveBaseFailure: null,
            previousEvaluatedWaveBaseFailure: null,
            evaluatedWaveStartingCount: null,
            evaluatedWaveSurvivingCount: null);
        return new ResponseScoreResult(
            breakdown,
            naturalScore,
            0d,
            effectiveScore);
    }

    private static DlrcEvaluationResult CreateInvalidResult(DlrcEvaluationResult source)
    {
        return new DlrcEvaluationResult(
            source.RoundId,
            source.Timestamp,
            source.PopulationTier,
            source.NaturalResponseScore,
            source.PersistentAdjustment,
            source.EffectiveResponseScore,
            source.ResponseBreakdown,
            source.TheoreticalLevel,
            source.ControlAssessment,
            source.ControlState,
            source.FinalLevel,
            isValid: false,
            source.Code);
    }

    private static ResponseBreakdown CalculateBreakdown(
        RoundSnapshot snapshot,
        EvaluationOptions? options = null,
        double persistentAdjustment = 0d)
    {
        ResponseScoreResult result = ResponseScoreCalculator.Calculate(
            snapshot,
            options ?? new EvaluationOptions(),
            persistentAdjustment);
        return result.Breakdown;
    }

    private static RoundSnapshot CreateSnapshot(
        long roundId = 1,
        DateTime? timestamp = null,
        PopulationTier populationTier = PopulationTier.C,
        int roundStartPopulation = 100,
        int startingScpCount = 0,
        int mainScpAlive = 0,
        IEnumerable<ScpSnapshot>? scpStates = null,
        int scp0492Count = 0,
        bool scp079Present = false,
        int scp079Tier = 0,
        int currentOnlinePlayers = 0,
        int foundationCombatants = 0,
        int chaosCombatants = 0,
        int otherHostileCombatants = 0,
        int eligibleSpectators = 0,
        TimeSpan? roundElapsedTime = null,
        bool warheadUnlocked = false,
        bool warheadActive = false,
        bool warheadDetonated = false,
        int warheadCancellationCount = 0,
        IEnumerable<MajorWaveSnapshot>? majorWaveHistory = null,
        int recentFoundationDeaths120s = 0,
        int recentHostileDeaths120s = 0,
        int recentMainScpDeaths120s = 0)
    {
        return new RoundSnapshot(
            roundId: roundId,
            timestamp: timestamp ?? new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc),
            roundElapsedTime: roundElapsedTime ?? TimeSpan.Zero,
            populationTier: populationTier,
            roundStartPopulation: roundStartPopulation,
            startingScpCount: startingScpCount,
            currentOnlinePlayers: currentOnlinePlayers,
            foundationCombatants: foundationCombatants,
            chaosCombatants: chaosCombatants,
            otherHostileCombatants: otherHostileCombatants,
            eligibleSpectators: eligibleSpectators,
            mainScpAlive: mainScpAlive,
            scpStates: scpStates,
            scp0492Count: scp0492Count,
            scp079Present: scp079Present,
            scp079Tier: scp079Tier,
            warheadUnlocked: warheadUnlocked,
            warheadActive: warheadActive,
            warheadDetonated: warheadDetonated,
            warheadCancellationCount: warheadCancellationCount,
            majorWaveHistory: majorWaveHistory,
            recentFoundationDeaths120s: recentFoundationDeaths120s,
            recentHostileDeaths120s: recentHostileDeaths120s,
            recentMainScpDeaths120s: recentMainScpDeaths120s);
    }

    private static MajorWaveSnapshot CreateWave(
        int startingCount,
        int survivingCount,
        bool isEvaluationComplete,
        double baseFailureScore,
        DateTime? startedAt = null,
        DateTime? evaluatedAt = null,
        bool? isCatastrophic = null)
    {
        DateTime start = startedAt ?? new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        return new MajorWaveSnapshot(
            name: "NTF",
            startingCount: startingCount,
            survivingCountAtEvaluation: survivingCount,
            isEvaluationComplete: isEvaluationComplete,
            baseFailureScore: baseFailureScore,
            isCatastrophic: isCatastrophic ?? survivingCount == 0,
            startedAt: start,
            evaluatedAt: evaluatedAt);
    }

    private static void AssertReadOnly<T>(IReadOnlyList<T> values, string message)
    {
        if (values is not IList<T> list || !list.IsReadOnly)
        {
            throw new InvalidOperationException(message);
        }

        try
        {
            list.Add(default!);
            throw new InvalidOperationException($"{message}；实际允许修改");
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void AssertSequence<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string message)
    {
        AssertEqual(expected.Count, actual.Count, message);
        for (int index = 0; index < expected.Count; index++)
        {
            AssertEqual(expected[index], actual[index], message);
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static TException AssertThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"{message}；实际抛出 {exception.GetType().Name}");
        }

        throw new InvalidOperationException($"{message}；实际未抛出异常");
    }

    private static void AssertNear(double expected, double actual, string message, double tolerance = 1e-9)
    {
        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException($"{message}；期望 {expected}，实际 {actual}");
        }
    }

    private static void AssertFinite(double actual, string message)
    {
        if (double.IsNaN(actual) || double.IsInfinity(actual))
        {
            throw new InvalidOperationException($"{message}；实际 {actual}");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}；期望 {expected}，实际 {actual}");
        }
    }
}
