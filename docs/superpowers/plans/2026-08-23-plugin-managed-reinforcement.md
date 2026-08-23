# Plugin-Managed Reinforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让插件从回合 05:00 基准主动管理正常大波刷新，接纳中途加入的 dummy，保留 Support Score，并完全拦截原版波次与小波。

**Architecture:** `ReinforcementManager` 保留现有 Support Score、开局锁定编制和原生实际生成流程，只把波次触发权收回插件。每秒调度器在 `NextNormalWaveDueSeconds` 到达且存在合格候选人时调用 EXILED 的 `Respawn.ForceWave`，事件边界只允许由插件发起的正常大波继续；候选池同时接受 `Spectator` 和已连接、非 Overwatch 的 `None` 角色 dummy。

**Tech Stack:** C# 12、.NET Framework 4.8、EXILED 9.14.2、SCP:SL 14.2.7、现有 .NET 8 手写测试运行器。

**Spec:** `handoff.md` 的 Reinforcement 验收矩阵与用户本次要求。

## Global Constraints

- Round Core 的开局人口编制继续只读取回合开始人口，不因晚加入 dummy 改变。
- Support Score 继续累计、按票数选择阵营并在成功大波后按 25% 四舍五入保留。
- 插件不进入 Module 4，不实现 Crisis、Event Director、O4 或危机标签。
- 小波永远不得实际生成，原版未由插件发起的正常波次不得实际生成。
- 只操作 `D:\Project\Emergency-events\.test-server` 隔离服务器，不操作用户真实游戏客户端。

### Task 1: Add pure wave-control policies

**Files:**
- Create: `Reinforcement/WaveControlPolicy.cs`
- Modify: `Evaluation.Tests/Evaluation.Tests.csproj`
- Modify: `Evaluation.Tests/Program.cs`

**Interfaces:**
- `WaveControlPolicy.IsDue(float elapsedSeconds, float dueSeconds)` returns whether the plugin timer may trigger.
- `WaveControlPolicy.ShouldAllowRespawn(bool pluginWaveInProgress, bool isMiniWave)` returns whether the respawn event may continue.
- `WaveControlPolicy.IsEligibleObserver(bool isConnected, bool isOverwatch, bool isSpectator, bool isUninitialized)` accepts both normal spectators and late dummy `None` roles.

- [ ] **Step 1: Write failing tests**

Add tests asserting 05:00 is due, 04:59.99 is not due, only a plugin normal wave is allowed, every mini wave is rejected, and a connected non-Overwatch `None` dummy is eligible.

- [ ] **Step 2: Run the test runner and verify the expected failure**

Run `dotnet run --project Evaluation.Tests\Evaluation.Tests.csproj -c Release --no-restore --nologo` and require compilation failure because `WaveControlPolicy` does not exist.

- [ ] **Step 3: Implement the smallest policy type**

Implement only the three pure methods above without EXILED or server references.

- [ ] **Step 4: Run the test runner and verify the new tests pass**

Require all existing tests and the new policy tests to pass.

### Task 2: Move wave triggering into the plugin timer

**Files:**
- Modify: `Reinforcement/ReinforcementState.cs`
- Modify: `Reinforcement/ReinforcementManager.cs`

**Interfaces:**
- State records whether a plugin-requested wave is pending or in progress.
- `ReinforcementManager` schedules the existing coroutine monitor for the entire round, not only the first native event.

- [ ] **Step 1: Add the plugin-request state fields**

Add pending/in-progress flags and clear them in every cancellation and round cleanup path.

- [ ] **Step 2: Trigger the due normal wave from the timer**

At `NextNormalWaveDueSeconds`, choose the faction from the first-wave rule or Support Score ratio, mark the request, and call `Respawn.ForceWave` with the selected native team and effects enabled; do not modify tickets.

- [ ] **Step 3: Keep the monitor alive after every wave**

After a successful normal wave, continue monitoring until the next due time, while preserving the existing 05:00/06:30 first-wave skip behavior.

### Task 3: Suppress native waves and admit late dummy candidates

**Files:**
- Modify: `Reinforcement/ReinforcementManager.cs`

**Interfaces:**
- Selecting/respawning events reject any native wave without a matching plugin request.
- Plugin-managed respawn replaces the candidate list with all connected non-Overwatch `Spectator` or `None` players.

- [ ] **Step 1: Reject all unrequested normal waves and all mini waves**

Set `IsAllowed=false` and emit auditable suppression/cancellation logs before the native wave can spawn.

- [ ] **Step 2: Permit only the matching plugin request through the event pipeline**

Override the selected normal wave, populate the candidate list from the expanded eligibility rule, and cancel the request if no candidates remain.

- [ ] **Step 3: Commit only actual plugin-managed spawns**

Keep Support Score and major-wave history updates on `RespawnedTeam`, and clear the in-progress flag on success, empty spawn, rejected mini wave, and cleanup.

### Task 4: Build, deploy, and live-verify

**Files:**
- Generated: `bin/Release/net48/EmergencyEvents.dll`
- Deployed: `.test-server/AppData/EXILED/Plugins/EmergencyEvents.dll`

- [ ] **Step 1: Run pure tests and Release build**

Run the complete test runner and build with `SCPSL_SERVER_PATH` set to `.test-server`, requiring zero failures, warnings, and errors.

- [ ] **Step 2: Deploy the verified DLL and restart only the isolated server**

Verify the deployed hash equals the built hash and confirm plugin load without touching the real client process.

- [ ] **Step 3: Verify the 05:00 and late-dummy behavior**

Start with 16 dummy, add dummy after round start, and confirm logs show a plugin-triggered wave at approximately 05:00 whose candidate count includes the late dummy while the locked opening population remains unchanged.

- [ ] **Step 4: Verify native suppression and mini-wave blocking**

Confirm unrequested native waves produce suppression logs, no mini wave reaches `RespawnedTeam`, and a plugin-requested normal wave produces exactly one `SupportCycleCompleted` record.

- [ ] **Step 5: Re-run regression checks and report Module 3 only**

Require no `ApplyFailed`, `TeleportFailed`, `EvaluationFailed`, or stale-round records, and do not start Module 4.
