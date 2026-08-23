# Five-Minute Normal Reinforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow only native non-mini reinforcement waves at least five minutes apart, select their faction by the two Support Scores, and suppress every mini wave without forcing a wave early.

**Architecture:** Keep the native EXILED respawn scheduler as the clock. `ReinforcementManager` will reject mini waves and normal waves that arrive before the next five-minute gate, then replace only the faction of an eligible native normal wave. The existing first-wave observer/deadline state remains, but `Respawn.ForceWave` is removed so the plugin never accelerates a respawn.

**Tech Stack:** C#/.NET Framework 4.8, EXILED 9.14.2, SCP:SL 14.2.7, YAML configuration, build/deploy verification and live LocalAdmin logs.

## Global Constraints

- Do not add a test project or automated test files; the user explicitly requested live/build verification instead.
- Do not call `Respawn.ForceWave`.
- Do not allow `IsMiniWave == true` to reach a respawn.
- A normal wave may be accepted only when at least 300 seconds have elapsed since the previous accepted normal wave.
- Support Score remains the faction-selection source and retains the existing 25% carryover behavior.

---

### Task 1: Add the configurable normal-wave interval

**Files:**
- Modify: `D:\project\emergency-events\Config.cs`
- Modify: `D:\project\emergency-events\Reinforcement\ReinforcementState.cs`

- [ ] Add `NormalReinforcementIntervalSeconds` with a default of `300f` and add `NextNormalWaveDueSeconds` to round state.
- [ ] Initialize the first due time from the existing first-wave time and report both values in the round-start log.

### Task 2: Gate native waves without accelerating them

**Files:**
- Modify: `D:\project\emergency-events\Reinforcement\ReinforcementManager.cs`

- [ ] Remove the first-wave `Respawn.ForceWave` path.
- [ ] Reject all mini waves in `SelectingRespawnTeam`.
- [ ] Reject normal waves before `NextNormalWaveDueSeconds` and log the native wave name, elapsed time and next due time.
- [ ] For an eligible normal wave, select NTF/CI from Support Score ratio, preserve native player generation, and set the selected native non-mini wave.
- [ ] After a successful normal wave, set the next due time to actual elapsed time plus 300 seconds and then apply the existing score carryover.
- [ ] Defensively reject a mini wave in `RespawningTeam`/`RespawnedTeam` if another component bypasses the selection gate.

### Task 3: Build, deploy and verify

**Files:**
- Generated output: `D:\project\emergency-events\bin\Release\net48\EmergencyEvents.dll`
- Deployed plugin: `C:\Users\Administrator\AppData\Roaming\EXILED\Plugins\EmergencyEvents.dll`
- Live config: `C:\Users\Administrator\AppData\Roaming\EXILED\Configs\Plugins\emergency_events\7777.yml`

- [ ] Build with the existing server reference path and require zero errors.
- [ ] Verify the deployed DLL hash equals the built DLL hash.
- [ ] Confirm the YAML contains the five-minute interval.
- [ ] Do not restart the active round during this change; report that the new DLL takes effect on the next server restart.
- [ ] After the next restart, verify logs for `MiniWaveCancelled`, `NormalWaveHeld`, `WaveSelected`, `SupportCycleCompleted`, and the absence of `FirstWaveForceRequested`.
