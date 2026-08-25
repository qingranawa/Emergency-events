using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis;
using EmergencyEvents.Crisis.Detectors;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RemoteAdminCommands;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;
using EmergencyEvents.Runtime;

namespace EmergencyEvents.Evaluation.Tests;

internal static class Program
{
    private static int Main(string[] args)
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
            ("SCP-939 只是合法随机候选且 SCP 总数保持正确", ScpRolePolicyUsesRandom939Candidate),
            ("强制重启按顺序清理全部回合状态", RoundRestartResetsAllRoundState),
            ("Primary Wave 五档人数上限符合规格", PrimaryWaveCapsMatchSpecification),
            ("Primary Wave 上限只能截断原版人数", PrimaryWaveCapsNeverExpandVanillaWave),
            ("Primary Wave 始终使用开局锁定档位", PrimaryWaveUsesLockedPopulationTier),
            ("仅在配置启用时取消 Mini-Wave", MiniWaveCancellationRespectsConfiguration),
            ("Mini-Wave 只在刷新边界取消以避免原版重试", MiniWaveCancellationUsesRespawningBoundary),
            ("Late Join 依旧保留在原版选中的名单内", PrimaryWaveCapPreservesVanillaSelection),
            ("Major Wave History 轮转、去重与清理正确", MajorWaveHistoryRollsOverDeduplicatesAndCleansUp),
            ("Primary Wave 按刷新阵营使用 60/15 计时器增量", PrimaryWaveTimerExtensionMapsSpawningAndOpposingTimers),
            ("Foundation Wave 使用动态原版计时器", FoundationWaveUsesDynamicVanillaTimers),
            ("Chaos Wave 使用动态原版计时器", ChaosWaveUsesDynamicVanillaTimers),
            ("原版 Timer Passed 归零才视为 Reset 完成", VanillaResetRequiresFreshTimer),
            ("Primary Wave Timer Extension 为零时不修改计时器", DisabledPrimaryWaveTimerExtensionDoesNotApply),
            ("Mini-Wave 不应用 Timer Extension", MiniWaveDoesNotApplyTimerExtension),
            ("零人 Primary Wave 不应用 Timer Extension", ZeroSpawnPrimaryWaveDoesNotApplyTimerExtension),
            ("未完成 Primary Wave 不应用 Timer Extension", IncompletePrimaryWaveDoesNotApplyTimerExtension),
            ("同一波次不得重复应用 Timer Extension", DuplicateWaveDoesNotApplyTimerExtension),
            ("MTF Primary Wave 被识别为 Timer Extension 目标", NtfPrimaryWaveIsTimerExtensionTarget),
            ("CI Primary Wave 被识别为 Timer Extension 目标", ChaosPrimaryWaveIsTimerExtensionTarget),
            ("Timer Extension 不产生第二次 POST_MAJOR_WAVE", TimerExtensionDoesNotDuplicatePostMajorWave),
            ("非法 Timer Extension 配置回退到安全默认值", InvalidTimerExtensionConfigurationFallsBackSafely),
            ("刷新方和对方可以独立禁用计时器增量", TimerExtensionSidesCanBeDisabledIndependently),
            ("Timer Extension 不保存跨波次累计增量", TimerExtensionDoesNotAccumulateAcrossWaves),
            ("特殊人员事件不应用 Timer Extension", SpecialPersonnelEventDoesNotApplyTimerExtension),
            ("Evaluator 忙碌时只保留一个补算队列项", BusyEvaluatorCoalescesPostMajorWaveQueue),
            ("原版候选只有成功出生玩家才计入实际人数", ActualSpawnedPlayerRequiresSuccessfulNativeRole),
            ("Module 04 发布危机公共契约", CrisisContractsArePublished),
            ("Module 04 发布四个无状态危机判定器", StatelessCrisisDetectorsArePublished),
            ("WAR 按可靠核弹事实判定生命周期", WarUsesReliableWarheadFacts),
            ("BIO 使用各档默认僵尸阈值且不依赖 049 本体", BioUsesTierAwareZombieThresholds),
            ("SYS 与 SEC 只按其自身事实判定", StatelessDetectorsUseOwnFacts),
            ("SEC 覆盖全部人口档位且保留 E 档特殊 L3", SecurityUsesTierAwareThresholds),
            ("CON、END 与地表事实快照契约已发布", StatefulCrisisContractsArePublished),
            ("CON 跟随第二个实际大波并按五分钟检查升级", ContainmentUsesSecondMajorWaveCheckpoints),
            ("CON 的首个检查点从实际大波完成时刻起算", ContainmentUsesActualWaveCompletionTime),
            ("CON 基准使用第二波完成时的客观事实", ContainmentUsesCompletionFactBaseline),
            ("END 仅在核爆后连续地表僵持时升级", EndgameRequiresContinuousSurfaceStalemate),
            ("END 仅接受可靠的 DetonatedAt 事实", EndRequiresReliableDetonationFact),
            ("GOI 只使用未来注册钩子与基金会劣势条件", GoiRequiresRegisteredHostileThirdParty),
            ("CrisisManager 与 Module03 完成事件契约已发布", CrisisManagerContractsArePublished),
            ("CrisisManager 固定排序且保持 Global D-LRC 独立", CrisisManagerBuildsStableAssessment),
            ("RA 命令语法精确识别 D-LRC 子命令", EmergencyEventsCommandSyntaxRecognizesDlrcEvaluate),
            ("RA 状态报告包含人口、响应、危机与积分", DlrcStateReportContainsRequiredFacts),
            ("低人口暂停在本回合不可逆并在下一局重新判定", LowPopulationSuspensionIsRoundLocked),
            ("管理员启停不会在进行中的回合重跑 Round Core", EnableDisableDefersRoundActivation),
            ("命令守卫在低人口暂停时保留查询并拒绝真实评估", CommandGuardPreservesQueriesDuringSuspension),
            ("RA 根命令树精确识别支持的诊断和测试请求", EmergencyEventsCommandSyntaxRecognizesCommandTree),
            ("RA 命令树支持 WaveId 详情与 D-LRC 战局展示模式", EmergencyEventsCommandSyntaxRecognizesWaveIdAndStageModes),
            ("危机诊断复用正式 Detector 且不写入真实评估状态", CrisisDiagnosticsAreReadOnly),
            ("危机 Dry Run 快照只覆盖指定输入而不修改原快照", CrisisDiagnosticSnapshotFactoryPreservesSource),
            ("D-LRC 标准战局报告使用中文字段且不泄漏内部值", DlrcStageReportUsesChineseFields),
            ("完整 D-LRC 代码只接受同一次危机评估", DlrcDisplayCodeRequiresSynchronizedAssessment),
            ("CON 快速检查默认只读而 commit 才推进正式状态", ContainmentDiagnosticCommitIsExplicit),
            ("END 快速模拟使用正式 Detector 且不改变真实时间", EndDiagnosticSimulationUsesIsolatedState),
            ("RA 语法接受 round state 作为 round 查询别名", EmergencyEventsCommandSyntaxRecognizesRoundStateAlias),
            ("FDI 首次结算使用 120 秒回看窗口", FdiInitialSettlementUsesLookbackWindow),
            ("FDI 后续结算只处理上次结算之后的新事件", FdiIncrementalSettlementUsesLastProcessedBoundary),
            ("FDI MTF 变化事件去重且不重复计当前人数", FdiMtfChangesDoNotRepeat),
            ("FDI 战斗方向事件保留正负方向", FdiCombatDirectionIsPreserved),
            ("FDI 危机转换事件进入下一次周期结算", FdiCrisisTransitionsAreSettled),
            ("FDI POST 和 MANUAL 评估只读", FdiSpecialEvaluationsAreReadOnly),
            ("FDI 分数限制范围并正确映射区间", FdiScoreClampsAndMapsBands),
            ("FDI 清理会清除事件、状态和窗口", FdiCleanupClearsRoundState),
            ("FDI 低人口开局与中途降人口不可逆暂停", FdiLowPopulationSuspensionIsRoundLocked),
            ("FDI 默认权重全部可配置且标记为临时平衡值", FdiConfigurationIsExplicit),
            ("FDI RA 支持 disorder/fdi 查询和 dry-run 事件", FdiCommandSyntaxRecognizesCommands),
            ("FDI 06:31 当前 MTF 存量只计算一次", FdiInitialStockIncludesMtfWithoutDoubleCounting),
            ("FDI 06:31 当前 079 与 SYS 不重复计算", FdiInitial079AndSysDoNotDoubleCount),
            ("FDI 06:31 WAR 与核弹状态不重复计算", FdiInitialWarAndWarheadDoNotDoubleCount),
            ("FDI 06:31 当前危机存量只计算一次", FdiInitialCrisisStockIsIncluded),
            ("FDI 首次结算后只应用新事件增量", FdiPostInitializationUsesPureIncrement),
            ("FDI 无效危机评估不推进结算窗口", FdiInvalidCrisisAssessmentDoesNotAdvanceWindow),
            ("FDI RoundId 不一致不推进结算窗口", FdiRoundIdMismatchDoesNotAdvanceWindow),
            ("FDI 无效 Evaluation 不消费事件", FdiInvalidEvaluationDoesNotConsumeEvents),
            ("FDI 失败周期后的下一次成功周期补处理事件", FdiSuccessfulPeriodicProcessesFailedWindow),
            ("FDI FactionAdvantageChanged 默认 Delta 为零", FdiFactionAdvantageDefaultDeltaIsZero),
            ("FDI 资源边界保留未结算事件并限制历史", FdiResourceBoundsPreservePendingEvents),
            ("FDI 1000 次 Periodic 后历史和事件容器有界且不重复消费", FdiResourceBoundsRemainStableAfterThousandPeriodics),
        };

        string requestedModule = args.Length == 0 ? "ALL" : args[0].ToUpperInvariant();
        int total = 0;
        int failed = 0;

        for (int index = 0; index < tests.Length; index++)
        {
            string module = tests[index].Name.StartsWith("FDI", StringComparison.Ordinal)
                ? "FDI"
                : index < 43 ? "M03" : index < 46 ? "M01" : index < 71 ? "M02" : "M04";
            bool isRaTest = tests[index].Name.StartsWith("RA ", StringComparison.Ordinal)
                || tests[index].Name.StartsWith("FDI RA", StringComparison.Ordinal);
            if (requestedModule == "RA" && isRaTest)
            {
                module = "RA";
            }

            if (requestedModule != "ALL" && requestedModule != module)
            {
                continue;
            }

            total++;
            failed += RunTest(module, tests[index].Name, tests[index].Body);
        }

        if (total == 0)
        {
            Console.WriteLine($"Unknown module: {requestedModule}");
            return 2;
        }

        Console.WriteLine($"Total: {total}");
        Console.WriteLine($"Failed: {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static int RunTest(string module, string name, Action body)
    {
        try
        {
            body();
            Console.WriteLine($"[PASS][{module}] {name}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[FAIL][{module}] {name}: {exception.Message}");
            return 1;
        }
    }

    private static void CrisisContractsArePublished()
    {
        Type assemblyType = typeof(DlrcEvaluator);
        AssertTrue(
            assemblyType.Assembly.GetType("EmergencyEvents.Crisis.CrisisTag") is not null,
            "Module 04 必须公开 CrisisTag 契约");
        AssertTrue(
            assemblyType.Assembly.GetType("EmergencyEvents.Crisis.CrisisAssessment") is not null,
            "Module 04 必须公开 CrisisAssessment 契约");
    }

    private static void StatelessCrisisDetectorsArePublished()
    {
        Type assemblyType = typeof(DlrcEvaluator);
        string[] contractNames =
        {
            "EmergencyEvents.Crisis.CrisisOptions",
            "EmergencyEvents.Crisis.ICrisisDetector",
            "EmergencyEvents.Crisis.Detectors.BioCrisisDetector",
            "EmergencyEvents.Crisis.Detectors.SysCrisisDetector",
            "EmergencyEvents.Crisis.Detectors.SecCrisisDetector",
            "EmergencyEvents.Crisis.Detectors.GoiCrisisDetector",
            "EmergencyEvents.Crisis.Detectors.WarCrisisDetector",
        };

        foreach (string contractName in contractNames)
        {
            AssertTrue(
                assemblyType.Assembly.GetType(contractName) is not null,
                $"Module 04 必须公开 {contractName} 契约");
        }
    }

    private static void BioUsesTierAwareZombieThresholds()
    {
        (PopulationTier Tier, int BelowL3, int L3, int L4, int L5)[] cases =
        {
            (PopulationTier.E, 2, 3, 5, 7),
            (PopulationTier.D, 2, 3, 6, 8),
            (PopulationTier.C, 3, 4, 7, 10),
            (PopulationTier.B, 3, 4, 8, 12),
            (PopulationTier.A, 4, 5, 9, 14),
        };
        BioCrisisDetector detector = new BioCrisisDetector();

        foreach ((PopulationTier tier, int belowL3, int level3, int level4, int level5) in cases)
        {
            AssertEqual(
                CrisisSeverity.Inactive,
                Detect(detector, CreateSnapshot(populationTier: tier, scp0492Count: belowL3)).Severity,
                $"{tier} 档 BIO 的 L3 下方不应激活");
            AssertEqual(
                CrisisSeverity.Level3,
                Detect(detector, CreateSnapshot(populationTier: tier, scp0492Count: level3)).Severity,
                $"{tier} 档 BIO 的 L3 边界错误");
            AssertEqual(
                CrisisSeverity.Level4,
                Detect(detector, CreateSnapshot(populationTier: tier, scp0492Count: level4)).Severity,
                $"{tier} 档 BIO 的 L4 边界错误");
            AssertEqual(
                CrisisSeverity.Level5,
                Detect(detector, CreateSnapshot(populationTier: tier, scp0492Count: level5)).Severity,
                $"{tier} 档 BIO 的 L5 边界错误");
        }
    }

    private static void StatelessDetectorsUseOwnFacts()
    {
        SysCrisisDetector sys = new SysCrisisDetector();
        AssertEqual(CrisisSeverity.Inactive, Detect(sys, CreateSnapshot(scp079Present: false, scp079Tier: 5)).Severity, "079 不存在时 SYS 必须关闭");
        AssertEqual(CrisisSeverity.Inactive, Detect(sys, CreateSnapshot(scp079Present: true, scp079Tier: 2)).Severity, "079 Tier2 不得触发 SYS");
        AssertEqual(CrisisSeverity.Level3, Detect(sys, CreateSnapshot(scp079Present: true, scp079Tier: 3)).Severity, "079 Tier3 必须为 SYS L3");
        AssertEqual(CrisisSeverity.Level4, Detect(sys, CreateSnapshot(scp079Present: true, scp079Tier: 4)).Severity, "079 Tier4 必须为 SYS L4");
        AssertEqual(CrisisSeverity.Level5, Detect(sys, CreateSnapshot(scp079Present: true, scp079Tier: 5)).Severity, "079 Tier5 必须为 SYS L5");
        AssertEqual(CrisisSeverity.Inactive, Detect(sys, CreateSnapshot(scp079Present: true, scp079Tier: 6)).Severity, "非法 079 Tier 不得被误判为 SYS L5");

        SecCrisisDetector sec = new SecCrisisDetector();
        AssertEqual(CrisisSeverity.Inactive, Detect(sec, CreateSnapshot(populationTier: PopulationTier.C, foundationCombatants: 0)).Severity, "没有敌对威胁时 SEC 必须关闭");
        AssertEqual(CrisisSeverity.Level3, Detect(sec, CreateSnapshot(populationTier: PopulationTier.C, foundationCombatants: 2, mainScpAlive: 1)).Severity, "C 档基金会两人且存在 SCP 应为 SEC L3");
        AssertEqual(CrisisSeverity.Level4, Detect(sec, CreateSnapshot(populationTier: PopulationTier.C, foundationCombatants: 1, mainScpAlive: 1)).Severity, "C 档基金会一人且存在 SCP 应为 SEC L4");
        AssertEqual(CrisisSeverity.Level5, Detect(sec, CreateSnapshot(populationTier: PopulationTier.C, foundationCombatants: 0, mainScpAlive: 1)).Severity, "基金会为零且存在 SCP 应为 SEC L5");

    }

    private static void WarUsesReliableWarheadFacts()
    {
        WarCrisisDetector detector = new WarCrisisDetector();
        DateTime timestamp = new DateTime(2026, 8, 24, 16, 0, 0, DateTimeKind.Utc);

        AssertEqual(
            CrisisSeverity.Inactive,
            Detect(detector, CreateSnapshot(timestamp: timestamp, warheadUnlocked: false)).Severity,
            "Locked 核弹必须保持 WAR inactive");
        AssertEqual(
            CrisisSeverity.Level3,
            Detect(detector, CreateSnapshot(timestamp: timestamp, warheadUnlocked: true)).Severity,
            "Unlocked 核弹必须为 WAR L3");
        CrisisDetectionResult active = Detect(
            detector,
            CreateSnapshot(timestamp: timestamp, warheadUnlocked: true, warheadActive: true));
        AssertEqual(CrisisSeverity.Level4, active.Severity, "Countdown Active 核弹必须为 WAR L4");
        AssertTrue(active.Severity != CrisisSeverity.Level5, "没有可靠 L5 事实时不得猜测 WAR L5");
        AssertEqual(
            CrisisSeverity.Inactive,
            Detect(
                detector,
                CreateSnapshot(
                    timestamp: timestamp,
                    warheadUnlocked: true,
                    warheadActive: true,
                    warheadDetonated: true)).Severity,
            "Detonated 后 WAR 必须 inactive");
    }

    private static void SecurityUsesTierAwareThresholds()
    {
        (PopulationTier Tier, int L3Foundation, int L4Foundation)[] cases =
        {
            (PopulationTier.E, 1, 1),
            (PopulationTier.D, 2, 1),
            (PopulationTier.C, 2, 1),
            (PopulationTier.B, 4, 2),
            (PopulationTier.A, 5, 2),
        };
        SecCrisisDetector detector = new SecCrisisDetector();

        foreach ((PopulationTier tier, int level3Foundation, int level4Foundation) in cases)
        {
            CrisisSeverity expectedAtLevel3 = tier == PopulationTier.E
                ? CrisisSeverity.Level3
                : CrisisSeverity.Level3;
            AssertEqual(
                expectedAtLevel3,
                Detect(detector, CreateSnapshot(populationTier: tier, foundationCombatants: level3Foundation, chaosCombatants: 1)).Severity,
                $"{tier} 档 SEC L3 边界错误");
            if (tier != PopulationTier.E)
            {
                AssertEqual(
                    CrisisSeverity.Level4,
                    Detect(detector, CreateSnapshot(populationTier: tier, foundationCombatants: level4Foundation, chaosCombatants: 1)).Severity,
                    $"{tier} 档 SEC L4 边界错误");
            }

            AssertEqual(
                CrisisSeverity.Level5,
                Detect(detector, CreateSnapshot(populationTier: tier, foundationCombatants: 0, chaosCombatants: 1)).Severity,
                $"{tier} 档 SEC L5 边界错误");
        }
    }

    private static CrisisDetectionResult Detect(ICrisisDetector detector, RoundSnapshot snapshot)
    {
        return detector.Detect(
            snapshot,
            CreateResult(snapshot),
            new CrisisState(),
            new CrisisContext());
    }

    private static void StatefulCrisisContractsArePublished()
    {
        Type assemblyType = typeof(DlrcEvaluator);
        string[] typeNames =
        {
            "EmergencyEvents.Crisis.Detectors.ConCrisisDetector",
            "EmergencyEvents.Crisis.Detectors.EndCrisisDetector",
        };
        string[] snapshotProperties =
        {
            "HostileThirdPartyActive",
            "HostileThirdPartyCombatants",
            "SurfaceFoundationCombatants",
            "SurfaceChaosCombatants",
            "SurfaceMainScp",
            "SurfaceOtherHostiles",
        };

        foreach (string typeName in typeNames)
        {
            AssertTrue(assemblyType.Assembly.GetType(typeName) is not null, $"缺少 {typeName}");
        }

        AssertTrue(typeof(CrisisAssessment).GetProperty("Code") is not null, "CrisisAssessment 必须公开最终代码");
        AssertTrue(typeof(CrisisAssessment).GetProperty("ActiveTags") is not null, "CrisisAssessment 必须公开激活标签");
        AssertTrue(typeof(CrisisAssessment).GetMethod("IsActive") is not null, "CrisisAssessment 必须支持标签查询");

        foreach (string propertyName in snapshotProperties)
        {
            AssertTrue(typeof(RoundSnapshot).GetProperty(propertyName) is not null, $"RoundSnapshot 缺少事实字段 {propertyName}");
        }

        AssertTrue(typeof(MajorWaveSnapshot).GetProperty("CompletedAt") is not null, "MajorWaveSnapshot 必须保留实际波次完成时刻");
    }

    private static void ContainmentUsesSecondMajorWaveCheckpoints()
    {
        DateTime secondWaveAt = new DateTime(2026, 8, 24, 14, 0, 0, DateTimeKind.Utc);
        MajorWaveSnapshot[] waves =
        {
            CreateWave(6, 6, false, 0d, secondWaveAt.AddMinutes(-5), scpCombatEquivalentAtCompletion: 13d / 3d),
            CreateWave(6, 6, false, 0d, secondWaveAt, scpCombatEquivalentAtCompletion: 13d / 3d),
        };
        ConCrisisDetector detector = new ConCrisisDetector();
        CrisisState state = new CrisisState();

        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: secondWaveAt, mainScpAlive: 3, scp0492Count: 4, majorWaveHistory: waves), state).Severity, "第二波刚结束不能触发 CON");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: secondWaveAt.AddMinutes(4).AddSeconds(59), mainScpAlive: 3, scp0492Count: 4, majorWaveHistory: waves), state).Severity, "CON 五分钟前不能触发");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: secondWaveAt.AddMinutes(5), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "SCP 当量下降至少 1.0 时 CON 必须解除");
        AssertEqual(CrisisSeverity.Level3, Detect(detector, CreateSnapshot(timestamp: secondWaveAt.AddMinutes(10), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "第一次收容失败必须为 CON L3");
        AssertEqual(CrisisSeverity.Level4, Detect(detector, CreateSnapshot(timestamp: secondWaveAt.AddMinutes(15), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "第二次连续收容失败必须为 CON L4");
        AssertEqual(CrisisSeverity.Level5, Detect(detector, CreateSnapshot(timestamp: secondWaveAt.AddMinutes(20), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "第三次连续收容失败必须为 CON L5");
    }

    private static void ContainmentUsesActualWaveCompletionTime()
    {
        DateTime secondWaveStartedAt = new DateTime(2026, 8, 24, 15, 0, 0, DateTimeKind.Utc);
        DateTime secondWaveCompletedAt = secondWaveStartedAt.AddMinutes(2);
        MajorWaveSnapshot[] waves =
        {
            CreateWave(6, 6, false, 0d, secondWaveStartedAt.AddMinutes(-5), scpCombatEquivalentAtCompletion: 3d),
            CreateWave(6, 6, false, 0d, secondWaveStartedAt, completedAt: secondWaveCompletedAt, scpCombatEquivalentAtCompletion: 3d),
        };
        ConCrisisDetector detector = new ConCrisisDetector();
        CrisisState state = new CrisisState();

        Detect(detector, CreateSnapshot(timestamp: secondWaveCompletedAt, mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state);
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: secondWaveCompletedAt.AddMinutes(4).AddSeconds(59), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "CON 不得从波次开始时刻提前计算");
        AssertEqual(CrisisSeverity.Level3, Detect(detector, CreateSnapshot(timestamp: secondWaveCompletedAt.AddMinutes(5), mainScpAlive: 3, scp0492Count: 0, majorWaveHistory: waves), state).Severity, "CON 必须从实际波次完成五分钟后执行第一次检查");
    }

    private static void ContainmentUsesCompletionFactBaseline()
    {
        DateTime secondWaveCompletedAt = new DateTime(2026, 8, 24, 15, 30, 0, DateTimeKind.Utc);
        MajorWaveSnapshot[] waves =
        {
            CreateWave(6, 6, false, 0d, secondWaveCompletedAt.AddMinutes(-5), scpCombatEquivalentAtCompletion: 8d),
            CreateWave(6, 6, false, 0d, secondWaveCompletedAt, scpCombatEquivalentAtCompletion: 10d),
        };
        ConCrisisDetector detector = new ConCrisisDetector();
        CrisisState state = new CrisisState();

        AssertEqual(
            CrisisSeverity.Inactive,
            Detect(
                detector,
                CreateSnapshot(
                    timestamp: secondWaveCompletedAt,
                    mainScpAlive: 1,
                    majorWaveHistory: waves),
                state).Severity,
            "第二波完成时的 baseline 必须来自波次事实，而不是第一次 Detector 快照");

        AssertEqual(
            CrisisSeverity.Inactive,
            Detect(
                detector,
                CreateSnapshot(
                    timestamp: secondWaveCompletedAt.AddMinutes(5),
                    mainScpAlive: 8,
                    majorWaveHistory: waves),
                state).Severity,
            "CON 首次检查必须继续使用第二波完成时保存的 baseline");
    }

    private static void EndgameRequiresContinuousSurfaceStalemate()
    {
        DateTime detonationAt = new DateTime(2026, 8, 24, 14, 30, 0, DateTimeKind.Utc);
        EndCrisisDetector detector = new EndCrisisDetector();
        CrisisState state = new CrisisState();

        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: detonationAt, warheadDetonated: true), state).Severity, "核爆后没有地表敌对共存时 END 必须关闭");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: detonationAt, warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "地表僵持刚开始时 END 必须关闭");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(4).AddSeconds(59), warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "END 4:59 必须关闭");
        AssertEqual(CrisisSeverity.Level3, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(5), warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "END 5:00 必须为 L3");
        AssertEqual(CrisisSeverity.Level4, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(8), warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "END 8:00 必须为 L4");
        AssertEqual(CrisisSeverity.Level5, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(12), warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "END 12:00 必须为 L5");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(13), warheadDetonated: true), state).Severity, "地表僵持消失时 END 必须重置");
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, CreateSnapshot(timestamp: detonationAt.AddMinutes(18), warheadDetonated: true, surfaceFoundationCombatants: 1, surfaceChaosCombatants: 1), state).Severity, "僵持重新开始时不得沿用旧 END 计时");
    }

    private static void EndRequiresReliableDetonationFact()
    {
        DateTime timestamp = new DateTime(2026, 8, 24, 14, 30, 0, DateTimeKind.Utc);
        RoundSnapshot snapshotWithoutFact = new RoundSnapshot(
            roundId: 1,
            timestamp: timestamp.AddMinutes(5),
            roundElapsedTime: TimeSpan.Zero,
            populationTier: PopulationTier.C,
            roundStartPopulation: 20,
            startingScpCount: 2,
            warheadDetonated: true,
            surfaceFoundationCombatants: 1,
            surfaceChaosCombatants: 1);
        EndCrisisDetector detector = new EndCrisisDetector();

        AssertEqual(
            CrisisSeverity.Inactive,
            Detect(detector, snapshotWithoutFact).Severity,
            "没有可靠 DetonatedAt 时不得把第一次观察时间冒充真实核爆时间");
    }

    private static void GoiRequiresRegisteredHostileThirdParty()
    {
        GoiCrisisDetector detector = new GoiCrisisDetector();
        RoundSnapshot inactiveSnapshot = CreateSnapshot(hostileThirdPartyActive: false, hostileThirdPartyCombatants: 3);
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, inactiveSnapshot, CreateCrisisResult(inactiveSnapshot, 5, FoundationStrength.CRITICAL)).Severity, "没有注册敌对第三方时 GOI 必须关闭");

        RoundSnapshot lowLevelSnapshot = CreateSnapshot(hostileThirdPartyActive: true, hostileThirdPartyCombatants: 3);
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, lowLevelSnapshot, CreateCrisisResult(lowLevelSnapshot, 2, FoundationStrength.CRITICAL)).Severity, "Global Level 低于 3 时 GOI 必须关闭");

        RoundSnapshot adequateSnapshot = CreateSnapshot(hostileThirdPartyActive: true, hostileThirdPartyCombatants: 3);
        AssertEqual(CrisisSeverity.Inactive, Detect(detector, adequateSnapshot, CreateCrisisResult(adequateSnapshot, 3, FoundationStrength.ADEQUATE)).Severity, "基金会并非明显劣势时 GOI 必须关闭");

        RoundSnapshot activeSnapshot = CreateSnapshot(hostileThirdPartyActive: true, hostileThirdPartyCombatants: 3);
        AssertEqual(CrisisSeverity.Level3, Detect(detector, activeSnapshot, CreateCrisisResult(activeSnapshot, 3, FoundationStrength.WEAK)).Severity, "敌对第三方、Global L3 与基金会劣势同时存在时 GOI 应为 L3");
    }

    private static CrisisDetectionResult Detect(ICrisisDetector detector, RoundSnapshot snapshot, DlrcEvaluationResult result)
    {
        return detector.Detect(snapshot, result, new CrisisState(), new CrisisContext());
    }

    private static void CrisisManagerContractsArePublished()
    {
        Type assemblyType = typeof(DlrcEvaluator);
        string[] typeNames =
        {
            "EmergencyEvents.Crisis.CrisisManager",
            "EmergencyEvents.Crisis.DlrcEvaluationCompletedEvent",
            "EmergencyEvents.Crisis.DlrcEvaluationTrigger",
        };

        foreach (string typeName in typeNames)
        {
            AssertTrue(assemblyType.Assembly.GetType(typeName) is not null, $"缺少 {typeName}");
        }
    }

    private static void CrisisManagerBuildsStableAssessment()
    {
        RoundSnapshot snapshot = CreateSnapshot(
            populationTier: PopulationTier.C,
            scp0492Count: 4,
            scp079Present: true,
            scp079Tier: 4,
            warheadUnlocked: true,
            warheadActive: true);
        DlrcEvaluationResult result = CreateCrisisResult(snapshot, 4, FoundationStrength.WEAK);
        CrisisManager manager = new CrisisManager();
        CrisisAssessment assessment = manager.Evaluate(new DlrcEvaluationCompletedEvent(
            1001,
            DlrcEvaluationTrigger.PERIODIC,
            snapshot,
            result))
            ?? throw new InvalidOperationException("成功的 D-LRC 评估必须产生 CrisisAssessment");
        AssertEqual("DLRC-C4-BIO+SYS+WAR", assessment.Code, "完整 D-LRC Code 必须包含 WAR 标签");
        AssertSequence(
            new[] { CrisisTag.BIO, CrisisTag.SYS, CrisisTag.WAR },
            assessment.ActiveTags,
            "危机标签顺序错误");
        AssertEqual(CrisisSeverity.Level3, assessment.GetSeverity(CrisisTag.BIO), "BIO 严重度错误");
        AssertEqual(CrisisSeverity.Level4, assessment.GetSeverity(CrisisTag.SYS), "SYS 严重度错误");
        AssertEqual(CrisisSeverity.Level4, assessment.GetSeverity(CrisisTag.WAR), "核弹倒计时必须产生 WAR L4");

        CrisisAssessment? retained = manager.Evaluate(new DlrcEvaluationCompletedEvent(
            1002,
            DlrcEvaluationTrigger.POST_MAJOR_WAVE,
            snapshot,
            CreateInvalidResult(result)));
        AssertTrue(ReferenceEquals(assessment, retained), "上游无效评估不得覆盖上一份有效 CrisisAssessment");
        manager.CleanupRound();
        AssertTrue(manager.CurrentCrisisAssessment is null, "Round Cleanup 必须清空 CrisisAssessment");
    }

    private static void EmergencyEventsCommandSyntaxRecognizesDlrcEvaluate()
    {
        Type? syntaxType = typeof(DlrcEvaluator).Assembly.GetType(
            "EmergencyEvents.RemoteAdminCommands.EmergencyEventsCommandSyntax");
        AssertTrue(syntaxType is not null, "必须提供 EmergencyEvents RA 命令语法解析器");

        System.Reflection.MethodInfo? isDlrcEvaluate = syntaxType!.GetMethod(
            "IsDlrcEvaluate",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        AssertTrue(isDlrcEvaluate is not null, "RA 命令语法解析器必须提供 IsDlrcEvaluate");

        AssertTrue(
                (bool)isDlrcEvaluate!.Invoke(null, new object[] { new[] { "dlrc", "evaluate" } })!,
            "dlrc evaluate 必须被识别为立即评估命令");
        AssertTrue(
                !(bool)isDlrcEvaluate!.Invoke(null, new object[] { new[] { "dlrc" } })!,
            "不完整的 dlrc 命令不得执行");
        AssertTrue(
                !(bool)isDlrcEvaluate!.Invoke(null, new object[] { new[] { "dlrc", "status" } })!,
            "未知 dlrc 子命令不得执行");

        System.Reflection.MethodInfo? isDlrcState = syntaxType.GetMethod(
            "IsDlrcState",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        AssertTrue(isDlrcState is not null, "RA 命令语法解析器必须提供 IsDlrcState");
        AssertTrue(
            (bool)isDlrcState!.Invoke(null, new object[] { new[] { "dlrc", "state" } })!,
            "dlrc state 必须被识别为状态查询命令");
        AssertTrue(
            !(bool)isDlrcState.Invoke(null, new object[] { new[] { "dlrc", "evaluate", "now" } })!,
            "状态查询命令不得接受额外参数");
    }

    private static void DlrcStateReportContainsRequiredFacts()
    {
        Type? formatterType = typeof(DlrcEvaluator).Assembly.GetType(
            "EmergencyEvents.RemoteAdminCommands.DlrcStateReportFormatter");
        AssertTrue(formatterType is not null, "必须提供 D-LRC 状态报告格式化器");

        System.Reflection.MethodInfo? format = formatterType!.GetMethod(
            "Format",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        AssertTrue(format is not null, "状态报告格式化器必须提供 Format");

        RoundSnapshot snapshot = CreateSnapshot(
            populationTier: PopulationTier.C,
            roundStartPopulation: 33,
            currentOnlinePlayers: 30,
            foundationCombatants: 11,
            chaosCombatants: 4,
            mainScpAlive: 2,
            startingScpCount: 3,
            scp0492Count: 5,
            scp079Present: true,
            scp079Tier: 4);
        DlrcEvaluationResult result = CreateResult(snapshot);
        CrisisAssessment assessment = new CrisisAssessment(
            7,
            DlrcEvaluationTrigger.MANUAL,
            snapshot,
            result,
            new[]
            {
                new CrisisDetectionResult(
                    CrisisTag.BIO,
                    true,
                    CrisisSeverity.Level4,
                    "Zombie pressure",
                    new Dictionary<string, double> { ["ZombieCount"] = 5d }),
                new CrisisDetectionResult(
                    CrisisTag.WAR,
                    true,
                    CrisisSeverity.Level4,
                    "Countdown active"),
            });
        string report = (string)format!.Invoke(null, new object?[] { snapshot, result, assessment })!;

        string[] requiredTokens =
        {
            "PopulationTier=C",
            "RoundStartPopulation=33",
            "CurrentOnlinePlayers=30",
            "ResponseCode=",
            "NaturalResponseScore=",
            "EffectiveResponseScore=",
            "ScpPresence=",
            "FoundationCombatShare=",
            "CrisisCode=",
            "BIO=Active:True",
            "WAR=Active:True;Severity=4",
        };
        foreach (string token in requiredTokens)
        {
            AssertTrue(report.Contains(token, StringComparison.Ordinal), $"状态报告缺少 {token}");
        }
    }

    private static void LowPopulationSuspensionIsRoundLocked()
    {
        PluginRuntimeCoordinator coordinator = new PluginRuntimeCoordinator(isEnabledForNextRound: true, minimumPlayers: 16);

        coordinator.BeginRound(15);
        AssertEqual(PluginRuntimeState.STANDBY, coordinator.State, "15 人开局必须保持 STANDBY");
        AssertTrue(!coordinator.IsEmergencyEventsActiveForRound, "15 人开局不得激活 EmergencyEvents");

        coordinator.BeginRound(16);
        AssertEqual(PluginRuntimeState.ACTIVE, coordinator.State, "16 人开局必须激活 EmergencyEvents");
        AssertTrue(coordinator.IsEmergencyEventsActiveForRound, "16 人开局必须标记为有效 EE 回合");

        coordinator.ObservePopulation(15);
        AssertEqual(PluginRuntimeState.LOW_POPULATION_SUSPENDED, coordinator.State, "有效 EE 回合掉至 15 人必须暂停");
        AssertTrue(!coordinator.IsEmergencyEventsActiveForRound, "低人口暂停后不得继续 EE 干预");

        coordinator.ObservePopulation(18);
        AssertEqual(PluginRuntimeState.LOW_POPULATION_SUSPENDED, coordinator.State, "本回合人口恢复后不得重新激活");

        coordinator.EndRound();
        coordinator.BeginRound(16);
        AssertEqual(PluginRuntimeState.ACTIVE, coordinator.State, "下一局必须重新按开局人数判定");
    }

    private static void EnableDisableDefersRoundActivation()
    {
        PluginRuntimeCoordinator coordinator = new PluginRuntimeCoordinator(isEnabledForNextRound: true, minimumPlayers: 16);
        coordinator.BeginRound(20);
        AssertEqual(PluginRuntimeState.ACTIVE, coordinator.State, "有效回合应先启动");

        coordinator.Disable();
        AssertEqual(PluginRuntimeState.DISABLED, coordinator.State, "disable 必须立即停止本局干预");
        AssertTrue(!coordinator.IsEnabledForNextRound, "disable 必须关闭下一局启用标记");

        bool activatedImmediately = coordinator.Enable(isRoundInProgress: true);
        AssertTrue(!activatedImmediately, "进行中的回合 enable 不得重新执行 Round Core");
        AssertEqual(PluginRuntimeState.DISABLED, coordinator.State, "进行中的回合 enable 必须保持本局 DISABLED");
        AssertTrue(coordinator.IsEnabledForNextRound, "enable 必须为下一局恢复启用标记");

        coordinator.EndRound();
        coordinator.BeginRound(20);
        AssertEqual(PluginRuntimeState.ACTIVE, coordinator.State, "下一局必须按新的启用标记激活");
    }

    private static void CommandGuardPreservesQueriesDuringSuspension()
    {
        AssertTrue(
            EmergencyEventsCommandGuard.IsAllowed(
                EmergencyEventsCommandKind.Status,
                PluginRuntimeState.LOW_POPULATION_SUSPENDED),
            "低人口暂停时 status 必须仍可查询");
        AssertTrue(
            EmergencyEventsCommandGuard.IsAllowed(
                EmergencyEventsCommandKind.Round,
                PluginRuntimeState.STANDBY),
            "STANDBY 时 round 必须仍可查询");
        AssertTrue(
            !EmergencyEventsCommandGuard.IsAllowed(
                EmergencyEventsCommandKind.DlrcEvaluate,
                PluginRuntimeState.LOW_POPULATION_SUSPENDED),
            "低人口暂停时不得运行真实 D-LRC 评估");
        AssertTrue(
            !EmergencyEventsCommandGuard.IsAllowed(
                EmergencyEventsCommandKind.CrisisCheck,
                PluginRuntimeState.DISABLED),
            "DISABLED 时不得运行真实危机检查");
        AssertTrue(
            EmergencyEventsCommandGuard.IsAllowed(
                EmergencyEventsCommandKind.TestCrisisEndSimulate,
                PluginRuntimeState.DISABLED),
            "纯 Dry Run 测试可在 DISABLED 时运行");
    }

    private static void EmergencyEventsCommandSyntaxRecognizesCommandTree()
    {
        (string[] Arguments, EmergencyEventsCommandKind Expected)[] supported =
        {
            (Array.Empty<string>(), EmergencyEventsCommandKind.Help),
            (new[] { "status" }, EmergencyEventsCommandKind.Status),
            (new[] { "modules" }, EmergencyEventsCommandKind.Modules),
            (new[] { "module", "dlrc" }, EmergencyEventsCommandKind.ModuleDetail),
            (new[] { "round" }, EmergencyEventsCommandKind.Round),
            (new[] { "wave", "history", "10" }, EmergencyEventsCommandKind.WaveHistory),
            (new[] { "dlrc", "breakdown" }, EmergencyEventsCommandKind.DlrcBreakdown),
            (new[] { "crisis", "check", "bio" }, EmergencyEventsCommandKind.CrisisCheck),
            (new[] { "test", "crisis", "end", "simulate", "720" }, EmergencyEventsCommandKind.TestCrisisEndSimulate),
            (new[] { "test", "crisis", "con", "checkpoint", "commit" }, EmergencyEventsCommandKind.TestCrisisConCheckpointCommit),
            (new[] { "cleanup" }, EmergencyEventsCommandKind.Cleanup),
        };

        foreach ((string[] arguments, EmergencyEventsCommandKind expected) in supported)
        {
            AssertTrue(
                EmergencyEventsCommandSyntax.TryParse(arguments, out EmergencyEventsCommandRequest request),
                $"命令 {string.Join(" ", arguments)} 必须被识别");
            AssertEqual(expected, request.Kind, $"命令 {string.Join(" ", arguments)} 的类型错误");
        }

        AssertTrue(
            !EmergencyEventsCommandSyntax.TryParse(new[] { "module", "disable", "dlrc" }, out _),
            "第一版不得接受单模块运行时禁用");
        AssertTrue(
            !EmergencyEventsCommandSyntax.TryParse(new[] { "crisis", "force", "bio" }, out _),
            "第一版不得接受直接伪造真实危机");
    }

    private static void EmergencyEventsCommandSyntaxRecognizesWaveIdAndStageModes()
    {
        AssertTrue(
            EmergencyEventsCommandSyntax.TryParse(
                new[] { "wave", "history", "1-MW-001", "detail" },
                out EmergencyEventsCommandRequest waveDetail),
            "WaveId 详情查询必须接受实际格式的字符串标识");
        AssertEqual(
            EmergencyEventsCommandKind.WaveHistoryDetail,
            waveDetail.Kind,
            "WaveId 详情查询必须归类为 WaveHistoryDetail");
        AssertEqual("1-mw-001", waveDetail.Target, "WaveId 必须被保留为请求目标");

        (string[] Arguments, string ExpectedKind)[] stageCommands =
        {
            (new[] { "dlrc", "stage" }, "DlrcStage"),
            (new[] { "dlrc", "stage", "full" }, "DlrcStageFull"),
            (new[] { "dlrc", "stage", "raw" }, "DlrcStageRaw"),
            (new[] { "help", "dlrc" }, "Help"),
        };

        foreach ((string[] arguments, string expectedKind) in stageCommands)
        {
            AssertTrue(
                EmergencyEventsCommandSyntax.TryParse(arguments, out EmergencyEventsCommandRequest request),
                $"命令 {string.Join(" ", arguments)} 必须被识别");
            AssertEqual(expectedKind, request.Kind.ToString(), $"命令 {string.Join(" ", arguments)} 的类型错误");
        }
    }

    private static void CrisisDiagnosticsAreReadOnly()
    {
        RoundSnapshot snapshot = CreateSnapshot(
            populationTier: PopulationTier.C,
            scp0492Count: 7);
        DlrcEvaluationResult result = CreateResult(snapshot);
        CrisisManager manager = new CrisisManager();

        AssertTrue(
            manager.TryDiagnose(CrisisTag.BIO, snapshot, result, out CrisisDetectionResult? detection),
            "BIO 手动诊断必须调用正式 Detector");
        AssertTrue(detection is not null, "BIO 手动诊断必须返回检测结果");
        AssertEqual(CrisisSeverity.Level4, detection!.Severity, "BIO 手动诊断必须保留正式阈值判定");
        AssertTrue(manager.CurrentCrisisAssessment is null, "手动诊断不得写入真实 CrisisAssessment");

        AssertTrue(
            manager.TryDiagnose(CrisisTag.WAR, snapshot, result, out CrisisDetectionResult? war),
            "WAR 必须复用正式 Detector 提供诊断");
        AssertEqual(CrisisSeverity.Inactive, war!.Severity, "Locked WAR 诊断必须 inactive");
    }

    private static void CrisisDiagnosticSnapshotFactoryPreservesSource()
    {
        RoundSnapshot source = CreateSnapshot(
            scp0492Count: 4,
            scp079Present: true,
            scp079Tier: 2,
            foundationCombatants: 8,
            chaosCombatants: 3,
            warheadUnlocked: false,
            warheadActive: false,
            warheadDetonated: false);

        RoundSnapshot zombieSimulation = CrisisDiagnosticSnapshotFactory.WithZombieCount(source, 10);
        RoundSnapshot sysSimulation = CrisisDiagnosticSnapshotFactory.WithScp079Tier(source, 5);
        RoundSnapshot securitySimulation = CrisisDiagnosticSnapshotFactory.WithSecurityFacts(source, 1, true);
        RoundSnapshot warSimulation = CrisisDiagnosticSnapshotFactory.WithWarheadState(source, "detonated");

        AssertEqual(4, source.Scp0492Count, "Dry Run 不得改写真实僵尸数量");
        AssertEqual(2, source.Scp079Tier, "Dry Run 不得改写真实 079 等级");
        AssertEqual(8, source.FoundationCombatants, "Dry Run 不得改写真实基金会人数");
        AssertTrue(!source.WarheadDetonated, "Dry Run 不得改写真实核弹状态");
        AssertEqual(10, zombieSimulation.Scp0492Count, "BIO Dry Run 必须仅覆盖僵尸数量");
        AssertEqual(5, sysSimulation.Scp079Tier, "SYS Dry Run 必须仅覆盖 079 等级");
        AssertEqual(1, securitySimulation.FoundationCombatants, "SEC Dry Run 必须覆盖基金会人数");
        AssertEqual(3, securitySimulation.ChaosCombatants, "SEC Dry Run 必须保留原有敌对人数");
        AssertTrue(warSimulation.WarheadDetonated, "WAR Dry Run 必须构造目标核弹事实");
    }

    private static void DlrcStageReportUsesChineseFields()
    {
        RoundSnapshot snapshot = CreateSnapshot(
            populationTier: PopulationTier.C,
            roundStartPopulation: 33,
            currentOnlinePlayers: 30,
            foundationCombatants: 11,
            chaosCombatants: 4,
            mainScpAlive: 2,
            startingScpCount: 3,
            scp0492Count: 5,
            scp079Present: true,
            scp079Tier: 4,
            warheadDetonated: false);
        DlrcEvaluationResult result = CreateResult(snapshot);
        CrisisAssessment assessment = new CrisisAssessment(
            11,
            DlrcEvaluationTrigger.MANUAL_RA,
            snapshot,
            result,
            new[]
            {
                new CrisisDetectionResult(CrisisTag.BIO, true, CrisisSeverity.Level4, "ZombieCount >= L4Threshold"),
            });

        string report = DlrcStageReportFormatter.FormatStandard(snapshot, result, assessment);
        string[] requiredTokens =
        {
            "【D-LRC 当前战局快照】",
            "人口编制：C",
            "基金会战斗人员：11",
            "核弹已爆炸：否",
            "生化危机（BIO）：4级",
        };
        foreach (string token in requiredTokens)
        {
            AssertTrue(report.Contains(token, StringComparison.Ordinal), $"标准战局报告缺少 {token}");
        }

        string[] forbiddenTokens = { "FoundationCombatants", "EligibleSpectators", "True", "False", "null" };
        foreach (string token in forbiddenTokens)
        {
            AssertTrue(!report.Contains(token, StringComparison.Ordinal), $"标准战局报告不得泄漏 {token}");
        }
    }

    private static void DlrcDisplayCodeRequiresSynchronizedAssessment()
    {
        RoundSnapshot snapshot = CreateSnapshot(populationTier: PopulationTier.B, scp0492Count: 4);
        DlrcEvaluationResult result = CreateResult(snapshot);
        CrisisAssessment matching = new CrisisAssessment(
            19,
            DlrcEvaluationTrigger.MANUAL_RA,
            snapshot,
            result,
            new[] { new CrisisDetectionResult(CrisisTag.BIO, true, CrisisSeverity.Level3, "Active") });
        CrisisAssessment stale = new CrisisAssessment(
            18,
            DlrcEvaluationTrigger.PERIODIC,
            snapshot,
            result,
            Array.Empty<CrisisDetectionResult>());

        AssertTrue(
            DlrcDisplayCodeFormatter.TryFormat(result, 19, matching, out string code, out _),
            "同一次评估的 CrisisAssessment 必须能生成完整代码");
        AssertEqual($"{result.Code}-BIO", code, "完整代码必须使用正式危机标签");
        AssertTrue(
            !DlrcDisplayCodeFormatter.TryFormat(result, 19, stale, out _, out string reason),
            "不同评估编号的 CrisisAssessment 不得被拼接到当前结果");
        AssertTrue(reason.Contains("不同步", StringComparison.Ordinal), "不同步结果必须明确提示，而非伪造无危机");
    }

    private static void ContainmentDiagnosticCommitIsExplicit()
    {
        DateTime secondWaveAt = new DateTime(2026, 8, 24, 17, 0, 0, DateTimeKind.Utc);
        MajorWaveSnapshot[] waves =
        {
            CreateWave(6, 6, false, 0d, secondWaveAt.AddMinutes(-5), scpCombatEquivalentAtCompletion: 13d / 3d),
            CreateWave(6, 6, false, 0d, secondWaveAt, scpCombatEquivalentAtCompletion: 13d / 3d),
        };
        RoundSnapshot baseline = CreateSnapshot(
            timestamp: secondWaveAt,
            mainScpAlive: 3,
            scp0492Count: 4,
            majorWaveHistory: waves);
        DlrcEvaluationResult baselineResult = CreateResult(baseline);
        CrisisManager manager = new CrisisManager();
        manager.Evaluate(new DlrcEvaluationCompletedEvent(1, DlrcEvaluationTrigger.PERIODIC, baseline, baselineResult));

        RoundSnapshot checkpoint = CreateSnapshot(
            timestamp: secondWaveAt.AddMinutes(1),
            mainScpAlive: 3,
            scp0492Count: 4,
            majorWaveHistory: waves);
        DlrcEvaluationResult checkpointResult = CreateResult(checkpoint);
        AssertTrue(
            manager.TryRunContainmentCheckpoint(checkpoint, checkpointResult, commit: false, out CrisisDetectionResult? dryRun),
            "CON Dry Run 必须在已有正式基线时执行");
        AssertEqual(CrisisSeverity.Level3, dryRun!.Severity, "CON Dry Run 必须按正式 Detector 预测失败结果");

        AssertTrue(
            manager.TryDiagnose(CrisisTag.CON, checkpoint, checkpointResult, out CrisisDetectionResult? afterDryRun),
            "CON 诊断必须可查询");
        AssertEqual(CrisisSeverity.Inactive, afterDryRun!.Severity, "CON Dry Run 不得推进真实 FailureStreak");

        AssertTrue(
            manager.TryRunContainmentCheckpoint(checkpoint, checkpointResult, commit: true, out CrisisDetectionResult? committed),
            "CON commit 必须在已有正式基线时执行");
        AssertEqual(CrisisSeverity.Level3, committed!.Severity, "CON commit 必须推进一次正式失败状态");
        AssertTrue(
            manager.TryDiagnose(CrisisTag.CON, checkpoint, checkpointResult, out CrisisDetectionResult? afterCommit),
            "CON commit 后仍必须可诊断");
        AssertEqual(CrisisSeverity.Level3, afterCommit!.Severity, "CON commit 后正式 FailureStreak 必须保留");
    }

    private static void EndDiagnosticSimulationUsesIsolatedState()
    {
        RoundSnapshot source = CreateSnapshot(
            timestamp: new DateTime(2026, 8, 24, 18, 0, 0, DateTimeKind.Utc),
            warheadDetonated: false,
            surfaceFoundationCombatants: 0,
            surfaceChaosCombatants: 0);
        DlrcEvaluationResult sourceResult = CreateResult(source);
        CrisisManager manager = new CrisisManager();
        RoundSnapshot simulated = CrisisDiagnosticSnapshotFactory.WithEndStalemate(source);
        DlrcEvaluationResult simulatedResult = CreateResult(simulated);

        AssertTrue(
            manager.TryDiagnoseEndSimulation(simulated, simulatedResult, 480, out CrisisDetectionResult? detection),
            "END 快速模拟必须使用正式 END Detector");
        AssertEqual(CrisisSeverity.Level4, detection!.Severity, "480 秒地表僵持必须为 END L4");
        AssertTrue(!source.WarheadDetonated, "END 模拟不得改写真实核弹事实");
        AssertEqual(0, source.SurfaceFoundationCombatants, "END 模拟不得改写真实地表人数");
    }

    private static void EmergencyEventsCommandSyntaxRecognizesRoundStateAlias()
    {
        AssertTrue(
            EmergencyEventsCommandSyntax.TryParse(new[] { "round", "state" }, out EmergencyEventsCommandRequest request),
            "round state 必须被识别为回合状态查询");
        AssertEqual(EmergencyEventsCommandKind.Round, request.Kind, "round state 必须与 round 使用同一查询处理器");
    }

    private static void FdiInitialSettlementUsesLookbackWindow()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 50d, InitialLookbackSeconds = 120 };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime at = Utc(6, 31);
        service.StartRound(Utc(4, 0), 16);
        service.Record(new DisorderEvent("old", at.AddSeconds(-121), DisorderEventCategory.CombatDeath, 20d));
        service.Record(new DisorderEvent("inside", at.AddSeconds(-120), DisorderEventCategory.CombatDeath, 3d));
        service.Record(new DisorderEvent("latest", at.AddSeconds(-1), DisorderEventCategory.MtfForceChanged, -2d));

        FacilityDisorderSettlement? settlement = SettleFdi(service, at);
        AssertTrue(settlement is not null, "首次 PERIODIC 必须初始化并结算");
        AssertNear(51d, service.State.CurrentFacilityDisorder, "首次 FDI 必须使用 InitialBase 加回看窗口事件");
        AssertEqual(at, service.State.LastProcessedAt, "首次结算必须记录实际周期时间");
        AssertEqual(2, settlement!.ProcessedEvents.Count, "窗口外事件不得进入首次结算");
    }

    private static void FdiIncrementalSettlementUsesLastProcessedBoundary()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime first = Utc(6, 31);
        DateTime second = first.AddSeconds(30);
        service.StartRound(first.AddMinutes(-10), 16);
        service.Record(new DisorderEvent("first", first, DisorderEventCategory.CombatDeath, 4d));
        SettleFdi(service, first);
        service.Record(new DisorderEvent("boundary", first, DisorderEventCategory.CombatDeath, 100d));
        service.Record(new DisorderEvent("second", second, DisorderEventCategory.CombatDeath, 5d));

        SettleFdi(service, second);
        AssertNear(59d, service.State.CurrentFacilityDisorder, "后续周期只能处理 LastProcessedAt 之后的新事件");
        AssertEqual(1, service.State.LastSettlement!.ProcessedEvents.Count, "后续窗口不得重复计算边界事件");
    }

    private static void FdiMtfChangesDoNotRepeat()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("wave-mtf", at, DisorderEventCategory.MtfForceChanged, -6d));
        service.Record(new DisorderEvent("wave-mtf", at, DisorderEventCategory.MtfForceChanged, -6d));
        SettleFdi(service, at);
        AssertNear(44d, service.State.CurrentFacilityDisorder, "MTF 增援应降低 FDI 且重复 source id 只能计一次");
        AssertEqual(1, service.State.LastSettlement!.ProcessedEvents.Count, "MTF 事件不得重复计入");
    }

    private static void FdiCombatDirectionIsPreserved()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("scp-kills-foundation", at, DisorderEventCategory.CombatDeath, 3d));
        service.Record(new DisorderEvent("foundation-kills-chaos", at, DisorderEventCategory.CombatDeath, -2d));
        service.Record(new DisorderEvent("scp-eliminated", at, DisorderEventCategory.ScpEliminated, -3d));
        SettleFdi(service, at);
        AssertNear(48d, service.State.CurrentFacilityDisorder, "战斗事件必须按方向累加正负变化");
    }

    private static void FdiCrisisTransitionsAreSettled()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("bio-l3", at, DisorderEventCategory.CrisisTransition, 3d));
        service.Record(new DisorderEvent("bio-l4", at, DisorderEventCategory.CrisisTransition, 4d));
        service.Record(new DisorderEvent("bio-resolved", at, DisorderEventCategory.CrisisTransition, -4d));
        SettleFdi(service, at);
        AssertNear(53d, service.State.CurrentFacilityDisorder, "危机状态转换应只通过事件影响 FDI");
    }

    private static void FdiSpecialEvaluationsAreReadOnly()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime periodic = Utc(6, 31);
        service.StartRound(periodic.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("periodic", periodic, DisorderEventCategory.CombatDeath, 2d));
        SettleFdi(service, periodic);
        DateTime before = service.State.LastProcessedAt!.Value;
        double score = service.State.CurrentFacilityDisorder;
        service.Record(new DisorderEvent("post", periodic.AddSeconds(10), DisorderEventCategory.CombatDeath, 20d));
        AssertTrue(service.ObserveEvaluation(periodic.AddSeconds(10), DlrcEvaluationTrigger.POST_MAJOR_WAVE), "POST 必须被识别为只读评估");
        AssertTrue(service.ObserveEvaluation(periodic.AddSeconds(20), DlrcEvaluationTrigger.MANUAL_RA), "MANUAL_RA 必须被识别为只读评估");
        AssertEqual(before, service.State.LastProcessedAt, "特殊评估不得推进 FDI 时间窗口");
        AssertNear(score, service.State.CurrentFacilityDisorder, "特殊评估不得改变 FDI 分数");
        SettleFdi(service, periodic.AddSeconds(30));
        AssertNear(72d, service.State.CurrentFacilityDisorder, "特殊评估观察到的事实应留给后续 PERIODIC 结算");
    }

    private static void FdiScoreClampsAndMapsBands()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("high", at, DisorderEventCategory.WarheadChanged, 1000d));
        SettleFdi(service, at);
        AssertNear(100d, service.State.CurrentFacilityDisorder, "FDI 必须封顶 100");
        AssertEqual(FacilityDisorderBand.HIGH, service.State.DisorderBand, "60-100 必须映射 HIGH");
        service.Record(new DisorderEvent("low", at.AddSeconds(30), DisorderEventCategory.WarheadChanged, -1000d));
        SettleFdi(service, at.AddSeconds(30));
        AssertNear(0d, service.State.CurrentFacilityDisorder, "FDI 必须封底 0");
        AssertEqual(FacilityDisorderBand.LOW, service.State.DisorderBand, "0-29 必须映射 LOW");
    }

    private static void FdiCleanupClearsRoundState()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("cleanup", at, DisorderEventCategory.CombatDeath, 2d));
        SettleFdi(service, at);
        service.CleanupRound();
        AssertTrue(!service.State.IsActive && !service.State.IsInitialized, "清理后 FDI 必须失活且未初始化");
        AssertEqual(0, service.EventCount, "清理后不得残留事件历史");
        AssertTrue(SettleFdi(service, at.AddMinutes(1)) is null, "清理后不得继续结算上一局");
    }

    private static void FdiLowPopulationSuspensionIsRoundLocked()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 15);
        AssertTrue(!service.State.IsActive, "不足 16 人开局时 FDI 必须完全不启动");
        service.StartRound(at.AddMinutes(-5), 16);
        service.ObservePopulation(15);
        AssertTrue(service.State.IsSuspended, "活动回合降到 15 人必须暂停 FDI");
        service.ObservePopulation(20);
        AssertTrue(service.State.IsSuspended && !service.State.IsActive, "本回合暂停必须不可逆");
    }

    private static void FdiConfigurationIsExplicit()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig();
        AssertTrue(config.IsProvisionalBalance, "默认 FDI 权重必须明确标记为临时平衡值");
        AssertEqual(50d, config.InitialBase, "默认 InitialBase 必须为 50");
        AssertEqual(120, config.InitialLookbackSeconds, "默认首次回看必须为 120 秒");
        AssertEqual(256, config.SettlementHistoryCapacity, "默认结算历史容量必须为最近 256 条");
        AssertEqual(512, config.EventHistoryCapacity, "默认事件历史容量必须为最近 512 条");
        AssertTrue(config.MtfLossPerCombatant > 0d && config.MtfGainPerCombatant < 0d, "MTF 增减方向必须可配置");
        AssertTrue(config.LowMaximum > config.LowMinimum && config.HighMinimum > config.MediumMaximum, "FDI 区间边界必须可配置");
    }

    private static void FdiCommandSyntaxRecognizesCommands()
    {
        (string[] Arguments, EmergencyEventsCommandKind Expected)[] commands =
        {
            (new[] { "disorder" }, EmergencyEventsCommandKind.DisorderState),
            (new[] { "fdi", "events" }, EmergencyEventsCommandKind.DisorderEvents),
            (new[] { "disorder", "history", "10" }, EmergencyEventsCommandKind.DisorderHistory),
            (new[] { "test", "disorder", "event", "mtf-loss", "3" }, EmergencyEventsCommandKind.TestDisorderEvent),
        };

        foreach ((string[] arguments, EmergencyEventsCommandKind expected) in commands)
        {
            AssertTrue(EmergencyEventsCommandSyntax.TryParse(arguments, out EmergencyEventsCommandRequest request), $"FDI RA 语法未识别：{string.Join(" ", arguments)}");
            AssertEqual(expected, request.Kind, $"FDI RA 命令类型错误：{string.Join(" ", arguments)}");
        }
    }

    private static void FdiInitialStockIncludesMtfWithoutDoubleCounting()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 50d };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 11);
        service.Record(new DisorderEvent("mtf-wave", at.AddMinutes(-151), DisorderEventCategory.MtfForceChanged, -6d, "04:00 MTF 0->6;仍存活", isRepresentedByCurrentStock: true));

        FacilityDisorderSettlement? settlement = SettleFdi(
            service,
            at,
            new FacilityDisorderStockSnapshot(mtfCount: 6, chaosCount: 0, zombieCount: 0, currentHostileForce: 0, scp079Present: false, scp079Tier: 0, crisisAssessment: null, warheadUnlocked: false, warheadActive: false, warheadDetonated: false),
            roundId: 11);

        AssertTrue(settlement is not null, "有效的首次 PERIODIC 必须结算");
        AssertNear(44d, service.State.CurrentFacilityDisorder, "MTF 存量应只通过 CurrentStockAdjustment 计算一次");
        AssertNear(-6d, settlement!.CurrentStockAdjustment, "MTF 当前存量调整错误");
        AssertNear(0d, settlement.RecentTransientDelta, "已由当前存量表达的 MTF 变化不得再次进入窗口 Delta");
    }

    private static void FdiInitial079AndSysDoNotDoubleCount()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 50d, CurrentScp079Tier = 2d, CurrentCrisisPerLevel = 1d };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 12);
        service.Record(new DisorderEvent("079-established", at.AddMinutes(-151), DisorderEventCategory.Scp079TierChanged, 2d, "04:00 Tier3;仍为Tier3", isRepresentedByCurrentStock: true));
        service.Record(new DisorderEvent("079-upgrade", at.AddMinutes(-61), DisorderEventCategory.Scp079TierChanged, 2d, "05:30 T2->T3;ExpressedBySYS", isRepresentedByCurrentStock: true));
        RoundSnapshot snapshot = CreateSnapshot(roundId: 12, timestamp: at, scp079Present: true, scp079Tier: 3);
        CrisisAssessment assessment = CreateCrisisAssessment(snapshot, (CrisisTag.SYS, CrisisSeverity.Level3));

        FacilityDisorderSettlement? settlement = SettleFdi(service, at, CreateStock(snapshot, assessment), 12, assessment);

        AssertTrue(settlement is not null, "SYS 存量测试必须结算");
        AssertNear(53d, service.State.CurrentFacilityDisorder, "SYS 已表达 079 Tier3 时不得再叠加 079 Tier Delta");
        AssertNear(3d, settlement!.CurrentStockAdjustment, "SYS L3 当前存量调整错误");
        AssertNear(0d, settlement.RecentTransientDelta, "已由 SYS 表达的 079 变化不得重复进入首次窗口");
    }

    private static void FdiInitialWarAndWarheadDoNotDoubleCount()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 50d, CurrentCrisisPerLevel = 1d };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 13);
        RoundSnapshot snapshot = CreateSnapshot(roundId: 13, timestamp: at, warheadUnlocked: true, warheadActive: true);
        CrisisAssessment assessment = CreateCrisisAssessment(snapshot, (CrisisTag.WAR, CrisisSeverity.Level4));
        FacilityDisorderSettlement? settlement = SettleFdi(service, at, CreateStock(snapshot, assessment), 13, assessment);

        AssertTrue(settlement is not null, "WAR 存量测试必须结算");
        AssertNear(54d, service.State.CurrentFacilityDisorder, "WAR 危机已表达核弹状态时不得重复计算 Unlock/Countdown");
        AssertNear(4d, settlement!.CurrentStockAdjustment, "WAR L4 当前存量调整错误");
    }

    private static void FdiInitialCrisisStockIsIncluded()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 50d, CurrentCrisisPerLevel = 1d };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 14);
        RoundSnapshot snapshot = CreateSnapshot(roundId: 14, timestamp: at);
        CrisisAssessment assessment = CreateCrisisAssessment(snapshot, (CrisisTag.BIO, CrisisSeverity.Level3));
        service.Record(new DisorderEvent("sys-active", at.AddMinutes(-151), DisorderEventCategory.CrisisTransition, 3d, "04:00 SYS active;仍为Active", isRepresentedByCurrentStock: true));
        FacilityDisorderSettlement? settlement = SettleFdi(service, at, CreateStock(snapshot, assessment), 14, assessment);

        AssertTrue(settlement is not null, "危机存量测试必须结算");
        AssertNear(53d, service.State.CurrentFacilityDisorder, "06:31 当前危机状态必须进入 InitialFDI");
    }

    private static void FdiPostInitializationUsesPureIncrement()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime first = Utc(6, 31);
        service.StartRound(first.AddMinutes(-5), 16, 20);
        SettleFdi(service, first, new FacilityDisorderStockSnapshot(6, 0, 0, 0, false, 0, null, false, false, false), roundId: 20);
        service.Record(new DisorderEvent("new-event", first.AddSeconds(30), DisorderEventCategory.CombatDeath, 5d));

        FacilityDisorderSettlement? second = SettleFdi(
            service,
            first.AddSeconds(30),
            new FacilityDisorderStockSnapshot(0, 0, 0, 0, false, 0, null, true, true, false),
            evaluationId: 21,
            roundId: 20);

        AssertTrue(second is not null, "首次结算后的 PERIODIC 必须继续结算");
        AssertNear(0d, second!.CurrentStockAdjustment, "后续纯增量结算不得重新应用 CurrentStockAdjustment");
        AssertNear(5d, second.RecentTransientDelta, "后续周期必须只处理新事件增量");
        AssertNear(49d, service.State.CurrentFacilityDisorder, "后续周期应保持首次存量结果并只加新事件");
    }

    private static void FdiInvalidCrisisAssessmentDoesNotAdvanceWindow()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 15);
        service.Record(new DisorderEvent("pending", at, DisorderEventCategory.CombatDeath, 5d));
        RoundSnapshot snapshot = CreateSnapshot(roundId: 15, timestamp: at);
        DlrcEvaluationCompletedEvent evaluation = CreateEvaluation(snapshot, 15);

        AssertTrue(service.SettlePeriodic(new FacilityDisorderEvaluationContext(evaluation, null), CreateStock(snapshot, null)) is null, "无效 CrisisAssessment 不得结算");
        AssertTrue(!service.State.LastProcessedAt.HasValue && !service.State.LastSettlementAt.HasValue, "无效 CrisisAssessment 不得推进窗口");
        AssertEqual(1, service.EventCount, "无效 CrisisAssessment 不得消费事件");
    }

    private static void FdiRoundIdMismatchDoesNotAdvanceWindow()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 16);
        RoundSnapshot snapshot = CreateSnapshot(roundId: 17, timestamp: at);
        DlrcEvaluationCompletedEvent evaluation = CreateEvaluation(snapshot, 17);
        CrisisAssessment assessment = CreateCrisisAssessment(snapshot);

        AssertTrue(service.SettlePeriodic(new FacilityDisorderEvaluationContext(evaluation, assessment), CreateStock(snapshot, assessment)) is null, "RoundId 不一致不得结算");
        AssertTrue(!service.State.LastProcessedAt.HasValue && !service.State.LastSettlementAt.HasValue, "RoundId 不一致不得推进窗口");
    }

    private static void FdiInvalidEvaluationDoesNotConsumeEvents()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime at = Utc(6, 31);
        service.StartRound(at.AddMinutes(-5), 16, 18);
        service.Record(new DisorderEvent("invalid-evaluation-event", at, DisorderEventCategory.CombatDeath, 7d));
        RoundSnapshot snapshot = CreateSnapshot(roundId: 18, timestamp: at);
        DlrcEvaluationResult invalidResult = CreateInvalidResult(CreateResult(snapshot));
        DlrcEvaluationCompletedEvent evaluation = new DlrcEvaluationCompletedEvent(18, DlrcEvaluationTrigger.PERIODIC, snapshot, invalidResult);
        CrisisAssessment assessment = new CrisisAssessment(18, DlrcEvaluationTrigger.PERIODIC, snapshot, invalidResult, Array.Empty<CrisisDetectionResult>());

        AssertTrue(service.SettlePeriodic(new FacilityDisorderEvaluationContext(evaluation, assessment), CreateStock(snapshot, assessment)) is null, "无效 Evaluation 不得结算");
        AssertTrue(!service.State.LastProcessedAt.HasValue && !service.State.LastSettlementAt.HasValue, "无效 Evaluation 不得推进时间状态");
        AssertEqual(1, service.EventCount, "无效 Evaluation 不得消费事件");
    }

    private static void FdiSuccessfulPeriodicProcessesFailedWindow()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime failedAt = Utc(6, 31);
        DateTime successAt = failedAt.AddMinutes(1);
        service.StartRound(failedAt.AddMinutes(-5), 16, 19);
        service.Record(new DisorderEvent("after-failure", failedAt.AddSeconds(10), DisorderEventCategory.CombatDeath, 6d));
        RoundSnapshot failedSnapshot = CreateSnapshot(roundId: 19, timestamp: failedAt);
        DlrcEvaluationResult invalidResult = CreateInvalidResult(CreateResult(failedSnapshot));
        DlrcEvaluationCompletedEvent failedEvaluation = new DlrcEvaluationCompletedEvent(19, DlrcEvaluationTrigger.PERIODIC, failedSnapshot, invalidResult);
        CrisisAssessment failedAssessment = new CrisisAssessment(19, DlrcEvaluationTrigger.PERIODIC, failedSnapshot, invalidResult, Array.Empty<CrisisDetectionResult>());
        AssertTrue(service.SettlePeriodic(new FacilityDisorderEvaluationContext(failedEvaluation, failedAssessment), CreateStock(failedSnapshot, failedAssessment)) is null, "失败周期不得结算");

        RoundSnapshot successSnapshot = CreateSnapshot(roundId: 19, timestamp: successAt);
        FacilityDisorderSettlement? settlement = SettleFdi(service, successAt, CreateStock(successSnapshot, null), 20, null, roundId: 19);
        AssertTrue(settlement is not null, "下一次成功 PERIODIC 必须补处理失败窗口");
        AssertNear(56d, service.State.CurrentFacilityDisorder, "失败周期留下的事件必须在下一次成功周期结算");
        AssertEqual(successAt, service.State.LastProcessedAt, "成功周期才可推进 LastProcessedAt");
    }

    private static void FdiFactionAdvantageDefaultDeltaIsZero()
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig();
        AssertNear(0d, config.FactionAdvantageChanged, "FactionAdvantageChanged 默认只记录事实，不应产生 Delta");
        DisorderEvent eventFact = new DisorderEvent("advantage", Utc(6, 31), DisorderEventCategory.FactionAdvantageChanged, config.FactionAdvantageChanged, "history-only");
        AssertNear(0d, eventFact.Delta, "FactionAdvantageChanged 事件默认 Delta 必须为零");
    }

    private static void FdiResourceBoundsPreservePendingEvents()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime first = Utc(6, 31);
        DateTime pendingAt = first.AddSeconds(1);
        service.StartRound(first.AddMinutes(-5), 16);
        service.Record(new DisorderEvent("pending", pendingAt, DisorderEventCategory.CombatDeath, 7d));

        SettleFdi(service, first);
        AssertEqual(1, service.EventCount, "尚未到结算窗口的事件不得被容量淘汰");

        SettleFdi(service, pendingAt.AddSeconds(1), evaluationId: 2);
        AssertNear(57d, service.State.CurrentFacilityDisorder, "保留的未结算事件必须在下一次窗口正常结算");
    }

    private static void FdiResourceBoundsRemainStableAfterThousandPeriodics()
    {
        FacilityDisorderService service = NewFdiService();
        DateTime first = Utc(6, 31);
        service.StartRound(first.AddMinutes(-5), 16);

        for (int index = 0; index < 1000; index++)
        {
            DateTime timestamp = first.AddSeconds(index);
            service.Record(new DisorderEvent($"bounded-{index}", timestamp, DisorderEventCategory.CombatDeath, 1d));
            SettleFdi(service, timestamp, evaluationId: index + 1);
        }

        AssertEqual(256, service.History.Count, "结算历史必须限制为最近 256 条");
        AssertTrue(service.EventCount <= 512, "已结算事件和 dedup 集合必须限制在最近 512 条以内");
        AssertNear(100d, service.State.CurrentFacilityDisorder, "1000 次 Periodic 后 FDI 结果必须保持封顶正确");
        AssertTrue(service.State.LastSettlement is not null, "历史淘汰后最近一次结算详情必须保留");

        DateTime duplicateAt = first.AddSeconds(1001);
        service.Record(new DisorderEvent("bounded-0", first, DisorderEventCategory.CombatDeath, 1000d));
        double beforeDuplicate = service.State.CurrentFacilityDisorder;
        FacilityDisorderSettlement? duplicateSettlement = SettleFdi(service, duplicateAt, evaluationId: 1001);
        AssertNear(beforeDuplicate, service.State.CurrentFacilityDisorder, "淘汰旧 dedup ID 后旧时间戳事件不得重新结算");
        AssertEqual(0, duplicateSettlement!.ProcessedEvents.Count, "旧重复事件不得进入新结算");
    }

    private static DlrcEvaluationCompletedEvent CreateEvaluation(RoundSnapshot snapshot, long evaluationId)
    {
        return new DlrcEvaluationCompletedEvent(evaluationId, DlrcEvaluationTrigger.PERIODIC, snapshot, CreateResult(snapshot));
    }

    private static CrisisAssessment CreateCrisisAssessment(
        RoundSnapshot snapshot,
        params (CrisisTag Tag, CrisisSeverity Severity)[] activeTags)
    {
        DlrcEvaluationResult result = CreateResult(snapshot);
        List<CrisisDetectionResult> detections = new List<CrisisDetectionResult>();
        foreach ((CrisisTag tag, CrisisSeverity severity) in activeTags)
        {
            detections.Add(new CrisisDetectionResult(tag, true, severity, "test"));
        }
        return new CrisisAssessment(snapshot.RoundId, DlrcEvaluationTrigger.PERIODIC, snapshot, result, detections);
    }

    private static FacilityDisorderStockSnapshot CreateStock(RoundSnapshot snapshot, CrisisAssessment? assessment)
    {
        return new FacilityDisorderStockSnapshot(
            snapshot.FoundationCombatants,
            snapshot.ChaosCombatants,
            snapshot.Scp0492Count,
            snapshot.MainScpAlive + snapshot.OtherHostileCombatants + snapshot.HostileThirdPartyCombatants,
            snapshot.Scp079Present,
            snapshot.Scp079Tier,
            assessment,
            snapshot.WarheadUnlocked,
            snapshot.WarheadActive,
            snapshot.WarheadDetonated);
    }

    private static FacilityDisorderSettlement? SettleFdi(
        FacilityDisorderService service,
        DateTime timestamp,
        FacilityDisorderStockSnapshot? stock = null,
        long evaluationId = 1,
        CrisisAssessment? assessment = null,
        DlrcEvaluationTrigger trigger = DlrcEvaluationTrigger.PERIODIC,
        long roundId = 0)
    {
        long resolvedRoundId = roundId == 0 ? evaluationId : roundId;
        RoundSnapshot snapshot = CreateSnapshot(roundId: resolvedRoundId, timestamp: timestamp);
        DlrcEvaluationCompletedEvent evaluation = new DlrcEvaluationCompletedEvent(evaluationId, trigger, snapshot, CreateResult(snapshot));
        CrisisAssessment resolvedAssessment = assessment ?? new CrisisAssessment(evaluationId, trigger, snapshot, evaluation.Result, Array.Empty<CrisisDetectionResult>());
        return service.SettlePeriodic(
            new FacilityDisorderEvaluationContext(evaluation, resolvedAssessment),
            stock ?? CreateStock(snapshot, resolvedAssessment));
    }

    private static FacilityDisorderService NewFdiService()
    {
        return new FacilityDisorderService(new FacilityDisorderConfig { InitialBase = 50d, InitialLookbackSeconds = 120 });
    }

    private static DateTime Utc(int minute, int second)
    {
        return new DateTime(2026, 8, 25, 6, minute, second, DateTimeKind.Utc);
    }

    private static CrisisDetectionResult Detect(ICrisisDetector detector, RoundSnapshot snapshot, CrisisState state)
    {
        return detector.Detect(snapshot, CreateResult(snapshot), state, new CrisisContext());
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

    private static void ScpRolePolicyUsesRandom939Candidate()
    {
        string[] pool =
        {
            "Scp049",
            "Scp079",
            "Scp106",
            "Scp3114",
            "Scp939",
        };
        const int totalRounds = 10000;
        int double939Count = 0;
        Random random = new Random(939);
        for (int round = 0; round < totalRounds; round++)
        {
            List<string> roles = ScpRolePolicy.BuildRoles(3, pool, random);
            AssertEqual(3, roles.Count, "每轮 SCP 角色数量必须等于请求数量");
            int scp939Count = 0;
            foreach (string role in roles)
            {
                AssertTrue(Array.IndexOf(pool, role) >= 0, "每个角色都必须来自合法 SCP 候选池");
                if (role == "Scp939")
                {
                    scp939Count++;
                }
            }

            if (scp939Count >= 2)
            {
                double939Count++;
            }
        }

        AssertTrue(double939Count > 0, "固定随机模拟必须出现双 939 回合");
        AssertTrue(double939Count < totalRounds, "固定随机模拟不得保证每轮都是双 939");
        Console.WriteLine($"[INFO][M01] Scp939Simulation TotalRounds={totalRounds}; Double939Count={double939Count}; TotalScpPerRound=3");
    }

    private static void PrimaryWaveCapsMatchSpecification()
    {
        PrimaryWaveCaps caps = new PrimaryWaveCaps();
        AssertEqual(6, caps.GetCap(PopulationTier.E), "E 档 Primary Wave 上限错误");
        AssertEqual(6, caps.GetCap(PopulationTier.D), "D 档 Primary Wave 上限错误");
        AssertEqual(8, caps.GetCap(PopulationTier.C), "C 档 Primary Wave 上限错误");
        AssertEqual(14, caps.GetCap(PopulationTier.B), "B 档 Primary Wave 上限错误");
        AssertEqual(18, caps.GetCap(PopulationTier.A), "A 档 Primary Wave 上限错误");
    }

    private static void PrimaryWaveCapsNeverExpandVanillaWave()
    {
        PrimaryWaveCaps caps = new PrimaryWaveCaps();
        AssertEqual(4, PrimaryWavePolicy.GetCappedMaximumRespawnAmount(4, PopulationTier.E, caps), "原版仅选四人时不得扩充到六人");
        AssertEqual(6, PrimaryWavePolicy.GetCappedMaximumRespawnAmount(12, PopulationTier.E, caps), "原版人数超过 E 档上限时应截断");
        AssertEqual(0, PrimaryWavePolicy.GetCappedMaximumRespawnAmount(0, PopulationTier.A, caps), "空原版波次不得被扩充");
    }

    private static void PrimaryWaveUsesLockedPopulationTier()
    {
        PrimaryWaveCaps caps = new PrimaryWaveCaps();
        int lockedTierCap = PrimaryWavePolicy.GetCappedMaximumRespawnAmount(18, PopulationTier.E, caps);
        AssertEqual(6, lockedTierCap, "开局 16 人锁定 E 档后，即使后来在线人数上升也必须继续使用 E 档上限");
    }

    private static void MiniWaveCancellationRespectsConfiguration()
    {
        AssertTrue(PrimaryWavePolicy.ShouldCancelMiniWave(true, true), "启用配置后必须取消 Mini-Wave");
        AssertTrue(!PrimaryWavePolicy.ShouldCancelMiniWave(false, true), "Primary Wave 不得被误取消");
        AssertTrue(!PrimaryWavePolicy.ShouldCancelMiniWave(true, false), "关闭配置后不得取消 Mini-Wave");
    }

    private static void MiniWaveCancellationUsesRespawningBoundary()
    {
        AssertTrue(
            !PrimaryWavePolicy.ShouldCancelMiniWaveAtBoundary(
                true,
                true,
                MiniWaveCancellationBoundary.SelectingRespawnTeam),
            "选择阶段不得取消 Mini-Wave，否则原版会重复进入选择事件");
        AssertTrue(
            PrimaryWavePolicy.ShouldCancelMiniWaveAtBoundary(
                true,
                true,
                MiniWaveCancellationBoundary.RespawningTeam),
            "真正刷新阶段必须取消 Mini-Wave");
        AssertTrue(
            !PrimaryWavePolicy.ShouldCancelMiniWaveAtBoundary(
                false,
                true,
                MiniWaveCancellationBoundary.RespawningTeam),
            "Primary Wave 在刷新阶段不得被当成 Mini-Wave 取消");
    }

    private static void PrimaryWaveCapPreservesVanillaSelection()
    {
        int[] vanillaSelection = { 10, 11, 101, 102, 103, 104, 105 };
        IReadOnlyList<int> capped = PrimaryWavePolicy.TruncateVanillaSelection(vanillaSelection, 6);
        AssertSequence(new[] { 10, 11, 101, 102, 103, 104 }, capped, "人数上限只能保留原版名单的前段，不得重新筛选或排除 Late Join");
        AssertEqual(7, vanillaSelection.Length, "原版选择名单不得被策略函数修改");
    }

    private static void MajorWaveHistoryRollsOverDeduplicatesAndCleansUp()
    {
        MajorWaveHistory history = new MajorWaveHistory();
        DateTime now = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        MajorWaveRecord first = history.Record("RW-1", "NtfWave", PopulationTier.E, 6, new[] { 1, 2, 3, 4, 5, 6 }, now, now.AddSeconds(8));
        MajorWaveRecord second = history.Record("RW-2", "ChaosWave", PopulationTier.E, 5, new[] { 7, 8, 9, 10, 11 }, now.AddMinutes(5), now.AddMinutes(5).AddSeconds(8));

        AssertEqual(second, history.CurrentWave, "最新波次应成为 CurrentWave");
        AssertEqual(second, history.LastMajorWave, "最新波次应成为 LastMajorWave");
        AssertEqual(first, history.PreviousMajorWave, "前一波应成为 PreviousMajorWave");
        AssertTrue(history.TryMarkPostMajorWavePublished(second), "首次 POST_MAJOR_WAVE 应发布");
        AssertTrue(!history.TryMarkPostMajorWavePublished(second), "同一波不得重复发布 POST_MAJOR_WAVE");
        AssertTrue(second.TryCompleteSurvivalObservation(3, now.AddMinutes(7)), "存活采样首次应完成");
        AssertTrue(!second.TryCompleteSurvivalObservation(2, now.AddMinutes(8)), "存活采样不得重复覆盖");
        AssertEqual(3, second.ToSnapshot().SurvivingCountAtEvaluation, "存活人数事实记录错误");
        AssertEqual(now.AddMinutes(5), second.ToSnapshot().StartedAt, "波次快照必须保留实际开始时间");

        history.Clear();
        AssertEqual(0, history.Count, "清理后历史必须为空");
        AssertEqual(null, history.CurrentWave, "清理后 CurrentWave 必须为空");
        AssertEqual(null, history.LastMajorWave, "清理后 LastMajorWave 必须为空");
        AssertEqual(null, history.PreviousMajorWave, "清理后 PreviousMajorWave 必须为空");
    }

    private static void PrimaryWaveTimerExtensionMapsSpawningAndOpposingTimers()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 6, true, 60, 15, false),
            "成功的 NTF Primary Wave 应应用 Timer Extension");
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("NtfWave", 60, 15, out int foundation, out int chaos),
            "NTF Primary Wave 应生成 60/15 增量");
        AssertEqual(60, foundation, "Foundation 刷新方增量错误");
        AssertEqual(15, chaos, "Chaos 对方增量错误");

        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("ChaosWave", 60, 15, out foundation, out chaos),
            "Chaos Primary Wave 应生成 15/60 增量");
        AssertEqual(15, foundation, "Foundation 对方增量错误");
        AssertEqual(60, chaos, "Chaos 刷新方增量错误");
    }

    private static void FoundationWaveUsesDynamicVanillaTimers()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("NtfWave", 60, 15, out int foundation, out int chaos),
            "Foundation Wave 应生成计时器增量");
        AssertNear(510d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(450d, foundation), "Foundation 450+60 错误");
        AssertNear(302d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(287d, chaos), "Chaos 287+15 错误");
        AssertNear(390d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(330d, foundation), "Foundation 330+60 错误");
        AssertNear(436d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(421d, chaos), "Chaos 421+15 错误");
    }

    private static void ChaosWaveUsesDynamicVanillaTimers()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("ChaosWave", 60, 15, out int foundation, out int chaos),
            "Chaos Wave 应生成计时器增量");
        AssertNear(315d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(300d, foundation), "Foundation 300+15 错误");
        AssertNear(510d, PrimaryWaveTimerExtensionPolicy.AddExtensionSeconds(450d, chaos), "Chaos 450+60 错误");
    }

    private static void VanillaResetRequiresFreshTimer()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.IsVanillaResetDetected(0d),
            "TimePassed=0 应视为原版 Reset 已完成");
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.IsVanillaResetDetected(0.49d),
            "刚完成 Reset 的小幅 TimePassed 应视为已完成");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.IsVanillaResetDetected(0.51d),
            "过大的 TimePassed 不应误判为刚完成 Reset");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.IsVanillaResetDetected(-1d),
            "负 TimePassed 不应视为有效 Reset");
    }

    private static void DisabledPrimaryWaveTimerExtensionDoesNotApply()
    {
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 6, true, 0, 0, false),
            "配置为 0 时不得修改计时器");
    }

    private static void MiniWaveDoesNotApplyTimerExtension()
    {
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfMiniWave", true, 6, true, 60, 15, false),
            "Mini-Wave 不得应用 Timer Extension");
        AssertTrue(!PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction("NtfMiniWave"), "Mini-Wave 不应被识别为 Primary 阵营");
    }

    private static void ZeroSpawnPrimaryWaveDoesNotApplyTimerExtension()
    {
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 0, true, 60, 15, false),
            "零人 Primary Wave 不得应用 Timer Extension");
    }

    private static void IncompletePrimaryWaveDoesNotApplyTimerExtension()
    {
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("ChaosWave", false, 4, false, 60, 15, false),
            "未完成或取消的 Primary Wave 不得应用 Timer Extension");
    }

    private static void DuplicateWaveDoesNotApplyTimerExtension()
    {
        MajorWaveHistory history = new MajorWaveHistory();
        DateTime now = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        MajorWaveRecord record = history.Record("TW-DUP", "ChaosWave", PopulationTier.E, 4, new[] { 1, 2, 3, 4 }, now, now.AddSeconds(1));
        MajorWaveRecord duplicateRecord = history.Record("TW-DUP", "ChaosWave", PopulationTier.E, 4, new[] { 1, 2, 3, 4 }, now, now.AddSeconds(1));

        AssertEqual(record, duplicateRecord, "相同 WaveId 应复用已有记录");
        AssertEqual(1, history.Count, "相同 WaveId 不得重复入库");
        AssertTrue(record.TryMarkTimerExtensionProcessed(), "首次处理 Timer Extension 应成功");
        AssertTrue(!record.TryMarkTimerExtensionProcessed(), "同一 WaveId 的 Timer Extension 处理标记不得重复成功");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("ChaosWave", false, record.ActualSpawnedCount, true, 60, 15, true),
            "同一 WaveId 已处理后不得再次应用 Timer Extension");
    }

    private static void NtfPrimaryWaveIsTimerExtensionTarget()
    {
        AssertTrue(PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction("NtfWave"), "NtfWave 应是 Primary Wave");
    }

    private static void ChaosPrimaryWaveIsTimerExtensionTarget()
    {
        AssertTrue(PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction("ChaosWave"), "ChaosWave 应是 Primary Wave");
    }

    private static void TimerExtensionDoesNotDuplicatePostMajorWave()
    {
        MajorWaveHistory history = new MajorWaveHistory();
        DateTime now = new DateTime(2026, 8, 24, 12, 0, 0, DateTimeKind.Utc);
        MajorWaveRecord record = history.Record("TW-1", "NtfWave", PopulationTier.E, 6, new[] { 1, 2, 3, 4, 5, 6 }, now, now.AddSeconds(1));

        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, record.ActualSpawnedCount, true, 60, 15, false),
            "正常波次应允许 Timer Extension");
        AssertTrue(history.TryMarkPostMajorWavePublished(record), "首次 POST_MAJOR_WAVE 应发布");
        AssertTrue(!history.TryMarkPostMajorWavePublished(record), "Timer Extension 不得产生第二次 POST_MAJOR_WAVE");
    }

    private static void InvalidTimerExtensionConfigurationFallsBackSafely()
    {
        AssertEqual(60, PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(-1, 60), "刷新方负数配置应回退到 60 秒");
        AssertEqual(15, PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(301, 15), "对方超限配置应回退到 15 秒");
        AssertEqual(0, PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(0, 60), "0 应保留为禁用配置");
        AssertEqual(60, PrimaryWaveTimerExtensionPolicy.DefaultSpawningFactionSeconds, "刷新方默认增量应为 60 秒");
        AssertEqual(15, PrimaryWaveTimerExtensionPolicy.DefaultOpposingFactionSeconds, "对方默认增量应为 15 秒");
        AssertEqual(300, PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(300, 60), "300 秒应是合法上界");
        AssertEqual(15, PrimaryWaveTimerExtensionPolicy.NormalizeConfiguredSeconds(15, 60), "合法 15 秒配置不应改变");
    }

    private static void TimerExtensionSidesCanBeDisabledIndependently()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 2, true, 0, 15, false),
            "刷新方禁用时，对方仍应可以单独应用");
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 2, true, 60, 0, false),
            "对方禁用时，刷新方仍应可以单独应用");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("NtfWave", false, 2, true, 0, 0, false),
            "两边都禁用时不得应用");
    }

    private static void TimerExtensionDoesNotAccumulateAcrossWaves()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("NtfWave", 60, 15, out int firstFoundation, out int firstChaos),
            "第一波应生成无状态增量");
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.TryGetExtensions("NtfWave", 60, 15, out int secondFoundation, out int secondChaos),
            "第二波应重新从原版值生成增量");
        AssertEqual(firstFoundation, secondFoundation, "刷新方增量不应跨波次累加");
        AssertEqual(firstChaos, secondChaos, "对方增量不应跨波次累加");
        double firstTimePassed = PrimaryWaveTimerExtensionPolicy.ApplyExtensionToTimePassed(0d, firstFoundation);
        double secondTimePassed = PrimaryWaveTimerExtensionPolicy.ApplyExtensionToTimePassed(0d, secondFoundation);
        AssertNear(firstTimePassed, secondTimePassed, "连续两波都必须从原版 reset 的 TimePassed=0 开始");
        AssertNear(390d, 330d - firstTimePassed, "第一波应只延长当前 timer 的剩余时间");
        AssertNear(390d, 330d - secondTimePassed, "第二波应重新得到相同的当前 timer 延长值");
    }

    private static void SpecialPersonnelEventDoesNotApplyTimerExtension()
    {
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.IsPrimaryFaction("Beta7"),
            "Beta-7 不应被识别为 Primary Wave");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.ShouldApply("Beta7", false, 4, true, 60, 15, false),
            "特殊人员事件不得应用 Timer Extension");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.TryGetExtensions("Nu7", 60, 15, out _, out _),
            "Nu-7 不应生成 Foundation/Chaos 计时器增量");
    }

    private static void BusyEvaluatorCoalescesPostMajorWaveQueue()
    {
        AssertTrue(
            PostMajorWaveQueuePolicy.ShouldQueue(0),
            "没有补算时应允许排队");
        AssertTrue(
            !PostMajorWaveQueuePolicy.ShouldQueue(1),
            "已有补算时不得继续排队");
        AssertTrue(
            !PostMajorWaveQueuePolicy.ShouldQueue(9),
            "多个事件也只能保留一个补算");
    }

    private static void ActualSpawnedPlayerRequiresSuccessfulNativeRole()
    {
        AssertTrue(
            PrimaryWaveTimerExtensionPolicy.IsActualSpawnedPlayer(true, true, true),
            "连接、存活且阵营匹配的玩家才算实际出生");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.IsActualSpawnedPlayer(true, false, true),
            "角色设置失败而未存活的候选者不得计入实际出生");
        AssertTrue(
            !PrimaryWaveTimerExtensionPolicy.IsActualSpawnedPlayer(true, true, false),
            "阵营不匹配的候选者不得计入实际出生");
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

    private static DlrcEvaluationResult CreateCrisisResult(
        RoundSnapshot snapshot,
        int finalLevel,
        FoundationStrength foundationStrength)
    {
        DlrcEvaluationResult source = CreateResult(snapshot);
        ControlAssessment control = new ControlAssessment(
            ThreatTrend.STABLE,
            0d,
            null,
            foundationStrength,
            0d,
            WavePerformance.NEUTRAL,
            BattlefieldMomentum.NEUTRAL,
            0,
            0,
            false,
            false,
            false,
            ControlState.CONTROLLED,
            finalLevel);
        return new DlrcEvaluationResult(
            source.RoundId,
            source.Timestamp,
            source.PopulationTier,
            source.NaturalResponseScore,
            source.PersistentAdjustment,
            source.EffectiveResponseScore,
            source.ResponseBreakdown,
            finalLevel,
            control,
            ControlState.CONTROLLED,
            finalLevel,
            isValid: true,
            $"DLRC-{source.PopulationTier}{finalLevel}");
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
        int recentMainScpDeaths120s = 0,
        bool hostileThirdPartyActive = false,
        int hostileThirdPartyCombatants = 0,
        int surfaceFoundationCombatants = 0,
        int surfaceChaosCombatants = 0,
        int surfaceMainScp = 0,
        int surfaceOtherHostiles = 0,
        DateTime? warheadDetonatedAt = null)
    {
        DateTime resolvedTimestamp = timestamp ?? new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        return new RoundSnapshot(
            roundId: roundId,
            timestamp: resolvedTimestamp,
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
            recentMainScpDeaths120s: recentMainScpDeaths120s,
            hostileThirdPartyActive: hostileThirdPartyActive,
            hostileThirdPartyCombatants: hostileThirdPartyCombatants,
            surfaceFoundationCombatants: surfaceFoundationCombatants,
            surfaceChaosCombatants: surfaceChaosCombatants,
            surfaceMainScp: surfaceMainScp,
            surfaceOtherHostiles: surfaceOtherHostiles,
            warheadDetonatedAt: warheadDetonatedAt ?? (warheadDetonated ? resolvedTimestamp : null));
    }

    private static MajorWaveSnapshot CreateWave(
        int startingCount,
        int survivingCount,
        bool isEvaluationComplete,
        double baseFailureScore,
        DateTime? startedAt = null,
        DateTime? evaluatedAt = null,
        bool? isCatastrophic = null,
        DateTime? completedAt = null,
        double? scpCombatEquivalentAtCompletion = null)
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
            evaluatedAt: evaluatedAt,
            completedAt: completedAt,
            scpCombatEquivalentAtCompletion: scpCombatEquivalentAtCompletion ?? startingCount);
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
