# Runtime Contracts

本文档记录模块在真实回合中的状态边界、失败行为和可观测事实。除非明确标为 PROVISIONAL 或 PENDING DESIGN，否则这些规则是当前实现契约。

## Plugin runtime state

`PluginRuntimeState` 当前包含：`DISABLED`、`STANDBY`、`ACTIVE`、`LOW_POPULATION_SUSPENDED`、`ROUND_ENDED` 和 `ERROR`。

`PluginRuntimeCoordinator` 在回合开始时记录 `RoundStartPopulation` 和 `PopulationTier` 所需事实。少于 `MinimumPlayers`（配置默认 16）时不激活；活动回合降到最低人数以下时进入不可逆的 `LOW_POPULATION_SUSPENDED`。恢复到 16 人不会重新启用本回合模块，必须等待下一局。

回合结束或进入 WaitingForPlayers 时，Round Core、Reinforcement、Crisis、FDI、Director 和 O4 Panel 都应清理自己的回合状态。管理员执行 `ee disable` 时，会立即清理当前回合的 EE 状态并停止后续 EE 事件处理；插件仍保持加载，但本回合后续按原版处理，下一回合也保持禁用。已经实际应用到当前玩家的开局角色、物品或传送不会在回合中被强制反向改写。

## M02 wave contract

原版 Primary Respawn Wave 是唯一的原版波次来源。Emergency Events 保留原版：

- MTF/CI 阵营决定。
- Influence 和 Respawn Token。
- 原版计时器和玩家选择。
- 职业组成、装备、出生流程。

插件只在已发布的刷新边界执行以下行为：禁用 Mini-Wave、对原版 Primary Wave 结果做 E/D/C/B/A cap、记录实际出生成员和波次事实、应用一次性的 Timer Extension，并发布一次 `MajorWaveCompletedEvent`/`POST_MAJOR_WAVE` 链路。

Cap 为截断上限而不是目标：E=6、D=6、C=8、B=14、A=18。插件不能把原版计划人数扩充到 cap。

Timer Extension 只在该次原版 timer reset/recalculation 完成后施加到当前状态，不把增量永久写入下一波的基础间隔；Mini-Wave、零人波次和未完整结束的波次不应用该扩展。

## M03 evaluation contract

每次评估都必须绑定一个 `RoundSnapshot` 和同一 `RoundId` 的 `DlrcEvaluationResult`。结果公开 `IsValid`，无效结果不能被下游当作正式事实消费。

`DlrcEvaluationResult` 的 `NaturalResponseScore`、`PersistentAdjustment` 和 `EffectiveResponseScore` 分开保存。最终的 `TheoreticalLevel`、`ControlState`、`ControlLevelCap` 和 `FinalLevel` 不应被 Crisis Detector 或 Event Pack 重算。

Evaluation History 是 ring buffer，当前配置默认容量为 20；这是历史查询容量，不是业务状态无限存储。

## M04 crisis contract

每次合法评估由 `CrisisManager` 调用七个 Detector，得到 `CrisisAssessment`。Assessment 与 EvaluationId、Trigger、Snapshot 和 Result 绑定，并公开 ActiveTags、ActivatedTags、ResolvedTags 和 EpisodeIds。

状态变化契约：

```text
Inactive -> Active
  ActivatedTags 加入该 tag，创建新的 EpisodeId

Active -> Active
  保持同一 Episode，不重复 Activated

Active -> Inactive
  ResolvedTags 加入该 tag，结束当前 Episode

Inactive -> Active（再次发生）
  创建新的 EpisodeId
```

Crisis 没有独立 Severity。`BIO_L4_RESPONSE` 这类未来事件命名中的 L4 如果存在，只能表示 `EventResponseLevel.L4`，不能表示 BIO 的危机等级。

WAR 的 Active 条件是核弹已解锁且未爆炸；爆炸后 WAR inactive。END 需要可靠的 `WarheadDetonatedAt`、核弹已爆炸和持续地表敌对僵持，当前默认激活窗口为 300 秒。

CON 只接受 Foundation/MTF Primary Wave 作为 containment checkpoint。Chaos Wave 可以出现在 M02 历史中，但不能建立 CON baseline、推进 Foundation response count 或成为 Foundation containment failure。

当前未实现 Crisis Episode Resolve Debounce，状态为 `PENDING DESIGN`。

## M04.5 FDI contract

