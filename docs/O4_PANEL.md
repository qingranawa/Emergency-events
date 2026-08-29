# M06 — O4 Command Panel

## 目的与边界

M06 为在线 `Spectator` 和 `Overwatch` 提供一个服务器端单 Hint 面板，并在 M05 已经确定为 Foundation 多候选的普通 SUPPORT 选择时提供二选一投票。它只消费已完成的 M01–M05 事实，不重算 D-LRC、危机、FDI、Primary Wave 或 Director 候选。

M06 不创建事件、不会改变 Director cadence、不会安排 Event #2，也不会消费 Professional Response。Production Event Definitions 仍为 0；正式 Event Pack 不属于本模块。

## EXILED 9.14.2 运行时适配

展示使用 EXILED 9.14.2 的 `Player.ShowHint(string, float)`。当前目标 API 不提供可验证的 Hint anchor/vertical-offset 接口，因此没有伪造位置配置，默认由客户端 Hint 通道展示。

玩家投票使用真实客户端命令通道 `ClientCommandHandler`：

```text
o4vote 1
o4vote 2
```

别名为 `eevote`。它不是 RemoteAdmin 命令，也不能由 RA 代替客户端投票。

## O4 资格与隐私

资格条件是在线且当前为 `Spectator` 或 `Overwatch`。存活玩家、断开连接者和会话开始后才加入的观察者不能参与该会话。投票结算会再次根据当前资格过滤票数。

运行时只分配本回合局部 `O4-01`、`O4-02` 这类编号用于日志；不写玩家昵称、Steam ID、User ID 或 Account ID。Round End、Restart、WaitingForPlayers、Plugin Disable 与低人口暂停都会清空映射和会话。

## 普通面板

默认每秒向当前合法 O4 写入同一个 Hint，默认内容为：

```text
O4 COMMAND
DLRC-C4-BIO · 响应 L4 · 失控
FDI 中 · 危机 BIO
下次评估 00:27
```

`O4PanelConfig` 支持启用开关、刷新/Hint 时长、是否显示 FDI、危机、下次评估和控制状态。面板仅显示正式 D-LRC Code、最终响应等级、中文 ControlState、FDI 档位、活动危机标签及 M03 的下一次已排程评估时间；不显示 Raw Score、Response Breakdown、Crisis Severity 或内部候选评分。

## M05 选择边界

M05 先完成候选资格、专业响应优先级、来源仲裁与 fallback。只有以下同时成立时，M05 才调用 M06：

- Foundation 普通 SUPPORT 有多个合法候选；
- M05 的 `O4SelectionRequired=true`；
- M05 的有序 shortlist 有两个候选；
- 当前 M06 Selector 可用。

M06 不生成、重排或过滤候选，只展示该 shortlist。Chaos、GOI、专业响应和 NON_SUPPORT 不进入 O4 选择。M05 始终保留自己的确定性 fallback。

投票会话绑定 `RoundId + CycleId + SessionId`，默认 20 秒。多数票产生显式 winner；平票、零票、无 O4、取消、过期、失效候选或 Selector 不可用都回退 M05 原选择。stale callback 被忽略。M05 在 `TryStart` 和 Commit 前仍使用最新 Context 重验证人员、危机、D-LRC、人口计划和 Professional eligibility。

## 生命周期和资源上限

每次最多一个活动投票会话。会话超时通过 MEC `Timing.CallDelayed` 管理；Round End、Restart、WaitingForPlayers、LOW_POPULATION_SUSPENDED、Plugin Disable 和 Runtime Cleanup 会实际 Kill 该回调并取消会话。清理不会产生第二次 callback。

最近选择结果是环形历史，默认最多 256 条，超过上限淘汰最旧条目。活动会话与局部 O4 映射均在回合清理时清空。普通 Hint 刷新只有一个 MEC 回调，暂停或清理后停止。

## 管理、日志与探针

RemoteAdmin 只提供只读状态：

```text
ee o4 status
```

它输出配置、面板运行状态、合法 O4 数量、当前会话/周期、票数、剩余秒数和玩家输入路径，不提供 RA 投票。

Console 详细日志使用 `[EmergencyEvents][O4]` 前缀，关键动作包括 `O4_PANEL_STARTED`、`O4_SELECTION_REQUESTED`、`O4_VOTE_CAST`、`O4_SELECTION_RESOLVED`、`O4_SELECTION_FALLBACK`、`O4_SELECTION_CANCELLED` 和 `O4_PANEL_STOPPED`。

隔离服 dry-run 命令为：

```text
o4_panel_runtime_probe
o4_selection_runtime_probe
```

两者使用合成的只读面板/投票事实，明确输出 `ProductionDefinitions=0`；它们能够验证已加载 DLL 的适配层和会话链路，但不能替代真实客户端观看 Hint、输入命令、断线、复活或投票的真人验证。

## 验证状态

M06 的纯逻辑与 M05 边界测试覆盖候选限制、资格快照、改票、平票/零票/无 O4 fallback、stale 结果、cleanup、最终重验证、资源上限和 1000 次长运行。隔离服启动及真实客户端投票/Hint 呈现仍须单独报告；未运行时状态应标记为 `PENDING`，不能由单元测试代替。
