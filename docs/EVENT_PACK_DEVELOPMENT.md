# Event Pack Development

## 当前边界

M05 当前是 `FRAMEWORK READY / PRODUCTION EVENT PACK NOT STARTED`。`EventDefinition`、人口解析、资格判断、来源仲裁和生命周期已经可供未来 Event Pack 使用，但仓库当前没有正式注册的生产事件。

核心分工是：

```text
Director = Decision
Event Pack = Execution
```

Director 负责声明、筛选、选择和计划；Event Pack 负责实际职业组成、枪械、弹药、护甲、医疗、装备、出生和事件专属清理。

## 三维资格模型

每个事件由三个独立维度约束：

1. `PopulationTier`：E/D/C/B/A，决定使用哪一个人口 Profile。
2. `EventResponseLevel`：L0–L5，决定事件需要的 D-LRC 响应等级。
3. `CrisisTag`：BIO/SYS/CON/SEC/GOI/WAR/END，决定事件针对的 Active 危机。

Crisis 没有自己的 Severity。`RequiredResponseLevel=L4` 的 L4 是 D-LRC 要求，不是危机等级。

## 新事件开发步骤

### 1. 创建稳定标识

选择唯一 `EventId` 和 `DisplayName`，不要使用会随平衡调整改变的标识。

### 2. 声明分类和来源

设置 `Category`（Support 或 NonSupport）和 `Source`（Foundation、Chaos、Goi、ProfessionalCrisisResponse 或 Internal）。专业危机响应必须使用 `ProfessionalCrisisResponse`，并声明至少一个 `RequiredCrisisTag`。

### 3. 声明 D-LRC 要求

设置一个 `RequiredResponseLevel`。当前语义是候选的 FinalLevel 必须达到该等级；事件定义本身不能声明一组危机等级，也不能声明 `RequiredCrisisSeverity`。

### 4. 声明 Crisis Tags

`RequiredCrisisTags=[BIO,SYS]` 表示 BIO Active AND SYS Active。当前列表语义不是 OR；未来若需要 OR，必须增加显式模型并同步契约和测试。

### 5. 定义五份人口 Profile

每个 `EventDefinition` 都有 E、D、C、B、A 五份 `EventPopulationProfile`，不是五个不同事件。`EventPopulationProfile` 类型支持声明以下字段；当前 `EventDefinition` 默认构造这些 Profile 时使用 `AllowDownscale=true`，Composition/Loadout ID 为空，未来 Event Pack 接线时再提供具体配置：

- `TargetPersonnel`。
- `MinimumPersonnel`。
- `AllowDownscale`。
- `CompositionProfileId`。
- `LoadoutProfileId`。

Population Tier 在回合开始时锁定，当前可用合资格人员由 `IEventPopulationResolver` 解析最终 `Planned` 人数。锁定档位不会因为中途掉人而自动换档。

### 6. 实现 Event Pack execution

使用解析出的 `ResolvedEventPopulation` 和 Profile ID 执行事件。不要在执行器中重新读取玩家总数来推导 Population Tier，也不要重新判断 Crisis、D-LRC 或 FDI。

### 7. 实现 cleanup 和失败回滚

执行器必须能处理人员在 Candidate 与 Start 之间变化、FacilityState 变化、危机解除和 Round End。失败必须不产生 Event Cost、不消费专业响应、不伪造 ActualSpawnTime，也不能留下活动任务。

### 8. 注册定义

通过 `EventRegistry` 注册唯一的 `EventDefinition`。当前 Registry 会拒绝重复 ID，并保持确定的读取顺序。

### 9. 编写测试

至少覆盖逻辑资格、五档人口缩减、RequiredCrisisTags AND、Episode/ResponseLevel 去重、FacilityState 过滤、来源仲裁和生命周期失败路径。

### 10. 编写 RuntimeHarness 与隔离服测试

对涉及真实插件适配器的路径增加 RuntimeHarness probe，并在隔离服确认插件加载、无异常和状态清理。测试 Harness 不能代替真人验证。

## 文档示例（不注册）

下面只是接口形状示例，标记为 `DOCUMENTATION EXAMPLE ONLY`、`NOT A PRODUCTION EVENT`：

```text
EventId = TEST_BIO_L4_RESPONSE
RequiredResponseLevel = L4
Source = ProfessionalCrisisResponse
RequiredCrisisTags = [BIO]

PopulationProfiles:
  E: Target=3, Minimum=2, Composition=TEST_SMALL, Loadout=TEST_E
  D: Target=4, Minimum=2, Composition=TEST_SMALL, Loadout=TEST_D
  C: Target=5, Minimum=3, Composition=TEST_MEDIUM, Loadout=TEST_C
  B: Target=7, Minimum=4, Composition=TEST_LARGE, Loadout=TEST_B
  A: Target=9, Minimum=5, Composition=TEST_LARGE, Loadout=TEST_A
```

## 禁止事项

- 不要在 Event Pack 中重新计算 D-LRC、Crisis、Population Tier、FDI 或 Professional Eligibility。
- 不要把 Crisis Tag 写成带有自身 L3/L4/L5 的等级系统。
- 不要把 Locked Population Tier 当成“必须刷满”的人数目标。
- 不要让 NON_SUPPORT 读取 FDI。
- 不要让 O4 选择来源、召唤事件或阻止 Chaos/GOI。
- 不要绕过 `EventDirector` 直接消费 Event Cost 或专业响应。
- 不要在 M06 未实现时把 `HasO4Selector=false` 当作错误。

## 运行时竞态示例

若 Candidate 时 Available=6、Target=4、Minimum=3，Vanilla 波次在 Start 前消耗人员，导致 Available=2，则 Start 必须失败并 Rollback。此时不能消费专业响应、不能记录 Event Cost、不能安排 Event #2。

危机或等级同样需要重验证：Candidate 时 BIO Active/L4，Start 前 BIO 已解除或 FinalLevel 降到 L3，则该 L4 BIO 事件必须 Abort。
