# Roadmap

以下事项是当前明确的 `PENDING DESIGN` 或 `DEFERRED BY DESIGN`，不是已实现规则。不要在没有规格变更的情况下自行补齐最终策略。

## PENDING DESIGN

1. D-LRC population-tier reachability。
2. Small Primary Wave failure volatility。
3. Foundation=0 时的 Collapse semantics。
4. FDI Order Recovery（MODEL D 已实现；默认参数仍需真人数据校准）。
5. Low Population Debounce。
6. Director Cadence。
7. Event Intensity、ExclusiveGroup 和 overlap rules。
8. GOI Alignment 与正式 runtime source。
9. Crisis Episode Resolve Debounce。
10. Formal Event Pack 的事件内容、执行器和生产配置。

### D-LRC 平衡

当前阈值服务于逻辑和接口验证，不应视为最终平衡。后续需要独立验证人口档位可达性、L3/L4/L5 分布、小波波动和 Foundation=0 的 Collapse 语义。

### FDI Order Recovery

当前 FDI 保持增量结算模型，并增加可配置的 MODEL D 被动恢复；Recovery 参数仍需真人数据校准，不能据此修改 D-LRC 阈值或评分公式。

### 低人口防抖

当前低人口判断是单次运行时观察；低于最低人数后本回合不可逆暂停。恢复人数后的防抖时长仍为 PENDING DESIGN。

### Director Cadence

M05 Scheduler 与 M03 30 秒 PERIODIC 独立。正式 Director Cadence 尚未确定，当前默认不自动创建生产事件周期。

### GOI Alignment

GOI Detector 使用 `HostileThirdPartyActive`、战斗人数、D-LRC FinalLevel 和 Foundation disadvantage，但正式 GOI runtime provider 仍是 `INTERFACE READY / PROVISIONAL`。GOI Crisis 不等于 GOI Source Event 自动合法。

## M06 O4 Panel

M06 核心面板与二选一选择层已实现：动态 Spectator/Overwatch 资格、单 Hint、临时真实客户端 `o4vote` 输入、M05 shortlist/TIE/NO_O4 边界、有界会话和生命周期清理。它不包含 GUI/canvas、正式 Event Pack 或对事件执行器的控制。

仍待真人验证：真实客户端 Hint 视觉位置与刷新体验、客户端命令可达性、观察者断线/复活期间的投票资格变化、动态加入的真实角色转换、投票数量呈现、VoteDuration/HintDuration 调优，以及实服日志中的选择/跳过生命周期。VoteDuration 默认 20 秒、Refresh 默认 1 秒、Hint 默认 1.3 秒均为 provisional。M05 的 `HasO4Selector=false` 在 M06 禁用、低人口暂停或运行时不可用时仍是合法状态；O4-required SUPPORT 没有合法 O4 时跳过当前支援机会。

## 设计变更规则

任何未来变更都必须同步更新架构、API、运行时契约、测试和本路线图，并明确区分现行实现与 provisional 方案。Event Pack、M06 和平衡调整不能通过文档先行伪装成已完成能力。
