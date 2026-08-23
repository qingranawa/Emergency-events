# D-LRC Evaluator 设计规格

> 模块：03
>
> 更新时间：2026-08-23
>
> 依据：当前会话提供的第三模块完整设计，以及 `handoff.md` 的项目边界。

## 目标

第三模块只读取 SCP: Secret Laboratory 的真实回合状态，生成统一的 `RoundSnapshot`，计算 0–100 的 Response Score，判断现场控制状态，并输出 `DLRC-A0` 到 `DLRC-E5` 的最终响应代码。

固定数据流：

```text
游戏实际状态
    -> RoundSnapshot
    -> NaturalResponseScore
    -> Response Adjustments
    -> EffectiveResponseScore
    -> ControlState
    -> TheoreticalResponseLevel
    -> ControlLevelCap
    -> FinalResponseLevel
    -> DLRC-A0 ~ DLRC-E5
```

Evaluator 是只读分析系统，不刷人、不锁门、不改角色、不改装备、不广播、不执行事件、不改变胜负判断，也不改变普通 NTF/CI 支援逻辑。

## 明确不属于本模块

本模块不实现 BIO、SYS、CON、SEC、GOI、WAR、END、Beta-7、Nu-7、O4 投票、O4 面板、特殊事件、Event Director、人员刷新、普通支援积分、胜负判断、RA 命令或数据库。

危机标签属于第四模块，因此本模块只能输出 `DLRC-C3`，不能输出 `DLRC-C3-BIO`。

## 与前两个模块的接口

从 Round Core 只读取 `RoundId`、锁定的开局人口、锁定的 `PopulationTier` 和 `StartingScpCount`，绝不根据当前人数重新计算档位。

从 Reinforcement 只读取已完成大型支援的历史、最近一波和上一波表现，Support Score 不进入 Response Score。

第三模块对外只发布当前 `DlrcEvaluationResult`，供后续模块读取，不反向修改前两个模块。

## 运行周期

- 06:31（391 秒）第一次评估。
- 之后每 30 秒评估一次。
- 每次评估都按当前事实重新计算，允许直接升级或降级，不使用隐藏滞后。
- 同一时间只能有一个评估运行，上一轮未完成时下一轮记录 `SKIPPED` 和 `Reason=PreviousEvaluationStillRunning`。
- 第一次评估失败且没有历史有效结果时不发布虚假的 `DLRC-C0`。
- 后续评估失败时保留上一份有效结果。

## RoundSnapshot

快照至少包含：

```text
RoundId, Timestamp, RoundElapsedTime
PopulationTier, RoundStartPopulation, CurrentOnlinePlayers
FoundationCombatants, ChaosCombatants, OtherHostileCombatants
ClassDAlive, ScientistsAlive, EligibleSpectators, OverwatchCount
MainScpAlive, StartingScpCount, ScpStates[], Scp0492Count
Scp079Present, Scp079Tier
WarheadUnlocked, WarheadActive, WarheadDetonated
WarheadCancellationCount
LastMajorWave, PreviousMajorWave
RecentFoundationDeaths120s, RecentHostileDeaths120s
RecentMainScpDeaths120s
```

快照采集时只扫描一次在线玩家列表，复制成内存对象后，后续计算只读该对象。

Foundation Combatants 包括 Facility Guard、NTF Private、NTF Sergeant、NTF Captain 和 NTF Specialist。

Hostile Human Combatants 包括 Chaos Conscript、Chaos Rifleman、Chaos Marauder 和 Chaos Repressor。

Main SCP 包括正常主要 SCP，不包括 SCP-049-2；049-2 单独统计。SCP-079 计入 Presence，但不计入普通 Health。

## Response Score

总分使用 `double` 计算，最后限制在 0–100，内部步骤不提前取整。

| 部分 | 上限 |
|---|---:|
| SCP Threat | 40 |
| Foundation Pressure | 20 |
| Reinforcement Failure | 20 |
| Time Pressure | 10 |
| Strategic Hazard | 10 |

`NaturalResponseScore` 是以上五项之和，`EffectiveResponseScore` 再加上当前预留的 `PersistentAdjustment`，第一版调整值固定为 0，最后 Clamp 到 0–100。

### SCP Threat 0–40

- SCP Presence 0–20：`MainScpAlive / StartingScpCount * 20`，比例 Clamp 到 0–1。
- SCP Health 0–10：对每个有有效 HP/Hume 最大值的主要 SCP，计算 `(CurrentHP + CurrentHume) / (MaxHP + MaxHume)`，将所有有效比例之和除以 `StartingScpCount` 再乘 10。SCP-079 不参与，单个异常数据只记录 WARN 并排除。
- 049-2 Pressure 0–4：`min(Scp0492Count / ZombieFullPressureCount, 1) * 4`，默认满值数量为 6。
- SCP-079 Pressure 0–6：等级 1/2/3/4/5 对应 0/1.5/3/4.5/6，不存在为 0。

### Foundation Pressure 0–20

```text
SCPCombatEquivalent = MainScpAlive + Scp0492Count / 3
CombatTotal = FoundationCombatants + HostileHumanCombatants + SCPCombatEquivalent
FoundationCombatShare = FoundationCombatants / CombatTotal
```

CombatTotal 为 0 时占比按 1 处理。

| FoundationCombatShare | Combat Pressure |
|---|---:|
| >= 50% | 0 |
| >= 40% 且 < 50% | 3 |
| >= 30% 且 < 40% | 6 |
| >= 20% 且 < 30% | 10 |
| >= 10% 且 < 20% | 12 |
| < 10% | 14 |

