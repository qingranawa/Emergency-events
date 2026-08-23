# emergency-events 项目交接文档

> 更新时间：2026-08-23
>
> 交接目的：换电脑后继续开发 emergency-events。本文记录当前代码、已经验证的行为、尚未完成的测试、整体产品目标、技术边界和下一步实施顺序。

## 0. 一页结论

- 项目目录：D:\project\emergency-events
- 项目名称：emergency-events
- 程序集 / DLL：EmergencyEvents.dll
- 根命名空间：EmergencyEvents
- EXILED：9.14.2
- Target Framework：.NET Framework 4.8
- C#：12.0
- 当前版本：0.1.0
- 远程仓库：https://github.com/qingranawa/Emergency-events
- 当前已经完成：Round Core 开局编制主体、开局角色分配、装备、HCZ 出生点、Badge 追加、运行时校验、普通支援基础调度。
- 当前已经实机确认：17 人 E 档开局、精确编制、两只 SCP-939、安保/混沌装备、HCZ A/B 交换、Owner Badge 追加；用户确认 Badge 测试正常。
- 当前没有完成：D-LRC Evaluator、Crisis System、Event Director、O4 投票、特殊事件包和 RA 管理命令。
- 当前本地仓库已有初始提交 535e3aa（feat: Implement Five-Minute Normal Reinforcement Plan），包含当前源代码、计划文档、handoff.md 和 .gitignore。本次会话没有主动发出 commit 或 push 命令；远端是否已有该提交尚未确认。
- 按用户要求没有创建测试项目；验证方式是构建、日志和真实服务器回合测试。

## 1. 换电脑前后的 Git 说明

### 本次已完成

本目录原本不是 Git 仓库。本次已完成初始化和远程绑定；当前本地 HEAD 是提交 535e3aa：

~~~text
git init
git branch -M main
git remote add origin https://github.com/qingranawa/Emergency-events.git
~~~

执行后用下面命令确认：

~~~text
git status --short --branch
git remote -v
~~~

### 重要：远程推送状态尚未确认

本地有提交不等于 GitHub 远端已有提交。本次核对时 git ls-remote 因 GitHub 凭据读取失败而无法完成，因此不要把远端状态猜成已推送或未推送。

在确认远端状态和用户授权之前，不要擅自 push。若确认需要上传，先检查工作区，再执行：

~~~text
git add .
git status
git push -u origin main
~~~

推送前检查 git status，确认没有把 bin/、obj/、本机日志、服务器配置或凭据加入提交。

### 新电脑接手

如果远程已经完成首次 push：

~~~text
git clone https://github.com/qingranawa/Emergency-events.git
cd Emergency-events
~~~

如果还没有 push，需要先把当前工作区完整复制到新电脑，或者回到旧电脑完成 commit/push 后再 clone。

## 2. 整个插件最终要做什么

emergency-events 不是单纯的随机事件插件，而是一套基于 SCP: Secret Laboratory + EXILED 的动态回合导演系统。

核心系统名称是：

~~~text
D-LRC — Dynamic Lockdown Response Code
动态封锁响应代码
~~~

状态代码采用：

~~~text
DLRC-A4-BIO
~~~

其中人口编制、理论危机等级和具体危机类型最终共同决定回合进入什么响应状态。

最终目标包括以下模块：

### 2.1 Round Core

回合开始时读取有效开局人口，锁定 A–E 人口档位和精确编制。锁定后，本局不因后续加入、离开、死亡、逃脱或重连而重新计算。

人口档位：

| 档位 | 开局人口 |
|---|---:|
| E | 16–19 |
| D | 20–25 |
| C | 26–31 |
| B | 32–37 |
| A | 38–45 |

根据人口分配：

- SCP
- Foundation Security
- Chaos Infiltrator
- Class-D
- Scientist

Foundation Security 和 Chaos Infiltrator 数量镜像。两边使用镜像开局装备，出生在 HCZ Elevator A/B，并且每局随机交换 A/B。

