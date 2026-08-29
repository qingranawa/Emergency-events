# API Reference

以下名称和字段以当前源码为准。文档不是对源码的复制，具体行为以接口和测试为准。

## 三个事件资格维度

### Population Tier

`EmergencyEvents.RoundCore.PopulationTier` 的值为 `E`、`D`、`C`、`B`、`A`。它在回合开始时锁定，决定读取哪一份人口计划，不会因为后续掉人自动换档。

### EventResponseLevel

`Director.EventResponseLevel` 为 `L0` 至 `L5`。`EventDefinition.RequiredResponseLevel` 只有一个值，当前语义是最低要求：当前最终等级低于要求时拒绝，达到或超过要求后继续其他资格判断。

### CrisisTag

`Crisis.CrisisTag` 当前值为 `BIO`、`SYS`、`CON`、`SEC`、`GOI`、`WAR`、`END`。标签只描述目标危机，不附带独立等级。

## Crisis API

### `CrisisDetectionResult`

构造参数为 `Tag`、`IsActive`、`Reason` 和 `Metrics`。Detector 输出事实判断，不生成危机等级。

### `CrisisAssessment`

重要属性：

- `EvaluationId`、`Trigger`、`Snapshot`、`Result`：绑定同一次评估。
- `Detections`：按 CrisisTag 保存检测结果。
- `ActiveTags`：本次活动标签。
- `ActivatedTags`：相对上一份 Assessment 新进入 Active 的标签。
- `ResolvedTags`：相对上一份 Assessment 从 Active 变为 Inactive 的标签。
- `EpisodeIds`：当前 Active 标签对应的 Episode ID。
- `Code`：D-LRC 代码加当前 Active 标签展示，不表示危机等级。
- `IsActive(tag)`、`TryGetEpisodeId(tag, out id)`：下游资格判断入口。

状态序列 `Inactive -> Active -> Active -> Inactive -> Active` 会产生两个 Episode；持续 Active 不重复 Activated，重新 Active 会获得新 Episode。

### `CrisisManager`

`Evaluate(DlrcEvaluationCompletedEvent)` 只接受有效且 RoundId 一致的结果，并去重 processed evaluation ID。`TryDiagnose`、`TryRunContainmentCheckpoint` 和 `TryDiagnoseEndSimulation` 是诊断/模拟入口，默认不写入真实评估状态。

processed evaluation ID 的回合内去重容量为 512；回合清理时全部清空。

当前 Detector 语义：

| Tag | Active 条件摘要 | 事实来源 |
| --- | --- | --- |
| BIO | `Scp0492Count >=` 当前人口档位的 ActivationThreshold。默认 E/D=3、C/B=4、A=5。 | `RoundSnapshot.Scp0492Count` |
| SYS | SCP-079 存在、Tier 有效且 `Tier >= 3`。 | 079 事实 |
| CON | 第二个已完成的 Foundation/MTF 大波存在，且 containment checkpoint 出现失败 streak。 | `MajorWaveHistory` 与 `CrisisState` |
| SEC | 有敌对威胁且 Foundation 人数不超过当前人口档位的 Security ActivationThreshold。 | 人员与威胁快照 |
| GOI | 已注册敌对第三方有战斗人员、FinalLevel 至少为 3 且 Foundation 为 WEAK/CRITICAL。 | GOI 输入目前 PROVISIONAL |
| WAR | 核弹已解锁且尚未爆炸；Countdown Active 只是事实 reason，不是第二个等级。 | Warhead facts |
| END | 核弹已爆炸，且地表存在持续敌对僵持达到 `EndActivationSeconds`（默认 300 秒）。 | `WarheadDetonatedAt` 与地表事实 |

### 已删除的旧模型

Runtime 中的 `CrisisSeverity`、`RequiredCrisisSeverity` 和 `RespondedSeverities` 已删除，当前 Runtime old severity references 为 0。历史迁移文档若出现这些名称，必须标记为 `OBSOLETE` 或 `REMOVED`。

## M03 API

### `RoundSnapshot`

