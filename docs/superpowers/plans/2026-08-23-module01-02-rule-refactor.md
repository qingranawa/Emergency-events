# Module 01/02 规则重构计划

## 范围

本次只处理 Round Core 与 Reinforcement System，不开始 Module 03 D-LRC 新功能，也不实现 BIO、SYS、CON、SEC、GOI、WAR、END、Event Director 或 O4。

## 已完成的规则调整

- 普通大型支援由插件独占调度，原版独立正常波次和 mini wave 在事件边界拦截。
- 大型支援使用 05:00、10:00、15:00 的固定窗口，空窗口跳过但不漂移。
- 第一波等待观察者到 06:30，第一波阵营也使用全回合 Support Score 比例。
- 每次实际成功大型支援后才按 25% 和 AwayFromZero 规则衰减积分。
- 晚加入且角色为 `None` 的 dummy 纳入当前候选池，开局人口编制仍保持锁定。
- Support Score 账本覆盖主要 SCP 死亡、10% 伤害阈值、自然 SCP 物品和消耗品实例去重，并排除 SCP-914 产物。
- Round Core 的 SCP 角色池只固定一只 SCP-939，剩余槽位从原角色池随机选择。

## 验证要求

1. `Evaluation.Tests` 全部通过。
2. 使用 `.test-server\SCPSL_Data\Managed` 的真实 EXILED/SCP:SL 引用构建为 0 警告、0 错误。
3. 隔离服务器日志确认插件加载、原版波次拦截和插件波次请求日志。
4. 实机回合确认 05:00、10:00 固定窗口、晚加入 dummy、mini wave 禁止、Support Score 选择和单 SCP-939。