目标是尽量保留原版 SCP:SL 行为：角色能力、伤害、Keycard、914、Gate、物品、缴械、撤离和胜利判断都不由本插件重写。

### 2.2 Reinforcement System

接管普通支援波次的阵营选择和间隔门控，但仍使用原版 EXILED / SCP:SL 支援管线，不自己伪造整套刷新系统。

当前设计：

- Foundation Support Score
- Chaos Support Score
- Class-D 价值：1 分
- Scientist 价值：2 分
- 第一正常大波窗口：约 05:00
- 第一波观察者等待底线：06:30
- Overwatch 不计入第一波正常观察者
- 小波取消
- 正常大波之间最短间隔：5 分钟
- 插件独占普通大型支援调度，原版独立正常波次和 mini wave 不得自行刷新
- 第一波及后续波次统一按双方 Support Score 比例随机选择 NTF/CI，0:0 时 50/50
- 普通大型支援只在 05:00、10:00、15:00 等固定窗口处理，空窗口跳过但不漂移
- 每次实际支援周期结束后保留 25% 积分，采用 AwayFromZero 四舍五入规则
- 开局后加入且为普通观察者的 dummy 可进入后续支援，开局编制人口仍按锁定值
- 主要 SCP 死亡、伤害阈值、自然 SCP 物品和消耗品物品按实例去重计分，SCP-914 产物排除

### 2.3 D-LRC Evaluator

计划从 06:31 开始运行，每 30 秒采集一次 Round Snapshot，包括：

- 当前人口
- Foundation 有效力量
- Chaos 有效力量
- SCP 数量和有效生命状态
- 049-2 数量
- SCP-079 等级
- 当前观察者
- 上一波支援表现
- 核弹状态
- 正在进行的特殊事件

根据 Snapshot 计算：

- Response Score：约 0–100 的理论危机压力
- Control State：ADVANTAGE、CONTROLLED、UNCONTROLLED、COLLAPSE
- 最终响应等级：0–5

约束：

- 4 级原则上至少需要 UNCONTROLLED
- 5 级原则上需要 COLLAPSE
- 不能只因为账面分数高，就把实际可控的回合判成 5 级

### 2.4 Crisis System

危机不是随机事件本身，而是专业事件池的开启条件。计划中的危机池：

- BIO：生化危机
- SYS：系统危机
- CON：收容危机
- SEC：安全危机
- GOI：第三方敌对势力危机
- WAR：核危机
- END：终局危机

例子：

- 多名 049-2 形成有效生化威胁时开启 BIO
- SCP-079 达到 Level 3 及以上时开启 SYS
- 第二次实际大型支援后 5 分钟，SCP 当量减少不足 1 时开启 CON
- 基金会有效力量严重不足时开启 SEC
- 敌对 GOI 已经出现、整体至少 3 级且基金会明显弱势时开启 GOI
- 核弹被解锁时开启 WAR
- 核爆后 SCP 与人类在地表长期僵持时开启 END

### 2.5 Event Director

特殊事件大约每 120 秒评估一次：

~~~text
当前 DLRC
    -> 当前响应等级
    -> 当前 Crisis
    -> 开启对应事件池
    -> 筛选同等级事件
    -> 检查特殊条件
    -> 检查观察者人数
    -> 检查人员刷新冷却
    -> 得到合法候选
    -> O4 Command 投票
    -> 执行事件
~~~

人员事件之间至少 180 秒刷新冷却。每个事件需要有自己的开始条件、成功条件、失败条件、终止条件、规模和 A–E 适配。

### 2.6 O4 Command 和事件包

O4 只负责在已经通过合法性筛选的候选中投票，不应该看到不该暴露的地点、概率或内部候选池信息。

事件包计划按一次一个的方式实现，优先从 BIO Level 3 开始，再逐步加入 SYS、CON、SEC、GOI、WAR、END。

