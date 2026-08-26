# Architecture

## 端到端调用链

```text
Round Start
  -> M01 Round Core
  -> Locked Population Tier
  -> M02 Reinforcement Integration
  -> M03 D-LRC Evaluation
  -> M04 Crisis Assessment
  -> M04.5 Facility Disorder Index
  -> M05 Event Director
  -> Future Event Pack execution
  -> M06 O4 Panel (DEFERRED BY DESIGN)
```

模块之间通过已完成的快照、事件和只读上下文传递事实。下游可以消费上游结果，但不得用另一套算法重新推导同一事实。

## M01 — Round Core

**Purpose**：判断本局是否由 Emergency Events 接管，并在回合开始时锁定人口档位。

**Inputs**：当前在线人数、配置启用状态、回合生命周期事件。

**Outputs**：`PluginRuntimeState`、回合开始人口、`PopulationTier`、可供后续模块使用的生命周期边界。

**State owner**：`Runtime.PluginRuntimeCoordinator` 和 Round Core 管理器。

**Lifecycle**：回合开始时少于 `MinimumPlayers`（默认 16）时保持 STANDBY；活动回合中降到最低人数以下后进入 `LOW_POPULATION_SUSPENDED`，本回合不可逆恢复。下一局重新判断。

**Must not do**：不选择事件来源，不实现危机，不修改原版 Primary Wave 的职业、装备或阵营决定。

## M02 — Reinforcement Integration

**Purpose**：在原版刷新流程周围记录可复用的波次事实，并执行 Emergency Events 自己拥有的边界策略。

**Vanilla-owned facts**：MTF/CI 阵营、Influence、Respawn Token、原版计时器、原版玩家选择、职业组成、装备和实际出生流程。

**EmergencyEvents-owned facts**：Mini-Wave 禁用策略、Primary Wave 人数 cap、实际出生人数、成员 ID、阵营、完成时间、波次历史、Timer Extension 和 `POST_MAJOR_WAVE` 通知。

`MajorWaveHistory` 保存 `CurrentWave`、`LastMajorWave`、`PreviousMajorWave` 和有界记录。每个 Primary Wave 只允许产生一次 `MajorWaveCompletedEvent`。

**Must not do**：不得主动扩充原版计划人数，不得重实现原版候选选择或职业组成，不得把 Chaos Wave 当作 Foundation containment checkpoint。

## M03 — D-LRC Evaluator

**Purpose**：根据一次 `RoundSnapshot` 计算 Response Score、Control 和最终 D-LRC 等级。

**Inputs**：锁定的人口档位、SCP 状态、人员和 Spectator、波次快照、死亡窗口、核弹事实和已发布时间事实。

**Outputs**：`DlrcEvaluationResult`、`ResponseBreakdown`、Theoretical/Final Level、Control State、代码和有界 Evaluation History。

五个主要压力方向是 SCP Threat、Foundation Pressure、Foundation Reinforcement Failure、Time Pressure 和 Strategic Hazard。Foundation Reinforcement Failure 只读取 Foundation/MTF Primary Wave，Chaos Wave 不能进入该项或 Collapse C 的 Foundation 失败语义。

**Must not do**：不读取 Crisis Severity，不让危机标签改变 Global D-LRC 的独立计算，不把未来平衡阈值当作最终设计。

## M04 — Crisis System

**Purpose**：用无状态 Detector 读取快照，聚合危机 Active/Inactive 状态并维护 Episode。

**Inputs**：`RoundSnapshot`、同一次评估的 `DlrcEvaluationResult`、`CrisisState`。

**Outputs**：`CrisisAssessment`、ActiveTags、ActivatedTags、ResolvedTags、每个活动标签的 Episode ID，以及 `CrisisChanged` 事件。

**State owner**：`CrisisManager` 管理活动 Episode 和有界 processed evaluation IDs；各 Detector 不拥有跨评估业务状态，CON/END 的必要时序事实由 `CrisisState` 保存。

**Must not do**：不得创建危机自身的 L3/L4/L5。事件的 L4 是 `EventResponseLevel`，不是危机等级。

## M04.5 — Facility Disorder Index

**Purpose**：在 0–100 范围内记录设施秩序扰动，作为历史/诊断事实，并临时影响 M05 普通 SUPPORT 来源仲裁。

**Inputs**：合法的同回合 M03 Evaluation、同一次 Evaluation 的 CrisisAssessment、当前库存快照和 DisorderEvent。

**Outputs**：`FacilityDisorderState`、`FacilityDisorderSettlement`、事件历史和解释信息。

首次结算在 06:31 语义上由 Current Stock 与最近 120 秒的瞬时事件组成，并过滤已被 Current Stock 表达的重复状态。后续周期只使用 `PreviousFDI + NewEventDelta`。

**Must not do**：不把 FDI 变成 D-LRC，不参与 Professional Crisis Response 资格，不在上游 Evaluation 无效时消费事件或推进窗口。

## M05 — Event Director

**Purpose**：选择和协调未来事件的声明式生命周期，不生成正式 Event Pack 内容。

**Inputs**：`DirectorContext` 中的 M03、M04、M04.5、M02 和 M01 官方事实。

**Outputs**：`EventCandidate`、`DirectorCycle`、来源选择、人数计划、生命周期日志和第二槽位 DueAt。

**专业响应**：优先于普通 SUPPORT 来源，资格由 D-LRC Response Level、Active Crisis Tags、Episode、人口计划和事件要求决定。

**普通 SUPPORT**：只有合法候选参加来源仲裁，FDI 只影响 Foundation、Chaos、GOI 的临时权重。

**NON_SUPPORT**：不读取 FDI，不参与普通 SUPPORT 来源抽取。

**Must not do**：不重算上游状态，不实现 Event Pack 的枪械/装备/职业执行，不要求 M06 当前存在。

## M06 — O4 Panel

状态为 `DEFERRED BY DESIGN`。当前 M05 只保留 Foundation 多候选时的 O4SelectionRequired 边界和无 O4 时的确定性回退，不实现 HUD、Panel、投票、玩家资格或 Observer UX。
