# D-LRC Evaluator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不改变 Round Core 和 Reinforcement 行为的前提下，实现只读的 D-LRC Evaluator，按 30 秒周期输出 `DLRC-A0` 到 `DLRC-E5`。

**Architecture:** 将评分、等级和控制状态拆成不依赖 EXILED 的纯逻辑层，并用无第三方测试依赖的自动测试运行器覆盖边界和固定场景。运行时层只负责一次性采集在线玩家、接收前两个模块的只读状态、调度评估、记录日志和清理状态。

**Tech Stack:** C# 12、.NET Framework 4.8、EXILED 9.14.2、MEC、PowerShell、独立的纯逻辑 console test runner。

**Spec:** `docs/superpowers/specs/2026-08-23-dlrc-evaluator-design.md`

## Global Constraints

- 第三模块只读，不刷人、不改门、不改角色、不改装备、不广播、不改胜负判断。
- 只输出 `DLRC-A0` 到 `DLRC-E5`，危机标签属于第四模块。
- Round Core 的人口档位和起始 SCP 数量以已锁定状态为准，不能根据当前人数重算。
- Support Score 不进入 Response Score，普通 NTF/CI 支援的选择和时序不改写。
- 每 30 秒从 06:31 开始评估，评估不得并发；失败时不发布虚假 L0。
- 纯逻辑计算使用 `double`，最后 Clamp 到 0–100，等级使用未取整分数。
- 回合结束清空所有评估、动量、快照和去重状态，不能泄漏到下一局。
- 代码注释使用中文，不能硬编码凭据，不能执行 commit 或 push。
- 当前用户提供的第三模块自动测试要求优先于交接文档中“暂不创建测试项目”的旧阶段约束。

---

### Task 1: 完成第二模块唯一一次最终验收基线

**Files:**
- Read: `handoff.md`
- Read: `Reinforcement/ReinforcementManager.cs`
- Read: `Reinforcement/ReinforcementState.cs`
- Verify: `EmergencyEvents.csproj`

**Interfaces:**
- Consumes: 当前仓库代码、已有服务器日志（若当前机器提供）。
- Produces: 一份当前会话中的验收结论，不修改 Reinforcement 行为。

- [ ] **Step 1: 检查工作树和远程状态**

Run:

```powershell
git status --short --branch
git remote -v
```

Expected: 明确当前分支、未提交改动和远程地址，禁止把远端同步状态猜成已推送。

- [ ] **Step 2: 对照现有代码核对支援验收矩阵**

核对 DD/Scientist 四种撤离积分、玩家 ID 去重、05:00/06:30 门控、Overwatch 排除、首波阵营方向、后续积分比例、25% 四舍五入、mini wave 取消、波次日志和回合清理。

Expected: 只记录已经有代码或日志证据的项目，缺少服务器引用或真实回合证据时标记为未验证，不宣布通过。

- [ ] **Step 3: 运行现有项目基线构建**

Run:

```powershell
dotnet build EmergencyEvents.csproj -c Release -p:SL_REFERENCES=<当前服务器的SCP:SL_Data\\Managed路径>
```

Expected: 在引用可用时 0 warning、0 error；引用不可用时保留明确阻塞，不用 NuGet 包路径冒充完整服务器引用。

---

### Task 2: 创建纯逻辑测试运行器和快照模型

**Files:**
- Create: `Evaluation/EvaluationModels.cs`
- Create: `Evaluation/EvaluationEnums.cs`
- Create: `Evaluation/EvaluationOptions.cs`
- Create: `Evaluation.Tests/Evaluation.Tests.csproj`
- Create: `Evaluation.Tests/Program.cs`

**Interfaces:**
- Consumes: `RoundCore/PopulationTier.cs`。
- Produces: `RoundSnapshot`、`ScpSnapshot`、`MajorWaveSnapshot`、`EvaluationOptions`、评估枚举和可运行的纯逻辑测试入口。

- [ ] **Step 1: 写第一个会失败的模型测试**

在 `Evaluation.Tests/Program.cs` 中先写最小断言，要求 `RoundSnapshot` 能保留锁定人口档位、起始 SCP 数量和评估时间，并让测试项目引用尚不存在的模型类型。

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 编译失败，原因是模型尚未定义。

- [ ] **Step 2: 写最小模型和选项类型**

实现只含数据的类型，包含规范列出的玩家统计、SCP 状态、波次历史、核弹状态和最近 120 秒死亡数；默认选项包含满值僵尸数量 6、趋势窗口 300 秒、动量窗口 120 秒、核弹取消每次 5 分且最多 10 分，以及 A–E 阈值。

- [ ] **Step 3: 运行模型测试确认通过**

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 首个模型测试通过，输出测试数量和 `0 failed`。

---

### Task 3: 以 TDD 实现 Response Score 和理论等级

**Files:**
- Create: `Evaluation/ResponseBreakdown.cs`
- Create: `Evaluation/ResponseScoreCalculator.cs`
- Create: `Evaluation/LevelResolver.cs`
- Modify: `Evaluation.Tests/Program.cs`