第一版不需要数据库，不做 RA 管理命令，不重写原版胜利判断。

## 3. 当前代码结构

~~~text
emergency-events/
├─ EmergencyEvents.csproj
├─ Plugin.cs
├─ Config.cs
├─ handoff.md
├─ .gitignore
├─ RoundCore/
│  ├─ PopulationTier.cs
│  ├─ RoundComposition.cs
│  ├─ CompositionTable.cs
│  ├─ CompositionResolver.cs
│  ├─ RoundCoreState.cs
│  └─ RoundCoreManager.cs
├─ Reinforcement/
│  ├─ ReinforcementState.cs
│  └─ ReinforcementManager.cs
└─ docs/superpowers/
   ├─ specs/2026-08-22-round-core-design.md
   └─ plans/
      ├─ 2026-08-22-round-core.md
      ├─ 2026-08-22-reinforcement-system.md
      └─ 2026-08-23-five-minute-normal-reinforcement.md
~~~

已有规格和计划是设计记录，不要重复改写为另一套真相源：

- Round Core 设计：docs/superpowers/specs/2026-08-22-round-core-design.md
- Round Core 原计划：docs/superpowers/plans/2026-08-22-round-core.md
- Reinforcement 计划：docs/superpowers/plans/2026-08-22-reinforcement-system.md
- 五分钟正常支援计划：docs/superpowers/plans/2026-08-23-five-minute-normal-reinforcement.md

## 4. 当前已实现的代码行为

### 4.1 项目和插件入口

Plugin.cs 当前：

- 插件名：EmergencyEvents
- 作者：Qingran
- 版本：0.1.0
- 需要 EXILED：9.14.2
- 注册 WaitingForPlayers、RoundStarted、AllPlayersSpawned、RoundEnded
- 注册 SelectingRespawnTeam、RespawningTeam、RespawnedTeam
- 注册 Player Escaped
- 在禁用时取消事件并清理两个管理器的状态

EmergencyEvents.csproj 当前：

- net48
- C# 12
- x64
- Nullable enabled
- Deterministic build
- Treat warnings as errors
- NuGet 依赖 ExMod.Exiled 9.14.2

### 4.2 Round Core 纯逻辑

CompositionTable.cs 是 16–45 的权威表，字段顺序固定为：

~~~text
SCP / Security / Chaos / Class-D / Scientist
~~~

CompositionResolver.GetComposition(int)：

- 16–45 返回精确组成
- 16–19 返回 E
- 20–25 返回 D
- 26–31 返回 C
- 32–37 返回 B
- 38–45 返回 A
- 小于 16：回退 E，但 IsSupported=false
- 大于 45：回退 A，但 IsSupported=false
- 精确结果会校验总人数和 Security/Chaos 镜像关系

### 4.3 Round Core 运行时

RoundCoreManager 当前流程：

1. 捕获回合开始有效玩家列表和 RoundId。
2. 排除 Spectator 和 Overwatch，锁定开局名单。
3. 根据锁定人口取得精确编制。
4. 随机打乱玩家并分配角色。
5. SCP 角色池包含：049、079、096、106、173、3114、939。
6. 每局 SCP 角色池只固定一只 SCP-939，避免高人口回合无条件生成第二只 939。
7. 剩余 SCP 槽位从其余池随机选择，因此 3114 仍然可能作为额外 SCP 出现。
8. 分配 FacilityGuard、ChaosConscript、ClassD、Scientist。
9. 安保和混沌共用开局装备。
10. 安保额外获得 Radio。
11. Foundation 和 Chaos 随机交换 HCZ Elevator A/B。
12. 对实际分配结果做运行时计数校验。
13. 记录 Badge、角色、装备、传送和校验日志。
14. 回合结束恢复 Badge 并清理状态。

### 4.4 Badge 实现

Badge 现在使用 EXILED 的 Player.RankName，不再使用 CustomInfo，也不改变 Player.Group 或权限。

行为：