`FacilityDisorderService` 维护 0–100 的 `CurrentFacilityDisorder`、`DisorderBand`、`LastProcessedAt`、`LastSettlementAt` 和 `LastSettlement`。FDI 不是 D-LRC，只作为设施秩序事实和普通 SUPPORT 来源仲裁的临时输入。

### 首次结算

06:31 初始化由：

```text
InitialBase
+ CurrentStockAdjustment
+ Recent120sTransientDelta
```

Current Stock 已经表达的力量、079/SYS、危机或 Warhead 状态，不得再次通过对应的近期 State/Force transition 重复计分。近期 Combat Transient 仍应保留。

### 后续结算

首次结算后只执行：

```text
PreviousFDI + NewEventDelta
```

正式 PERIODIC 才能推进结算窗口。POST_MAJOR_WAVE 与 MANUAL_RA 当前只读或触发上游观察，不应偷偷改变 FDI 的正式结算时序。

### FDI crisis transitions

- Inactive -> Active：记录一次 Crisis Activated disorder event。
- Active -> Active：不重复产生等价 Delta。
- Active -> Inactive：记录一次 Crisis Resolved disorder event。
- 079/SYS 和 Warhead/WAR/END 的重叠事实必须遵守去重策略。

若上游 Evaluation、Snapshot、RoundId 或 CrisisAssessment 无效，FDI 保持原值，不消费 DisorderEvent，不推进 `LastProcessedAt` 或 `LastSettlementAt`；后续成功 PERIODIC 继续处理未消费窗口。

FDI 已实现 State-Gated + Band-Aware Order Recovery。只有正常 PERIODIC、上游有效、没有普通事件 Delta、没有 Active Crisis、没有明显敌对压力且设施未 Destroyed 时，才在静默窗口结束后产生负 Recovery Delta；单个普通 Chaos 不会自动永久阻断 Recovery，明确敌对占优才会阻断。默认静默窗口为 90 秒，HIGH/MEDIUM/LOW 分别为 -2/-1/0；每次恢复或新的普通事件都会重新开始窗口。Recovery 不在 POST_MAJOR_WAVE 或 MANUAL 查询中运行，并以 `ORDER_RECOVERY_CHECK` 记录 Gate、窗口、普通 Delta、前后 FDI 和结果。

### FDI capacities

当前实现有界保存：settlement history 默认 256、event history 默认 512、recorded event IDs 2048、FDI evaluation IDs 512、Crisis processed evaluation IDs 512、Director logs 默认 256。`IRecentEventHistory` 只是上游输入接口，不拥有内部存储。回合清理时全部清空；未结算事件不会因为淘汰历史而丢失。

## M05 Director contract

### Context boundary

`DirectorContext` 只拼接 M01–M04.5 已发布事实。M05 可以读取 D-LRC、CrisisAssessment、FDI、MajorWaveRecord、人员、FacilityState 和 `HasO4Selector`，不能重算这些状态。

### Eligibility

候选资格顺序包括：事件启用、Evaluation 存在且有效、RoundId 一致、Response Level、RequiredCrisisTags、Episode 消费状态、人口计划、FacilityState 和可用人员。

`RequiredCrisisTags` 是 AND：`[BIO,SYS]` 要求 BIO 与 SYS 同时 Active。不存在 `RequiredCrisisSeverity`。

专业响应优先于普通 SUPPORT 来源。FDI 只影响普通 Foundation/Chaos/GOI 来源仲裁；NON_SUPPORT 不读取 FDI。

### Revalidation and commit

Candidate 和 Selected 只是计划。`TryStart` 和 `RevalidateBeforeCommit` 会重新验证；`Commit` 只有传入 `latestContext` 时才触发同一重验证，调用方不能省略该最新上下文保护：

- Eligible Personnel。
- Crisis Active 状态和 Current Episode。
- D-LRC Response Level。
- Professional eligibility。
- Population Plan。
- RoundId 和 Evaluation 有效性。

例如 Candidate 时有 6 名可用人员，Start 前只剩 2 名而 Minimum 为 3，周期必须 Abort/Rollback，不扣 Event Cost、不消费 Professional Response、不创建 Event #2。

### ProfessionalResponseTracker

同一 Crisis Episode 的每个 D-LRC Response Level 只能成功消费一次。Candidate、Selected、Prepared 和 Start 失败都不消费；只有成本边界成功且 Commit 完成后才调用 `Consume`。Professional Response 的 Commit 必须提供最新 Context；成本边界失败不得消费 Response。Rollback 或 Completed 后 `IsBusy=false`、`CurrentCycle=null`，新周期可以继续。