**Interfaces:**
- Consumes: `RoundSnapshot`、`EvaluationOptions`、`MajorWaveSnapshot`。
- Produces: `ResponseScoreResult`、详细 `ResponseBreakdown`、`LevelResolver.ResolveTheoreticalLevel`。

- [ ] **Step 1: 先添加失败的 A–E 阈值边界测试**

测试 C 档至少覆盖 `21.99 -> 0`、`22.00 -> 1`、`35.99 -> 1`、`36.00 -> 2`、`51.99 -> 2`、`52.00 -> 3`、`68.99 -> 3`、`69.00 -> 4`、`85.99 -> 4`、`86.00 -> 5`，并用同样方式覆盖 A、B、D、E 的每个最小值。

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 测试失败，原因是等级解析器尚未实现。

- [ ] **Step 2: 实现最小等级解析器并让边界测试通过**

使用每级最低分数组从 L5 向 L0 比较，保持原始 `double`，拒绝无效档位或阈值长度不足。

- [ ] **Step 3: 先添加失败的五项评分测试**

覆盖 SCP Presence、满血/半血/极残/Hume/MaxHP=0、049-2 0/3/6/12、079 不存在和等级 1–5、Foundation 50/49.99/40/39.99/30/29.99/20/19.99/10/9.99、观察者比例边界、波次 100/76/75/51/50/26/25/1/0、时间边界和核弹取消 0/1/2。

- [ ] **Step 4: 实现最小评分计算器并让评分测试通过**

严格按规格中的五项公式和边界计算，SCP-079 排除普通 Health，Support Score 不读取，核弹启动不加分，最后统一 Clamp。

- [ ] **Step 5: 添加连续支援失败和高分限制前置测试**

验证最新已完成波次和上一波基础失败都至少 8 时增加 5 且封顶 20，并验证当前波未到 120 秒时回退上一条已完成波次。

- [ ] **Step 6: 运行全部纯逻辑测试**

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 当前测试全部通过，输出无异常和 `0 failed`。

---

### Task 4: 以 TDD 实现 Control State、最终结果和历史 Ring Buffer

**Files:**
- Create: `Evaluation/ControlAssessment.cs`
- Create: `Evaluation/ControlEvaluator.cs`
- Create: `Evaluation/EvaluationHistory.cs`
- Create: `Evaluation/DlrcEvaluationResult.cs`
- Create: `Evaluation/DlrcEvaluator.cs`
- Modify: `Evaluation.Tests/Program.cs`

**Interfaces:**
- Consumes: `RoundSnapshot`、`ResponseScoreResult`、`EvaluationHistory`、`EvaluationOptions`。
- Produces: `ControlAssessment`、`ControlState`、`DlrcEvaluationResult`、最多保留 20 项的历史。

- [ ] **Step 1: 添加六个固定 Control 场景的失败测试**

覆盖明显优势得到 ADVANTAGE、普通拉锯得到 CONTROLLED、开始失控得到 UNCONTROLLED、严重失控但不满足硬条件仍为 UNCONTROLLED、Foundation=0 且 Threat>0 得 COLLAPSE、连续两次 CATASTROPHIC 且 Threat 非 IMPROVING 得 COLLAPSE。

- [ ] **Step 2: 添加高分可控和低分不可抬高测试**

验证 NaturalScore=95 且 CONTROLLED 时 FinalLevel 不超过 3，ADVANTAGE 时不超过 2；验证理论等级 2 即使 Control=COLLAPSE 也只能保持 2。

- [ ] **Step 3: 添加失败保留和环形历史测试**

验证历史超过 20 项时只保留最近 20 项，首次评估失败不发布结果，后续失败返回上一有效结果并保持当前代码。

- [ ] **Step 4: 实现最小 Control、History 和聚合评估器**

按硬条件、负面信号、正面信号顺序判定 Control，使用 `min(TheoreticalLevel, ControlLevelCap)` 解析最终等级，并把当前结果加入 Ring Buffer。

- [ ] **Step 5: 运行全部纯逻辑测试并核对边界**

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 所有模型、评分、等级、Control、失败处理和历史测试通过。

---

### Task 5: 增加运行时配置和 Reinforcement 只读波次历史

**Files:**
- Modify: `Config.cs`
- Modify: `Reinforcement/ReinforcementState.cs`
- Modify: `Reinforcement/ReinforcementManager.cs`

**Interfaces:**
- Consumes: EXILED 配置序列化、当前正常波次生命周期。
- Produces: 391 秒启动、30 秒间隔、僵尸/趋势/动量/核弹配置、A–E 阈值配置，以及只读 `MajorWaveHistory`。

- [ ] **Step 1: 添加默认选项检查**

在纯逻辑测试中验证默认选项为 391、30、6、300、120、5、10 和五组阈值，并验证非法负值会被运行时选项 Clamp 或回退到安全默认值。

- [ ] **Step 2: 实现配置字段和纯逻辑选项转换**

把 YAML 可调整的数值暴露到 `Config`，不把正负信号数量等每个分支做成独立配置；稳定规则保持在代码中。