- 保存玩家原始 Badge 文本。
- 安保追加后缀（安保人员）。
- 混沌追加后缀（混沌渗透者）。
- 角色初始化后延迟 0.5 秒重新写入一次，防止原版角色初始化覆盖。
- 通过原始 Badge 计算，避免重复追加后缀。
- Round End / WaitingForPlayers 时恢复原始 Badge。

本轮日志中 Owner 的原始 Badge 是 SERVER OWNER，实际写入为：

~~~text
SERVER OWNER (混沌渗透者)
~~~

日志显示 Expected、Actual、Match=True，用户也已确认 Badge 测试正常。

### 4.5 Reinforcement 当前实现

ReinforcementManager 当前已经实现：

- Foundation / Chaos 两套 Support Score。
- ClassD 正常逃脱：Chaos +1。
- CuffedClassD：Foundation +1。
- Scientist 正常逃脱：Foundation +2。
- CuffedScientist：Chaos +2。
- 同一玩家每回合只计分一次。
- 第一正常波窗口 300 秒。
- 第一波无观察者时等待，390 秒仍无合格观察者则跳过。
- 第一波过滤 Overwatch，只允许正常 Spectator。
- 第一波及后续正常大波统一按 Support Score 比例选择阵营，0:0 时 50/50。
- 普通大波由插件在固定 5 分钟窗口调度，原版独立正常波次和 mini wave 均被拦截。
- 所有 mini wave 在选择、刷新和刷新完成阶段防御性取消。
- 正常波次使用固定 300 秒窗口，不因实际刷新延迟而漂移。
- 插件调用原生 `Respawn.ForceWave` 触发所选阵营，保留原版角色、装备和关系管线。
- 实际刷新完成后保留 25% Support Score。
- 支援周期和决策原因都有日志。

## 5. 已验证事实

### 5.1 构建

使用当前服务器实际 EXILED / SCP:SL 引用构建：

~~~text
dotnet build EmergencyEvents.csproj --no-restore -c Release -p:SL_REFERENCES=D:\PROGRA~2\Steam\STEAMA~1\common\SCPSEC~2\SCPSL_Data\Managed
~~~

结果：

~~~text
0 个警告
0 个错误
~~~

生成文件：

~~~text
D:\project\emergency-events\bin\Release\net48\EmergencyEvents.dll
~~~

### 5.2 服务器加载

本机服务器端口：7777。

最近一次重启后日志确认：

- EXILED 9.14.2 加载成功。
- EmergencyEvents@0.1.0 加载成功。
- EmergencyEvents enabled 成功。
- 服务器重新监听 UDP 7777。
- 未发现 EmergencyEvents 的加载错误。

本机最后一次服务器进程证据：旧 PID 27400 被结束，新服务器 PID 26336 监听 7777。这些 PID 只对当前电脑有效，换电脑不要照抄。

### 5.3 最近一次回合日志

最近一次实机回合的人口是 17：

~~~text
Population=17; Tier=E; Supported=True
Expected=17|E|3/2/2/7/3
RuntimeValidationPassed Actual=17|3/2/2/7/3
~~~

SCP 角色日志显示本局：

~~~text
Scp939Count=2
Roles=Scp939,Scp106,Scp939
~~~

本局还确认：

- Security 2 人、Chaos 2 人。
- 两边共用 KeycardGuard, SurfaceAccessPass, GunCOM18, Medkit。
- Security 额外 Radio。
- Foundation 被传送到 HCZ Elevator B。
- Chaos 被传送到 HCZ Elevator A。
- Owner 的 Badge 追加和延迟刷新都 Match=True。

当前回合启动日志还确认配置为：

~~~text
FirstWaveWindow=05:00
Deadline=06:30
NormalWaveInterval=05:00
MiniWaves=Disabled
ForceWave=false
CarryoverRatio=0.25
~~~

这条日志只能证明调度器按新配置启动，不能代替每个支援分支的完整实机验证。

