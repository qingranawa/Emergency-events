# Balance Telemetry

Balance Telemetry 是只读平衡数据收集器，不参与 D-LRC、FDI、Crisis、Respawn 或 Event Director 决策。它从正式 Evaluation、CrisisAssessment、FDI settlement 和 Primary Wave 事实生成单行 JSONL 记录，SchemaVersion 当前为 1。

输出写入插件进程目录下的 `telemetry/`，每日生成 `balance-YYYY-MM-DD.jsonl`，回合摘要写入 `round-summary-YYYY-MM-DD.jsonl`。写入失败只记录错误并继续 Gameplay；内存中的最近记录默认最多 2048 条，回合结束清理。

主要记录类型包括 `DLRC_EVALUATION`、`PRIMARY_WAVE`、`CRISIS_TRANSITION`、`FDI_SETTLEMENT` 和 `ROUND_BALANCE_SUMMARY`。Evaluation 字段直接来自官方结果，Telemetry 不重新调用评分器、危机判定器或 ControlState。

真实数据统计时，正常 `PERIODIC` 才作为等级时间线样本；`POST_MAJOR_WAVE` 与 `MANUAL_RA` 仍保留 Trigger，但不能伪装成额外 30 秒。未来加入等待样本时，只允许写入回合内匿名编号，不写 Nickname、SteamId、UserId、IP、DiscordId 或 AccountId。

D-LRC Phase 1 仍为 `NEEDS LIVE DATA BEFORE PRODUCTION TUNING`，Telemetry 基础设施可用不等于已经拥有真人数据。