快照包含 `RoundId`、`Timestamp`、`RoundElapsedTime`、`PopulationTier`、`RoundStartPopulation`、人员计数、SCP 状态、079、核弹、波次历史、120 秒死亡窗口、ActivePlayerIds、地表事实和 `WarheadDetonatedAt`。构造时会规范化负值、非法档位、重复玩家 ID 和非法数值。

### `MajorWaveSnapshot`

重要字段为 `Name`、`Faction`、`StartingCount`、`SurvivingCountAtEvaluation`、`IsEvaluationComplete`、`BaseFailureScore`、`IsCatastrophic`、`StartedAt`、`CompletedAt`、`EvaluatedAt`、`MemberIds` 和 `ScpCombatEquivalentAtCompletion`。

### `DlrcEvaluationResult`

重要属性为 `RoundId`、`Timestamp`、`PopulationTier`、`NaturalResponseScore`、`PersistentAdjustment`、`EffectiveResponseScore`、`ResponseBreakdown`、`TheoreticalLevel`、`ControlAssessment`、`ControlState`、`FinalLevel`、`IsValid` 和 `Code`。

## M02 API

### `MajorWaveHistory`

公开属性为 `Capacity`、`Count`、`CurrentWave`、`LastMajorWave`、`PreviousMajorWave` 和只读 `Records`。`Record(...)` 按 WaveId 去重，更新当前/上一次/上上次记录并按容量淘汰最旧记录；默认容量为 256，最小容量为 2。`Clear()` 在回合清理时清空全部映射。

### `MajorWaveCompletedEvent`

包含 `RoundId`、`WaveId`、`Faction`、`PopulationTier`、`ActualSpawnedCount` 和 `CompletedAt`，供 M03/M04/FDI 消费。

Primary cap 是上限，不是目标：E=6、D=6、C=8、B=14、A=18。实际人数不得超过 Vanilla planned、Vanilla eligible 和档位 cap 中的最小值。

## M05 API

### `EventDefinition`

当前真实字段为：

| 字段 | 用途 |
| --- | --- |
| `EventId` / `DisplayName` | 稳定标识和展示名。 |
| `Category` | `Support` 或 `NonSupport`。 |
| `Source` | `Foundation`、`Chaos`、`Goi`、`ProfessionalCrisisResponse` 或 `Internal`。 |
| `RequiredResponseLevel` | 唯一的 L0–L5 要求。 |
| `RequiredCrisisTags` | 专业事件要求的 Active 标签集合，当前为 AND。 |
| `TargetPersonnel` / `MinimumPersonnel` | 五个档位的人数计划。 |
| `PopulationProfiles` | 每个档位解析出的 `EventPopulationProfile`。 |
| `IsEnabled` / `Priority` / `Weight` | 选择与启用策略。非法权重会归零。 |
| `RequiresUndergroundFacility` | DESTROYED 时过滤地下事件。 |
| `IsProfessionalResponse` | `Source == ProfessionalCrisisResponse`。 |

当前没有把 Intensity、ExclusiveGroup、CanOverlap 或 Alignment 当作已实现的正式字段；相关方向见 Roadmap。

### `EventPopulationProfile` 与 `ResolvedEventPopulation`

`EventPopulationProfile` 包含 `TargetPersonnel`、`MinimumPersonnel`、`AllowDownscale`、`CompositionProfileId` 和 `LoadoutProfileId`。`ResolvedEventPopulation` 另外输出 `Tier`、`Available`、`Planned`、`IsViable` 和 `RejectReason`。当前 `EventDefinition` 构造器从 `TierPersonnelPlan` 自动生成五份 Profile，并使用默认 `AllowDownscale=true`、空的 Composition/Loadout ID；自定义执行 Profile 仍是扩展接口，而不是已接线的生产 Event Pack。

`IEventPopulationResolver.Resolve(EventDefinition, PopulationTier, availablePersonnel)` 是统一人口计划入口。典型行为：Target=6、Minimum=4、允许缩减时，Available=8/5/4 得到 Planned=6/5/4，Available=3 拒绝；不允许缩减时，Available 小于 Target 直接拒绝。

