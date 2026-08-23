# Emergency Events Reinforcement System Implementation Plan

> **Current execution status:** Executing directly in the existing workspace per user instruction. No test project or test source will be added; acceptance is build verification plus live EXILED server log verification.

**Goal:** Implement the first runtime version of the Reinforcement System for `emergency-events`, while correcting Round Core SCP allocation so every supported opening composition with at least two SCP slots contains at least two `Scp939` roles and retains `Scp3114` as a possible remaining role.

**Architecture:** Keep support-score bookkeeping and decision rules inside `EmergencyEvents.Reinforcement`, and keep EXILED event subscriptions in the manager boundary. Use the native EXILED respawn pipeline: override the selected faction/wave, preserve native wave size and role generation, and only filter Overwatch from the first wave. Do not replace the server's native victory logic, respawn equipment, token system or wave timing beyond the explicitly requested first-wave window.

**Tech Stack:** C# 12, .NET Framework 4.8, `ExMod.Exiled` 9.14.2, EXILED 9.14.2 runtime assemblies and the server `UnityEngine.CoreModule.dll` supplied through `SL_REFERENCES`.

## Global Constraints

- Keep `EmergencyEvents` as the assembly and root namespace.
- Keep `ExMod.Exiled` pinned at `9.14.2`.
- Do not add a test project, test source or RA command in this milestone.
- Keep the existing Round Core exact population table and locked tier behavior unchanged.
- Guarantee two `Scp939` assignments whenever the exact composition has at least two SCP slots; fill remaining slots from the existing random pool, including `Scp3114`.
- Use the verified EXILED 9.14.2 `EscapeScenario` values:
  - `ClassD` -> Chaos +1.
  - `CuffedClassD` -> Foundation +1.
  - `Scientist` -> Foundation +2.
  - `CuffedScientist` -> Chaos +2.
- Score each player escape at most once per round; rejected duplicates must be logged.
- The first normal large wave opens at 300 seconds, waits for the first eligible spectator, and is skipped at 390 seconds if none appears.
- First-wave faction: higher support score wins; a tie uses a logged 50/50 random roll.
- First wave uses native non-mini NTF/CI wave behavior and excludes Overwatch; later waves preserve the native mini/non-mini shape and choose faction by the two score ratio.
- After each actual respawn cycle, retain 25% of each score using midpoint-away-from-zero rounding.
- Do not claim a wave was spawned until the server's `RespawnedTeam` event and logs confirm it.
- Every decision log must include inputs, reason, selected action and outcome.

## Task 1: Correct SCP allocation

**Files:**
- Modify: `RoundCore/RoundCoreManager.cs`

**Implementation:**

- Build a shuffled list containing two `Scp939` entries when `ScpCount >= 2`.
- Fill the remaining slots from the existing pool excluding the guaranteed entries, preserving `Scp3114` as a valid candidate.
- Shuffle the final role list before assigning players so the two `Scp939` slots are not position-fixed.
- Keep the existing total-count/runtime validation.

## Task 2: Add reinforcement configuration and state

**Files:**
- Modify: `Config.cs`
- Create: `Reinforcement/ReinforcementState.cs`

**Configuration:**

- `ReinforcementEnabled` default `true`.
- `FirstReinforcementTimeSeconds` default `300`.
- `FirstReinforcementDeadlineSeconds` default `390`.
- `SupportScoreCarryoverRatio` default `0.25`.
- `ClassDSupportScore` default `1`.
- `ScientistSupportScore` default `2`.

**State:**

- Round id and active flag.
- Foundation and Chaos support scores.
- First-wave state: pending, waiting, requested, skipped or completed.
- Chosen faction and current wave metadata.
- Per-round scored player ids.
- Support cycle counter and last wave result metadata.

## Task 3: Implement the reinforcement manager

**Files:**
- Create: `Reinforcement/ReinforcementManager.cs`

**Interfaces:**

- `ResetForWaitingForPlayers()` clears the previous round and stops the first-wave monitor.
- `StartRound(long roundId)` initializes scores and starts the first-wave monitor.
- `HandleEscape(EscapedEventArgs ev)` maps and records support points.
- `HandleSelectingRespawnTeam(SelectingRespawnTeamEventArgs ev)` chooses the first-wave faction or later faction ratio and swaps the native timed wave.
- `HandleRespawningTeam(RespawningTeamEventArgs ev)` applies the pending native wave and filters first-wave players to eligible spectators.
- `HandleRespawnedTeam(RespawnedTeamEventArgs ev)` confirms the actual cycle, logs the result and applies 25% score carryover.
- `CleanupRound()` stops monitoring and emits the final support summary.

**First-wave monitor:**

- Run on the EXILED/MEC main-thread scheduler.
- At 300 seconds, immediately force the selected non-mini faction if eligible spectators exist.
- If none exist, poll the same round-thread state until an eligible spectator appears.
- At 390 seconds, mark the first wave skipped only if no eligible spectator ever appeared.
- Never force a wave from a background `System.Threading` callback.

## Task 4: Wire plugin events and logging

**Files:**
- Modify: `Plugin.cs`

Subscribe/unsubscribe:

- `ServerEvents.RoundStarted`
- `ServerEvents.RespawningTeam`
- `ServerEvents.SelectingRespawnTeam`
- `ServerEvents.RespawnedTeam`
- `PlayerEvents.Escaped`

Keep Round Core's existing event-order workaround. Log enable/disable and all reinforcement lifecycle events with `RoundId`, elapsed time, score before/after, faction comparison, wave shape, eligible/excluded player counts and failure reasons.

## Task 5: Build, deploy and perform live verification

**Verification:**

- Build the production DLL with the server's actual `SL_REFERENCES` path and require zero warnings/errors.
- Deploy the DLL and current configuration to the EXILED plugin directory.
- Restart only the LocalAdmin-hosted server process, never the user's client process.
- Inspect the new LocalAdmin log for plugin load, first-wave window, score changes, faction decision, respawning and post-wave carryover.
- Report only the log lines that actually exist. If the first-wave time window cannot be reached in the current live round, report the implementation/build result separately and give the exact manual test sequence for the user.

## Stop conditions

Stop and report instead of guessing if:

- The verified EXILED API does not compile with the chosen event signatures.
- Native respawn events do not expose a safe way to preserve the wave shape.
- The server log contradicts the intended first-wave or score behavior.
- A build error indicates a wrong API assumption; fix from the actual assembly before proceeding.
