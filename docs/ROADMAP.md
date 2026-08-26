# Roadmap

以下事项是当前明确的 `PENDING DESIGN` 或 `DEFERRED BY DESIGN`，不是已实现规则。不要在没有规格变更的情况下自行补齐最终策略。

## PENDING DESIGN

1. D-LRC population-tier reachability。
2. Small Primary Wave failure volatility。
3. Foundation=0 时的 Collapse semantics。
4. FDI Order Recovery。
5. Low Population Debounce。
6. Director Cadence。
7. Event Intensity、ExclusiveGroup 和 overlap rules。
8. GOI Alignment 与正式 runtime source。
9. Crisis Episode Resolve Debounce。
10. Formal Event Pack 的事件内容、执行器和生产配置。

### D-LRC 平衡

当前阈值服务于逻辑和接口验证，不应视为最终平衡。后续需要独立验证人口档位可达性、L3/L4/L5 分布、小波波动和 Foundation=0 的 Collapse 语义。

### FDI Order Recovery

当前 FDI 是增量结算模型，没有最终的被动恢复规则。不能把“每分钟 -1”或“每 30 秒 -2”写成现行契约。

### 低人口防抖

当前低人口判断是单次运行时观察；低于最低人数后本回合不可逆暂停。恢复人数后的防抖时长仍为 PENDING DESIGN。

### Director Cadence

M05 Scheduler 与 M03 30 秒 PERIODIC 独立。正式 Director Cadence 尚未确定，当前默认不自动创建生产事件周期。

### GOI Alignment

GOI Detector 使用 `HostileThirdPartyActive`、战斗人数、D-LRC FinalLevel 和 Foundation disadvantage，但正式 GOI runtime provider 仍是 `INTERFACE READY / PROVISIONAL`。GOI Crisis 不等于 GOI Source Event 自动合法。

## DEFERRED BY DESIGN: M06 O4 Panel

M06 是 O4 Panel，当前不实现 HUD、Panel、Voting UI、Player eligibility、Interaction 或 Observer UX。M05 的 `HasO4Selector=false` 是合法系统状态；Foundation 多候选时必须有 fallback，O4 不选择来源、不召唤事件、不阻止 Chaos/GOI。

## 设计变更规则

任何未来变更都必须同步更新架构、API、运行时契约、测试和本路线图，并明确区分现行实现与 provisional 方案。Event Pack、M06 和平衡调整不能通过文档先行伪装成已完成能力。
