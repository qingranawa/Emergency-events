# Primary Wave Timer Extension 60/15 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在每次成功完成原版 Foundation/MTF 或 Chaos/CI Primary Wave 后，等待原版计时器重置与人数重算，再对刷新方增加 60 秒、对方增加 15 秒，并保留所有既有 Module 01–03 行为。

**Architecture:** 用纯逻辑策略把刷新方/对方配置映射为 Foundation/Chaos 两个独立增量，并用 `MajorWaveRecord` 的 WaveId guard 保证单波只处理一次。运行时先在 EXILED `RespawnedTeam` 记录波次事实，再绑定 SCP:SL 14.2.7 原生 `Respawning.WaveManager.OnWaveSpawned` 边界；该事件在 `SpawnableWaveBase.OnWaveSpawned` 重置和 `TimeBasedWave.OnAnyWaveSpawned` 人数重算之后触发。插件随后读取 `TimedWave.Timer.TimeLeft`，只修改 `SpawnIntervalSeconds`，发送原版 Timer 更新消息；不重置、暂停、同步或重建任一计时器。处理同时输出波次前、原版重算后和插件增量后的关键快照，再发布既有 `POST_MAJOR_WAVE`。

**Tech Stack:** C# 12、.NET Framework 4.8、EXILED 9.14.2、SCP:SL 14.2.7、MEC、现有 Evaluation.Tests net8 控制台测试。

**Spec:** 用户于 2026-08-24 粘贴的最新版 Module 02 Timer Extension 60/15 方案。

## Global Constraints

- 保留原版 Primary Wave、Influence、Respawn Token、MTF/CI 阵营决定、玩家选择、职业、装备和出生流程。
- Mini-Wave 继续禁用，Primary Wave 人数上限继续使用 E6 / D6 / C8 / B14 / A18。
- Timer Extension 只在 Primary、完成、实际刷新人数大于 0 时生效；Mini-Wave、取消、失败、回滚、特殊事件和非 NTF/Chaos 阵营不生效。
- 配置为 `SpawningFactionTimerExtensionSeconds` 与 `OpposingFactionTimerExtensionSeconds`，默认 60/15，0 独立禁用，合法范围 0–300，非法值 WARN 并分别回退默认值。
- 每个 WaveId 只应用一次 Timer Extension，重复回调必须跳过并记录 DEBUG/WARN。
- 只读取原版计算后的实际 `Timer.TimeLeft`，不假设 5:30、7:30 或其他固定基准，不维护跨波次累计 bonus。
- 不暂停、同步、重置、重建对方计时器，不把 Timer Extension 做成硬冷却，不重复实现原版 Influence 或人数相关计时调整。
- 不改变 `POST_MAJOR_WAVE` 一次性语义和 Module 03 原有 30 秒周期，不进入 Module 04，不实现特殊事件。
- 不提交、不推送代码，不操作用户客户端。

---

### Task 1: Replace the pure timer policy with 60/15 mapping

**Files:**
- Modify: `Reinforcement/PrimaryWaveTimerExtensionPolicy.cs`
- Modify: `Evaluation.Tests/Program.cs`
- Modify: `Evaluation.Tests/Evaluation.Tests.csproj` only if the policy link is missing

**Interfaces:**
- `DefaultSpawningFactionSeconds = 60` and `DefaultOpposingFactionSeconds = 15`.
- `NormalizeConfiguredSeconds(int configuredSeconds, int fallbackSeconds)` returns the configured value for 0–300, otherwise the supplied safe default.
- `TryGetExtensions(string? waveFaction, int spawningSeconds, int opposingSeconds, out int foundationSeconds, out int chaosSeconds)` maps NTF to Foundation=spawning/Chaos=opposing and Chaos to the reverse.
- Existing primary-faction, actual-player, addition and success predicates remain pure and stateless.

- [x] **Step 1: Add failing tests first**

Replace the old “both timers +60” assertions and add tests for Foundation `+60/+15`, Chaos `+15/+60`, dynamic values 450/287→510/302 and 330/421→390/436, independent zero disabling, 0–300 validation, no hidden accumulation, and existing abnormal-wave/duplicate guards.

- [x] **Step 2: Run Module 02 and confirm the new assertions fail**

Run:

```powershell
dotnet run --project Evaluation.Tests\Evaluation.Tests.csproj --no-restore -c Release M02
```

Expected: the old one-value policy cannot satisfy the new 60/15 mapping and the run fails only on the newly changed timer assertions.

- [x] **Step 3: Implement the minimal stateless mapping**

Implement the two defaults, per-field normalization, faction-to-timer mapping, and `AddExtensionSeconds` without storing any cumulative bonus.

- [x] **Step 4: Run Module 02 again**