## 6. 还没有完成的测试

### 6.1 Round Core

- 回合结束后 Badge 恢复原值，并在下一局不重复追加。
- 连续两局状态清理和 RoundId 隔离。
- 16、19、20、25、26、31、32、37、38、45 人口边界的真实服务器回合。
- 小于 16、大于 45 时保持原版开局而不是强行套表。
- 晚加入玩家不替代锁定开局名单。
- 3114 作为剩余 SCP 槽位的随机分支。
- 两边 HCZ A/B 交换在多局中出现两种方向。

### 6.2 Reinforcement

- 05:00 到达时有正常观察者，第一波实际刷新。
- Foundation 分数较高时选择 NTF。
- Chaos 分数较高时选择 CI。
- 平局时记录并执行 50/50 随机。
- 05:00 没有观察者、06:30 前出现观察者时继续等待并刷新。
- 直到 06:30 都没有观察者时跳过第一波。
- Overwatch 不进入第一波玩家列表。
- mini wave 被取消且不进入最终刷新。
- 正常波次未到 5 分钟时被阻止。
- 上一波完成后满 5 分钟才允许下一次正常波次。
- 实际刷新完成后的 25% 积分保留。
- 四种逃脱场景和重复计分保护。

### 6.3 尚未实现所以无法测试

- D-LRC 30 秒 Snapshot。
- Response Score 和 Control State。
- 0–5 响应等级。
- BIO、SYS、CON、SEC、GOI、WAR、END 危机判定。
- Event Director 候选筛选和 120 秒评估。
- O4 投票。
- 特殊人员事件和事件冷却。
- RA 命令、数据库、后台管理面板。

## 7. 换电脑后建议的第一轮工作

### 第一步：恢复环境

1. 确认新电脑安装 .NET SDK、SCP:SL Dedicated Server 和 EXILED 9.14.2。
2. 从实际服务器目录取得 SL_REFERENCES，不要凭记忆写路径。
3. 检查 EXILED 版本、SCP:SL 版本、Target Framework 和 C# 版本。
4. 运行 git remote -v，确认远程地址正确。
5. 先不要升级 EXILED；当前代码按 9.14.2 编译。

### 第二步：恢复并构建

新电脑首次构建可以使用：

~~~text
dotnet restore EmergencyEvents.csproj
dotnet build EmergencyEvents.csproj -c Release -p:SL_REFERENCES=<新电脑的 SCP:SL_Data\\Managed 路径>
~~~

要求：0 警告、0 错误。不要把本机旧的 bin/、obj/ 作为依赖带过去。

### 第三步：先完成 Reinforcement 实机验收

当前回合或下一局按照下面顺序做：

1. 准备至少一个非 Overwatch 的观察者。
2. 记录 RoundStarted 时间。
3. 在 05:00 附近检查第一波窗口。
4. 通过正常逃脱或被拘留逃脱制造可预期的 Foundation / Chaos 分数。
5. 检查 FirstWaveSelected、FirstWaveRespawning、RespawnedTeam 和 SupportCycleCompleted。
6. 观察 mini wave 是否出现 MiniWaveCancelled。
7. 在下一次原版正常波次前检查是否出现 NormalWaveHeld。
8. 支援周期完成后核对 DecaySupportScores 的前后分数。
9. 结束回合，检查 Cleanup 和 Badge 恢复。

### 第四步：再开始 D-LRC Evaluator

不要直接把 Evaluator、Crisis 和 Event Director 混在 Reinforcement 修复中。先定义独立的数据模型：

~~~text
RoundSnapshot
ResponseScoreResult
ControlState
ResponseLevel
~~~

先做纯逻辑和日志，再接入 30 秒运行时采集。每次接入都要保留当前 Round Core 和 Reinforcement 的行为。

## 8. 后续实施计划

### Phase 1：完成当前基础验收