- [ ] **Step 3: 记录大型支援成员和正式评估结果**

在实际 `RespawnedTeam` 后记录波次名称、起始人数、玩家 ID 和开始时间；在 120 秒回调计算存活人数，提前全灭时记录 CATASTROPHIC，向外只提供快照副本，不改变阵营选择或门控。

- [ ] **Step 4: 运行支援相关构建检查**

Run:

```powershell
dotnet build EmergencyEvents.csproj -c Release -p:SL_REFERENCES=<当前服务器的SCP:SL_Data\\Managed路径>
```

Expected: 支援原有日志、阵营选择、积分衰减和门控代码行为不改变。

---

### Task 6: 以 TDD 接入 SnapshotCollector 和 Service

**Files:**
- Create: `Evaluation/SnapshotCollector.cs`
- Create: `Evaluation/BattlefieldMomentumTracker.cs`
- Create: `Evaluation/EvaluationLogFormatter.cs`
- Create: `Evaluation/DlrcEvaluatorService.cs`
- Modify: `Plugin.cs`
- Modify: `Evaluation.Tests/Program.cs` only for pure formatter/tracker tests.

**Interfaces:**
- Consumes: Round Core state、Reinforcement 波次历史、EXILED `Player.Enumerable`、`Warhead`、`Round.ElapsedTime`。
- Produces: 06:31 启动、30 秒调度、一次玩家扫描的 `RoundSnapshot`、详细日志、失败保留和回合清理。

- [ ] **Step 1: 添加失败的调度与清理契约测试**

测试纯逻辑调度计算：回合时间 390 秒不评估、391 秒立即评估、之后步长为 30 秒；测试 Ring Buffer、动量队列和核弹取消去重的清理后为空。

- [ ] **Step 2: 实现统一玩家分类和一次扫描快照**

运行时只把 `Player.Enumerable` 物化一次，然后填充 Foundation、敌对人类、D 级、科学家、观察者、Overwatch、SCP、活跃玩家 ID 和统一分类计数；SCP-079 从 `Scp079Role.Level` 读取，健康数据异常只跳过该项。

- [ ] **Step 3: 接入核弹取消和死亡动量事件**

监听有效的 Warhead Stopping 和玩家死亡事件，只更新本局的取消计数与最近 120 秒动量，不在这些事件中触发事件导演或改变游戏状态。

- [ ] **Step 4: 实现服务启动、非并发、失败回退和详细日志**

在 `RoundStarted` 捕获已锁定的 Round Core 状态后创建评估服务，按 391 秒和 30 秒调度；每轮先采集固定快照，再在内存中完成全部计算；完整结果写 Debug/File 日志，等级或 Control 变化写简短日志。

- [ ] **Step 5: 在 WaitingForPlayers、RoundEnded 和 OnDisabled 清理服务**

停止计时器、取消已注册监听、清空历史、动量、快照、最后结果和核弹去重集合，并记录逐项清理成功状态。

---

### Task 7: 完成最终验证和边界审查

**Files:**
- Verify: 所有 `Evaluation/*.cs`
- Verify: `Config.cs`
- Verify: `Plugin.cs`
- Verify: `Reinforcement/*.cs`
- Verify: `docs/superpowers/specs/2026-08-23-dlrc-evaluator-design.md`

**Interfaces:**
- Consumes: 纯逻辑测试、当前服务器引用、构建输出和服务器日志。
- Produces: 可复核的完成结论，或明确列出环境阻塞，不提交、不推送。

- [ ] **Step 1: 运行纯逻辑自动测试**

Run:

```powershell
dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj
```

Expected: 所有测试通过，失败数为 0。

- [ ] **Step 2: 运行项目 Release 构建**

Run:

```powershell
dotnet build EmergencyEvents.csproj -c Release -p:SL_REFERENCES=<当前服务器的SCP:SL_Data\\Managed路径>
```

Expected: 0 warning、0 error，并生成 `bin/Release/net48/EmergencyEvents.dll`。

- [ ] **Step 3: 检查模块边界**

Run:

```powershell
rg -n "BIO|SYS|CON|SEC|GOI|WAR|END|Event Director|O4|ForceWave|Respawn\\.ForceWave" Evaluation Plugin.cs Reinforcement
```

Expected: 第三模块没有危机、事件导演、O4 或普通支援行为实现，也没有调用 `Respawn.ForceWave`。

- [ ] **Step 4: 在真实服务器完成最小集成回合**

观察 06:31、07:01、07:31、08:01 的评估日志，以及 SCP 掉血、079 升级、049-2 增加、基金会阵亡、支援团灭、核弹启动/取消和回合结束清理。

Expected: 生成 `DLRC-C0` 到 `DLRC-C5` 格式的无危机代码，30 秒周期稳定，核弹启动不加分、取消才加分，失败保留上一结果，回合结束输出 `Cleanup=SUCCESS`。

- [ ] **Step 5: 重新检查工作树并报告未执行的外部操作**

Run:

```powershell
git status --short --branch
```

Expected: 只列出本次实现文件和测试输出忽略项；不执行 commit 或 push，除非用户另行授权。