### Event #2

第二槽位是 `NON_SUPPORT`。只有第一槽位成功启动并记录 `ActualFirstSlotStartedAt` 后，才按：

```text
DueAt = Event1ActualSpawnTime + SecondSlotDelaySeconds
```

默认延迟为 60 秒。它由状态轮询和 DueAt 判断，不依赖 `Task.Delay`；第一槽位失败时当前策略为 `Skip`。回合结束、重启、低人口暂停、禁用或 Runtime Cleanup 必须清理 DueAt 和 Scheduler 状态。

### O4 boundary

Foundation 有多个合法普通 SUPPORT 候选时标记 `O4SelectionRequired`，不因当前 O4 是否在线而改写这个事实。O4 不选择来源，不召唤事件，也不能阻止 Chaos/GOI。

M06 的 O4 electorate 是动态的：在线 Spectator、Overwatch 和当前 API 可识别的监管模式可以参与，存活玩家和断开连接者不能参与。Vote Session 不保存固定选民白名单；会话开始后新进入 O4 状态的玩家可以投票，离开 O4 状态的既有投票在结算时作废。没有合法 O4 时，O4-required SUPPORT 立即返回 `SKIPPED / NO_O4_AVAILABLE`，跳过当前支援机会，不等待、不自动替代、不消费 Professional Response、Event Cost 或 Commit。

## M06 O4 Panel contract

M06 的展示和选择只消费已完成的上游事实。普通 Hint 只向当前在线 Spectator/Overwatch 发送，使用 EXILED `Player.ShowHint(string, float)`；目标 API 没有可靠的 Hint anchor/offset，因此不伪造位置契约。

客户端当前使用 `o4vote <1|2>`（别名 `eevote`）投票。它通过 `ClientCommandHandler` 处理，不是 RA 命令；`ee o4 status` 只读。该输入通道仍为 `PROVISIONAL / TBD`。投票时与结算时都读取当前 O4 资格，每个 O4 每个 Session 只能投一次，不能改票。

M05 仅在 Foundation 多个合法普通 SUPPORT 候选时传入最多两个已排序候选。M06 不生成、排序、过滤候选，也不能影响 Chaos、GOI、Professional Response 或 NON_SUPPORT。多数票只能选择 shortlist 内候选；平票返回 `TIE` 与最高票候选集合，由 M05 在该集合内使用既有系统规则裁决；单候选不建立会话并直接返回 M05；无 O4 返回 `SKIPPED`。M05 必须继续在 Start/Commit 前重验证。

Round End、Restart、WaitingForPlayers、Plugin Disable、LOW_POPULATION_SUSPENDED 和 Runtime Cleanup 必须 Kill Hint/超时回调、取消一次会话并清空局部 O4 ID。O4 取消、失效或请求失败不得让 O4-required SUPPORT 隐式回退到原始候选；最近选择结果默认最多 256 条，不保留账号或身份标识。

## FacilityState

`IFacilityStateProvider` 是 Facility 状态来源接口。当前 `SnapshotFacilityStateProvider` 只可靠读取 `RoundSnapshot.WarheadDetonated`：已爆炸映射为 `FacilityState.Destroyed`，Normal/Lockdown/Evacuation 的正式运行来源仍是 `PROVISIONAL`。

当状态为 Destroyed 时，`RequiresUndergroundFacility=true` 的事件必须拒绝；Surface、External、Endgame 事件由自身要求判断。

## Logging and privacy

重要状态变化进入 Console，详细候选拒绝、选择、重验证、Rollback 和 FDI settlement 进入详细日志；RuntimeHarness 使用明确的 probe 名称和结构化证据。日志应能回答当前等级、危机、来源、人数缩减和失败原因。

当前事件事实优先使用回合内 player ID 和安全标签，不应写 Steam identifier 或 account identifier。GOI 生产输入仍为 `INTERFACE READY / PROVISIONAL`。

## RA channel

`EmergencyEventsCommand` 是 RemoteAdmin 命令，别名为 `ee`。它不是 LocalAdmin 的游戏控制台命令，因此在 LocalAdmin 中出现 `Command ee does not exist!` 不代表 RA parser 失败。可用语法见 [API Reference](API_REFERENCE.md) 和 [Testing](TESTING.md)。