- 完成 Reinforcement 第一波、mini wave、5 分钟门控、积分方向和 25% 保留的实机日志验证。
- 完成回合结束 Badge 恢复和连续两局验证。
- 决定是否保留 3114 为剩余 SCP 随机候选。

### Phase 2：D-LRC Evaluator

- 定义 Snapshot 数据结构。
- 实现有效力量、SCP 当量、观察者、核弹和上一波表现采集。
- 实现 Response Score。
- 实现 Control State。
- 实现响应等级约束。
- 从 06:31 开始每 30 秒运行并记录可审计日志。

### Phase 3：Crisis System

- 为每种危机定义触发条件、冷却、生命周期、取消条件和日志。
- 危机判定只负责开启事件池，不直接执行事件。
- 先实现 BIO/SYS，再扩展 CON/SEC/GOI/WAR/END。

### Phase 4：Event Director

- 每 120 秒生成一次评估。
- 从 DLRC、响应等级和 Crisis 选择合法事件池。
- 检查观察者数量、人员冷却、当前活动事件和资源成本。
- 生成候选后才交给 O4 投票。
- 事件执行、成功、失败、终止都要有独立日志。

### Phase 5：O4 和事件包

- 实现 O4 动态面板和投票。
- 先做一个完整 BIO 事件作为模板。
- 事件包逐个扩展，不要一次性创建大量空目录。

### Phase 6：发布和兼容性

- 清理调试日志策略。
- 写正式 README 和配置说明。
- 记录 EXILED / SCP:SL 版本兼容性。
- 生成可部署 DLL 包。
- 在真实测试服务器完成回放式验收后再发布。

## 9. 重要技术约束和决策

- 不要把项目根命名空间改成 DLRC；EmergencyEvents 是总插件，D-LRC 是其中一个内部系统。
- 不要用 CustomInfo 代替 Badge。Badge 使用 Player.RankName；权限使用 Player.Group，两者不能混淆。
- 不要通过修改权限组来实现临时称号。
- 不要调用 Respawn.ForceWave 加速原版支援时间。
- 不要自己重写原版胜利判定。
- 不要凭旧版 EXILED 记忆猜 API；先以当前 9.14.2 引用编译验证。
- 不要在第一版引入数据库。
- 不要因为用户要求“不要测试项目”就省略构建和服务器日志验证。
- 所有“完成”“通过”“正常”都必须对应构建输出、日志或用户实际回报。
- 当前 server-live.log 是本机临时运行记录，已被 .gitignore 排除，不是跨电脑的事实来源；跨电脑应以源代码、计划文档和新服务器日志为准。

## 10. 建议下一位开发者先读的文件

按顺序阅读：

1. 本文 handoff.md
2. docs/superpowers/specs/2026-08-22-round-core-design.md
3. docs/superpowers/plans/2026-08-22-reinforcement-system.md
4. docs/superpowers/plans/2026-08-23-five-minute-normal-reinforcement.md
5. Plugin.cs
6. Config.cs
7. RoundCore/RoundCoreManager.cs
8. Reinforcement/ReinforcementManager.cs

## 11. 下一会话建议使用的技能

- systematic-debugging：处理服务器实测中出现的异常角色、波次、积分或状态问题，先复现、定位证据，再修改。
- executing-plans：按已有的 Reinforcement、D-LRC 或 Crisis 计划逐项实施，避免把多个系统混在一个改动中。
- verification-before-completion：在声称完成、修复或通过前，重新构建并核对服务器日志和实际行为。
- handoff：再次更换开发环境或交给其他开发者时，更新本文而不是只依赖聊天记录。

当前用户明确要求不创建测试项目，因此下一会话默认继续使用构建验证、日志验证和真实服务器回合验证；除非用户明确改变这一要求，否则不要擅自引入测试工程。

继续工作前先运行：

~~~text
git status --short --branch
git remote -v
~~~

然后确认当前工作区是否已经完成首次 commit/push，再决定是继续开发、提交，还是先做服务器验收。