完整边界是：Available 大于或等于 Target 时按 Target 计划；Available 在 Minimum 与 Target 之间（含 Minimum）且允许缩减时按 Available 计划；Available 小于 Minimum 时拒绝；不允许缩减时只要 Available 小于 Target 就以 `TargetPersonnelUnavailable` 拒绝。Available 会先规范化为不小于 0 的整数。

### `DirectorContext`

它拼接 M01–M04.5 的官方事实，包括 RoundId、时间、PopulationTier、M03 结果、CrisisAssessment、FDI 状态、Current/Last/Previous Major Wave、人员事实、FacilityState、`HasO4Selector` 和 FDI band。Director 只能消费它，不能在其中重算上游结果。

### `EventDirector`

重要成员为 `SelectCycle`、`TryStart`、`RevalidateBeforeCommit`、`Commit`、`TryBeginSecondSlot` 和 `CleanupRound`，以及 `IsBusy`、`CurrentCycle`、`Tracker`、`Scheduler` 和有界 `Logs`。

`Commit` 只有在调用方提供 `latestContext` 时才执行 Commit 前重验证；不传该参数时只校验当前周期、候选和槽位。需要最新事实保护的生产适配器必须显式传入最新上下文。

### `ProfessionalResponseTracker`

按 CrisisTag 维护当前 Episode 和已成功消费的 `EventResponseLevel` 集合。同一 Episode 的同一 Response Level 只能成功消费一次；只有 Commit 成功后才调用 `Consume`。危机解除后，新 Episode 重新获得资格。

### 来源仲裁

`SupportSourceArbitrator` 只接收合法普通 SUPPORT 候选；专业响应在 `EventSelectionService` 中优先处理。普通来源权重经过有限正数检查，Random 可通过 `IRandomSource` 注入，`SeededRandomSource` 可复现；容器遍历顺序不会决定随机结果。

## M06 API

### `O4PanelConfig`

配置 M06 启用状态、普通 Hint、选择功能、刷新/Hint 时长、投票时长、显示字段与最近选择结果容量。`RefreshIntervalSeconds` 限制为 0.5–5，`HintDurationSeconds` 限制为 0.5–5，`VoteDurationSeconds` 限制为 5–120，`MaxCandidates` 限制为 1–2，`HistoryCapacity` 最大 256。EXILED 9.14.2 没有可验证的 Hint anchor API，因此不存在伪造的位置字段。

### `IO4EventSelector` 与 `O4SelectionRequest`

M05 唯一通过 `IO4EventSelector` 请求、取消 O4 会话。`O4SelectionRequest` 绑定 RoundId、CycleId、SessionId、由 M05 提供的 candidate views 和 M05 fallback ID；M06 不接受候选之外的 event ID。

### `O4SelectionResult`

结果包含同一组 RoundId/CycleId/SessionId、Outcome、SelectedEventId、Reason 和匿名投票统计。`EXPLICIT_WINNER` 只有在多数有效票选择当前 shortlist 内候选时出现；`FALLBACK` 保留 M05 原候选；`CANCELLED` 由生命周期清理产生。M05 必须通过 `MatchesBinding(...)` 拒绝 stale 结果。

### `O4PanelRuntimeService`

运行时服务使用 `Player.ShowHint(string, float)` 向当前在线 Spectator/Overwatch 写 Hint，使用 MEC 管理刷新和投票超时。客户端命令为 `o4vote <1|2>`（别名 `eevote`），由 `ClientCommandHandler` 接收；RA 不提供投票命令。`ee o4 status` 是只读管理诊断。

## Runtime Provider

`IFacilityStateProvider.GetState(RoundSnapshot)` 是 Facility 状态入口。当前 `SnapshotFacilityStateProvider` 可靠使用 `WarheadDetonated` 映射 `FacilityState.Destroyed`，其余状态仍为 `PROVISIONAL`。`HasO4Selector=false` 在 M06 被禁用、无合法 O4、低人口暂停或运行时不可用时是合法状态，M05 必须使用 fallback。

`IRecentEventHistory` 只定义由上游提供的只读最近事件列表，不在 M05 内部维护事件历史。`EventDirector.Logs` 才是 Director 自己拥有的诊断历史，容量由 `EventDirectorConfig.MaxLogEntries` 控制，默认 256 条。
