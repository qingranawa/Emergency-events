# Testing

## 测试层级

| 层级 | 能证明什么 | 不能单独证明什么 |
| --- | --- | --- |
| Logic / Unit Tests | 纯函数、契约、边界、去重和生命周期。 | EXILED 真实事件是否一定触发。 |
| Deterministic Simulation | 939 随机候选、权重、人口和长运行容器边界。 | 真人行为与服务器钩子。 |
| RuntimeHarness | 已加载插件内的 Runtime Adapter、Director、FDI 生产链路。 | 真人击杀、真实玩家选择和最终平衡。 |
| Isolated Server Runtime | DLL 加载、LocalAdmin/服务器日志、RuntimeHarness 命令和异常情况。 | RemoteAdmin 客户端体验和真人回合平衡。 |
| Live Player Validation | 真实客户端、真实玩家、真实 EXILED 事件钩子和视觉结果。 | 不能被自动化测试替代。 |

## 当前基线

这是 commit `679c5c9` 的文档基线，不是永久 API 合同：

- M01：3/3。
- M02：25/25。
- M03：43/43。
- M04：30/30。
- FDI：28/28。
- M05：46/46。
- Total：175/175 PASS。
- Release Build：PASS，Warnings=0，Errors=0。
- Isolated Runtime：PASS。
- Live Player Validation：PENDING。

M01 还包含 10,000 次 SCP-939 模拟：Double939Count=1042，TotalScpPerRound 始终为 3。

## 常用命令

在仓库根目录执行：

```powershell
dotnet clean .\EmergencyEvents.csproj -c Release
dotnet clean .\RuntimeHarness\RuntimeHarness.csproj -c Release
dotnet clean .\Evaluation.Tests\Evaluation.Tests.csproj -c Release
dotnet restore .\EmergencyEvents.csproj
dotnet restore .\RuntimeHarness\RuntimeHarness.csproj
dotnet restore .\Evaluation.Tests\Evaluation.Tests.csproj
# 在当前 shell 中将 SL_REFERENCES 设置为本机 SCP:SL 的 Managed 引用目录
dotnet build .\EmergencyEvents.csproj -c Release
dotnet build .\RuntimeHarness\RuntimeHarness.csproj -c Release
dotnet build .\Evaluation.Tests\Evaluation.Tests.csproj -c Release
dotnet run --project .\Evaluation.Tests\Evaluation.Tests.csproj -c Release --no-build
```

构建需要真实的 SCP:SL Managed 引用；请在当前 shell 中将 `SL_REFERENCES` 指向本机的该目录。不要把 Clean 后缺少本地程序集误判为 Gameplay 回归。

## RA 命令

根命令注册为 `EmergencyEvents`，别名为 `ee`，执行通道是 RemoteAdmin，不是 LocalAdmin 游戏控制台。当前语法包括：

```text
ee status
ee health
ee modules
ee module <round|reinforcement|dlrc|crisis|disorder|fdi>
ee round state
ee wave state|current|last|previous|timers|cap|survival
ee wave history [count]
ee wave history <waveId> detail
ee dlrc state|evaluate|stage|breakdown|control|snapshot
ee dlrc stage full|raw
ee dlrc history [count]
ee crisis state|list|<tag>|check <tag>
ee disorder state|events|explain|history [count]
ee fdi state|events|explain|history [count]
```

`ee test ...` 只用于受权限保护的 dry-run/验证入口，不能把测试命令当作正式 Event Pack。

LocalAdmin 出现 `Command ee does not exist!` 通常表示命令发到了错误通道；它不能证明 RemoteAdmin parser 失败。

## RuntimeHarness

当前已存在：

- `DIRECTOR_RUNTIME_PROBE`：验证 Scheduler/Context 解耦、显式周期、Event #2 DueAt、清理、低人口暂停、随机来源和生命周期边界。
- `FDI_RUNTIME_PROBE`：验证事件生产、06:31 存量去重、30 秒增量、079/SYS、WAR/END 去重、无效评估恢复、长运行和清理。

RuntimeHarness 默认用于隔离验证，不代表真人 Gameplay。Probe 注入的是可控事实，不能证明真实 SCP 玩家击杀真实 MTF 时 EXILED death hook 一定会生成相同 DisorderEvent。

## 新增测试要求

涉及新契约时，至少补充：

1. 正常输入和边界输入。
2. Invalid Evaluation、RoundId mismatch 和空事实的拒绝路径。
3. Round End、Restart、WaitingForPlayers、LOW_POPULATION_SUSPENDED 的清理。
4. 重复事件、重复 Commit、重复 Episode 响应和有界容器。
5. 真实 DLL 的隔离服加载与日志检查（若改动 Runtime Adapter）。

自动化结果、构建结果、RuntimeHarness 结果和真人验证必须分开报告，不得用 175/175 PASS 声称真人平衡已完成。