Spectator Pressure 使用 `EligibleSpectators / CurrentOnlinePlayers`：低于 10% 为 0，之后每个 10% 区间依次为 1、2、3、4，达到 50% 为 6。

### Reinforcement Failure 0–20

只评价已完成正式评估的正常大型支援，不评价刚刷新且未满 120 秒的波次；没有可用波次时为 0。

存活率边界为：大于 75% 得 0，大于 50% 得 4，大于 25% 得 8，大于 0 得 12，0 得 15。当前波和上一波基础失败分都至少为 8 时增加 5，最终封顶 20。120 秒前团灭的波次可立即记录为 15 分的 CATASTROPHIC。

### Time Pressure 0–10

回合时间 `<10:00`、`10:00–14:59`、`15:00–19:59`、`20:00–24:59`、`25:00–29:59`、`>=30:00` 对应 0、2、4、6、8、10。

### Strategic Hazard 0–10

核弹解锁、启动和进行中本身都不加分。每次有效取消加 5，最高 10。同一个取消事件只能计一次。

## 理论响应等级

等级解析使用每级最低分，不使用闭区间；比较未取整的原始分数，达到阈值立即进入该级。

| Tier | L0 | L1 | L2 | L3 | L4 | L5 |
|---|---:|---:|---:|---:|---:|---:|
| E | 0 | 18 | 32 | 48 | 65 | 82 |
| D | 0 | 20 | 34 | 50 | 67 | 84 |
| C | 0 | 22 | 36 | 52 | 69 | 86 |
| B | 0 | 24 | 38 | 54 | 71 | 88 |
| A | 0 | 26 | 40 | 56 | 73 | 90 |

## Control State

Control State 只使用四个值：`ADVANTAGE`、`CONTROLLED`、`UNCONTROLLED`、`COLLAPSE`。

### 四个信号

- Threat Trend：比较当前 SCP Threat 与约 5 分钟前的有效历史值，历史不足时为 `INSUFFICIENT`；Delta <= -5 为 `IMPROVING`，Delta >= +5 为 `WORSENING`，其余且当前 Threat >= 28 为 `STALLED_HIGH`，剩余为 `STABLE`。
- Foundation Strength：占比 >=45% 为 STRONG，>=30% 为 ADEQUATE，>=15% 为 WEAK，<15% 为 CRITICAL。
- Wave Performance：基础失败 <=4 为 GOOD，等于 8 为 NEUTRAL，>=12 为 POOR，团灭为 CATASTROPHIC。
- Recent Battlefield Momentum：最近 120 秒敌方损失为 HostileHumanDeaths + MainScpDeaths。敌方损失 >=3 且比 FoundationDeaths 至少多 2 为 FOUNDATION_POSITIVE；反方向同理为 FOUNDATION_NEGATIVE；其他为 NEUTRAL。

Threat 为 WORSENING 或 STALLED_HIGH、Foundation 为 WEAK 或 CRITICAL、Wave 为 POOR 或 CATASTROPHIC、Momentum 为 FOUNDATION_NEGATIVE 时，各自算一个负面信号。

Threat 为 IMPROVING、Foundation 为 STRONG、Wave 为 GOOD、Momentum 为 FOUNDATION_POSITIVE 时，各自算一个正面信号。

### 状态判定

以下任一条件直接为 COLLAPSE：

1. FoundationCombatants 为 0 且 SCPThreat 大于 0。
2. FoundationCombatShare 小于 10%，Threat 为 WORSENING 或 STALLED_HIGH，且 NaturalResponseScore >= 65。
3. 最近两次正式大型支援均为 CATASTROPHIC，且 Threat 不是 IMPROVING。

不满足 COLLAPSE 时，负面信号至少 2 个为 UNCONTROLLED。

不满足以上条件时，正面信号至少 2 个、负面信号为 0，并且 Threat 为 IMPROVING 或 Foundation 为 STRONG 时为 ADVANTAGE。

其他情况为 CONTROLLED。

Control State 对等级的上限为 ADVANTAGE=2、CONTROLLED=3、UNCONTROLLED=4、COLLAPSE=5，最终等级为 `min(TheoreticalLevel, ControlLevelCap)`。Control 只能限制等级，不能把低等级抬高。

## 历史、日志和清理

内存只保留最近 20 个评估结果的 Ring Buffer，完整历史交给服务器日志。

每次评估输出详细的 Snapshot、SCP Threat、Foundation Pressure、支援失败、时间、核弹、Response Breakdown、理论等级、Control Assessment 和最终结果日志。详细内容写入 Debug/File 日志，控制台只输出等级或控制状态变化及异常。

评估失败时记录失败原因，并按规则保留上一有效结果。回合结束必须停止计时器，清空 EvaluationHistory、Momentum 窗口、Snapshot、最后结果和核弹取消去重状态，取消监听并记录 `Cleanup=SUCCESS`。

## 完成验收

必须通过纯逻辑自动测试、项目构建和服务器集成验证，且不得新增任何危机、事件导演、O4 或普通支援行为。测试必须覆盖 A–E 阈值边界、SCP Presence/Health、049-2、079、Foundation/Spectator 边界、波次存活率、连续失败、时间、核弹取消、Control 六个固定场景、高分可控限制、低分不能被 Control 抬高、失败保留上一结果和回合清理。
