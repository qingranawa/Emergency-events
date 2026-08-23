# Round Core and Reinforcement Regression Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复安保/混沌死亡后 Badge 不清除，以及首波无普通观察者在 06:30 后仍可能迟到刷新的两个已确认缺陷。

**Architecture:** 将 Badge 原始值保存与取回封装为无游戏依赖的 `BadgeRegistry`，由 `RoundCoreManager` 使用死亡事件携带的 `Player` 对象恢复并移除对应玩家的管理状态。将首波截止判断和跳过后的下一次普通波次时间封装为无游戏依赖的 `FirstWavePolicy`，由 `ReinforcementManager` 在 `Requested` 状态继续监控截止时间，并在选择/实际 Respawn 两个边界都阻止跳过后的过早波次。

**Tech Stack:** C# 12、.NET Framework 4.8、EXILED 9.14.2、SCP: Secret Laboratory 14.2.7、现有手写 Console 测试项目。

**Spec:** `handoff.md` 中的 Round Core Badge 生命周期和普通支援首波截止规则，以及本回合实机日志确认的两个回归缺陷。

## Global Constraints

- 不实现第四模块 Crisis System、Event Director、O4 或特殊事件。
- 保留 D-LRC Evaluator 当前行为，只修复 Round Core / Reinforcement 边界。
- 首波截止时间默认仍为 390 秒，普通波次间隔默认仍为 300 秒。
- 不调用 `Respawn.ForceWave`，继续使用原生 EXILED Respawn 事件。
- 使用现有隔离测试服务器验证，不操作用户客户端进程。
- 不执行 `git commit` 或 `git push`。

---

### Task 1: Add regression tests for the two policies

**Files:**
- Modify: `Evaluation.Tests/Program.cs`
- Modify: `Evaluation.Tests/Evaluation.Tests.csproj`
- Create: `RoundCore/BadgeRegistry.cs`
- Create: `Reinforcement/FirstWavePolicy.cs`

**Interfaces:**
- `BadgeRegistry.Remember(int, string?)`, `BadgeRegistry.TryGet(int, out string?)`, `BadgeRegistry.Remove(int)` and `BadgeRegistry.Snapshot()` manage original Badge values.
- `FirstWavePolicy.ShouldSkip(bool, float, float, int)` determines whether the pending first wave must be skipped.
- `FirstWavePolicy.GetNextNormalWaveDueAfterSkip(float, float)` returns the first legal normal-wave time after a skip.

- [ ] **Step 1: Write the failing tests**

Add two named tests to the existing manual test list:

```csharp
("死亡清除 Badge 后不再保留旧玩家映射", BadgeRegistryRemovesBadgeAfterDeath),
("首波截止无人时跳过并推迟下一次普通波次", FirstWavePolicySkipsAtDeadline),
```

The first test must remember `Dummy`, retrieve it as the restoration value, remove the player, and assert a second retrieval fails. The second test must assert `(pending=true, elapsed=390, deadline=390, observers=0)` skips, one observer does not skip, and the next due time is `690` seconds for a 300-second interval.

- [ ] **Step 2: Run the tests to verify RED**

Run:

```powershell
dotnet run --project Evaluation.Tests\Evaluation.Tests.csproj -c Release --no-restore --nologo
```

Expected: compilation fails because the new production policy types do not exist yet, proving the regression tests target new behavior.

- [ ] **Step 3: Add the minimal pure policy types**

Implement only the registry and first-wave calculations described above, then include both files in `Evaluation.Tests.csproj` so the tests exercise the same source files as the production project.

- [ ] **Step 4: Run the focused tests to verify GREEN**

Run the same command and require both new tests to pass together with the existing suite.

---

### Task 2: Restore managed Badge on death

**Files:**
- Modify: `RoundCore/RoundCoreManager.cs:30,331-396,552-574`
- Modify: `Plugin.cs:122-126`

**Interfaces:**
- `RoundCoreManager.HandlePlayerDied(Player player)` restores the stored original Badge for a managed player and removes that player from the registry after a successful write.

- [ ] **Step 1: Add the failing integration-facing assertion**

Use the new registry test as the pure lifecycle guard, then wire the production death boundary so the live log can emit `BadgeClearedOnDeath`.

- [ ] **Step 2: Implement the minimal death handler**

Call `roundCoreManager?.HandlePlayerDied(ev.Player)` from `Plugin.OnPlayerDied`, reuse the existing restoration write path, keep failed writes in the registry for round cleanup retry, and log the player ID plus restored Badge.

- [ ] **Step 3: Run the complete pure suite**

Require the existing tests and both regression tests to pass before building the plugin.

---

### Task 3: Close the first-wave deadline race

**Files:**
- Modify: `Reinforcement/ReinforcementManager.cs:142-314,590-638`

**Interfaces:**
- The manager continues its deadline monitor while `FirstWaveState.Requested` is pending.
- At or after the deadline with zero eligible non-Overwatch observers, it sets `Skipped`, clears pending first-wave selection state, and sets `NextNormalWaveDueSeconds` to deadline plus the normal interval.
- `HandleRespawningTeam` rejects a normal wave that arrives before that new due time, preventing a delayed native event from bypassing the skip.

- [ ] **Step 1: Run the pure first-wave regression test in RED/GREEN context**

Confirm the policy test fails before the policy implementation and passes after it, then use it as the guard for the manager wiring.

- [ ] **Step 2: Implement the deadline and respawn guards**

Use `FirstWavePolicy.ShouldSkip` and `GetNextNormalWaveDueAfterSkip`; preserve the existing 5-minute gate for later accepted waves and emit `FirstWaveDeadlineReached` with `Decision=SKIP_FIRST_WAVE` and `NextNormalWaveDue`.

- [ ] **Step 3: Run pure tests and Release build**

Run the complete test command and:

```powershell
$env:SCPSL_SERVER_PATH = (Resolve-Path '.test-server').Path
dotnet build .\EmergencyEvents.csproj -c Release --no-restore --nologo
```

Require 0 failures, 0 warnings, and 0 errors.

---

### Task 4: Deploy and perform live regression acceptance

**Files:**
- Generated: `bin\Release\net48\EmergencyEvents.dll`
- Runtime log: `.test-server\AppData\LocalAdminLogs\7777\*.txt`

- [x] **Step 1: Restart only the isolated LocalAdmin server**

Stop the exact `.test-server\LocalAdmin.exe` session after preserving its current log, start the rebuilt server with `--acceptEULA --skipHomeCheck --noSetCursor --noTerminalTitle`, and verify the plugin loads.

- [x] **Step 2: Reproduce Badge death behavior**

Start a 16-dummy round, verify Security and Chaos Badge application, kill one Security and one Chaos player, and require two `BadgeClearedOnDeath` records plus visible Badge removal.

- [x] **Step 3: Reproduce first-wave no-observer behavior**

Start with only Overwatch as the human client, keep all dummies alive through 06:30, require `FirstWaveDeadlineReached; Decision=SKIP_FIRST_WAVE`, then verify a dummy killed after 06:30 is not immediately respawned before `NextNormalWaveDue`.

- [x] **Step 4: Recheck D-LRC regression safety**

Require at least two post-start D-LRC snapshots, no `EvaluationFailed`, unchanged Round Core composition, and no Module 4 log or code changes.

---

### Task 5: Fresh verification and report

- [x] **Step 1: Inspect the final diff and test output**
- [x] **Step 2: Re-run the complete pure suite and Release build**
- [x] **Step 3: Report pass, fail, and not-run items separately**
