# Emergency Events 开发文档

这套文档描述 `main` 当前稳定基线（commit `679c5c9`）的真实架构、公开契约和扩展边界。它面向维护者、未来的 Coding Agent 和 Event Pack 开发者，不是玩家宣传页。

## 模块状态

| 模块 | 当前状态 | 说明 |
| --- | --- | --- |
| M01 — Round Core | READY | 回合资格、人口锁定和生命周期边界。 |
| M02 — Reinforcement Integration | READY | 保留原版 Primary Wave，补充人数上限、Mini-Wave 策略和波次事实。 |
| M03 — D-LRC Evaluator | LOGIC READY / BALANCE PENDING | 纯逻辑和接口已实现，人口可达性与平衡仍需后续验证。 |
| M04 — Crisis System | READY | Active/Inactive 危机与 Episode 模型。 |
| M04.5 — Facility Disorder | LOGIC READY / BALANCE PENDING | FDI 结算和去重已实现，秩序恢复策略仍待设计。 |
| M05 — Event Director | FRAMEWORK READY / PRODUCTION EVENT PACK NOT STARTED | Director、资格判断和运行时安全边界已实现，正式事件未注册。 |
| M06 — O4 Panel | DEFERRED BY DESIGN | 当前不实现 UI、投票或 O4 选择器。 |

## 阅读顺序

- [Architecture](ARCHITECTURE.md)：模块调用链、状态所有权和边界。
- [API Reference](API_REFERENCE.md)：当前类、接口、字段和生命周期契约。
- [Event Pack Development](EVENT_PACK_DEVELOPMENT.md)：未来新增事件的步骤和禁止事项。
- [Runtime Contracts](RUNTIME_CONTRACTS.md)：回合状态、危机、FDI、波次和 Director 的运行规则。
- [Testing](TESTING.md)：自动化、RuntimeHarness、隔离服和真人验证的区别。
- [Roadmap](ROADMAP.md)：未决定事项和明确延期事项。

## 核心原则

1. Population Tier、D-LRC Response Level 和 Crisis Tag 是事件资格的三个独立维度。
2. Crisis 只有 Active/Inactive，不拥有独立 Severity。
3. M02 不重实现原版阵营、职业组成、装备、选择或出生流程。
4. M05 只消费 M01–M04.5 已发布事实，不重复计算上游状态。
5. Event Pack 负责执行，不能自行重算 D-LRC、Crisis、Population Tier、FDI 或资格。
6. 任何 provisional 行为都必须继续保持显式标记，不能写成已完成的正式规则。