Require all Module 02 tests, including the new dynamic and zero-disable cases, to pass before changing runtime code.

### Task 2: Add independent configuration and pending pre-wave snapshot

**Files:**
- Modify: `Config.cs`
- Modify: `Reinforcement/ReinforcementManager.cs`
- Modify: `Reinforcement/ReinforcementState.cs`
- Modify: `Reinforcement/MajorWaveHistory.cs` only if the existing WaveId guard needs adjustment

**Interfaces:**
- Config exposes `SpawningFactionTimerExtensionSeconds` and `OpposingFactionTimerExtensionSeconds`.
- Round state stores nullable Foundation/Chaos timer values captured immediately before a Primary Wave begins, and clears them with pending-wave state.
- `MajorWaveRecord.TryMarkTimerExtensionProcessed()` remains the only per-wave application guard.

- [x] **Step 1: Normalize both configurations at round start**

Use independent fallback defaults and log configured/effective values, without touching native timers, tokens, Influence or caps.

- [x] **Step 2: Capture the pre-wave timer snapshot**

At the allowed `RespawningTeam` boundary, read both actual non-mini primary timers if available and store nullable values for diagnostics only; do not modify them.

- [x] **Step 3: Run Module 02 after configuration/state changes**

Confirm no existing Module 02 regression before changing the timer write path.

### Task 3: Apply 60/15 after vanilla reset and emit diagnostic logs

**Files:**
- Modify: `Reinforcement/ReinforcementManager.cs`

**Interfaces:**
- The native `Respawning.WaveManager.OnWaveSpawned` callback is the ordering boundary after `SpawnableWaveBase.OnWaveSpawned` and `TimeBasedWave.OnAnyWaveSpawned` finish.
- It reads both current timers as `BeforeExtension`/`AfterVanillaReset`, maps extensions by the completed wave faction, writes only `SpawnIntervalSeconds`, sends native `UpdateMessageFlags.Timer`, then reads the after values.
- Success logs include `WaveId`, `WaveFaction`, `ActualSpawnedCount`, `VanillaResetDetected`, `BeforeWave`, `BeforeExtension`, `AfterVanillaReset`, both extensions, both after values, `AppliedAt`, `WaveCompletedAt`, `DelayAfterWaveCompletionMs`, and `Applied=true`.
- Timer unavailability, disabled values, duplicate callback, Mini-Wave, zero spawn and non-primary cases log `Applied=false` with explicit reason and still preserve one `POST_MAJOR_WAVE`.

- [x] **Step 1: Write a focused regression test for non-refreshing timer accumulation**

Use two consecutive Foundation waves with arbitrary timer values and assert the computed plan is 60/15 per wave, not a persistent `ChaosExtraDelay += 60` stack; the test must fail against the current both-60 policy.

- [x] **Step 2: Run the focused Module 02 test and observe the expected failure**

Run the M02 command and verify the failure identifies the old opposing-timer behavior.

- [x] **Step 3: Implement the single runtime change**

Replace the unconditional two-sided +60 writes with the policy’s per-faction 60/15 values, use the native `WaveManager.OnWaveSpawned` ordering boundary, preserve one-time WaveId and POST guards, and add the requested snapshot fields.

- [x] **Step 4: Run Module 02 again**

Require the focused regression and all existing Module 02 tests to pass.

### Task 4: Full regression, build and isolated-server verification

**Files:**
- No additional source files beyond Tasks 1–3.

- [x] **Step 1: Run fresh Module 01, Module 02, Module 03 and ALL tests**

Require Module 03 to remain 43/43 and report the exact new total.

- [x] **Step 2: Run a fresh Release build with real SCP:SL references**

Run:

```powershell
dotnet build EmergencyEvents.csproj --no-restore -c Release -p:SL_REFERENCES='D:\Program Files\steam\steamapps\common\SCP Secret Laboratory Dedicated Server\SCPSL_Data\Managed'
```

Require exit code 0 and the complete warning/error counts.

- [x] **Step 3: Deploy only the verified DLL to the isolated server**

Compare source/destination hashes, copy only `EmergencyEvents.dll`, and do not stop or manipulate the client process.

- [x] **Step 4: Read the isolated server log**

Confirm the new two-field config is deployed, the plugin enables, and record at least one real Foundation and one real Chaos Primary Wave for the requested before/reset/extension/after evidence when the live session covers them.

- [ ] **Step 5: Report and stop**

Report the root cause of the old opposing-timer growth, new 60/15 mapping, API, next-frame ordering, live wave evidence, whether 7:30→5:30 is confirmed as native behavior, all automated results, build/load results and remaining live validation gaps. Explicitly state that Module 04 was not entered and no commit/push occurred.
