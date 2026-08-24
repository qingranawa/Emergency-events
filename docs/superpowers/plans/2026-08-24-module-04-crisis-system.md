# Module 04 Crisis System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement a read-only, event-driven crisis assessment system for BIO, SYS, CON, SEC, GOI, WAR, and END.

**Architecture:** Module 03 emits exactly one completion event for each successful evaluation. `CrisisManager` consumes that event and evaluates small, side-effect-free detectors against the matching immutable snapshot and result. Only CON and END retain per-round state; all state is cleared at round boundaries.

**Tech Stack:** C# 12, .NET Framework 4.8, EXILED 9.14.2, existing executable pure-logic test harness.

**Spec:** `docs/superpowers/specs/2026-08-24-module-04-crisis-system-design.md`

## Global Constraints

- Do not alter Module 01 A–E composition, Module 02 Primary Wave, Mini-Wave, timer-extension, or Module 03 score/control/30-second scheduling rules.
- Do not create events, O4, Beta-7, Nu-7, GOI gameplay, roles, equipment, or respawn behavior.
- A crisis tag never changes the global D-LRC final level.
- Active tag order is exactly BIO, SYS, CON, SEC, GOI, WAR, END.
- Do not commit or push this work.

---

### Task 1: Pure crisis contracts, configuration, and BIO/SYS/SEC/WAR/GOI detectors

**Files:**
- Create: `Crisis/CrisisTag.cs`, `Crisis/CrisisSeverity.cs`, `Crisis/CrisisDetectionResult.cs`, `Crisis/ICrisisDetector.cs`, `Crisis/CrisisOptions.cs`, `Crisis/Detectors/BioCrisisDetector.cs`, `Crisis/Detectors/SysCrisisDetector.cs`, `Crisis/Detectors/SecCrisisDetector.cs`, `Crisis/Detectors/GoiCrisisDetector.cs`, `Crisis/Detectors/WarCrisisDetector.cs`
- Modify: `Config.cs`, `Evaluation.Tests/Evaluation.Tests.csproj`, `Evaluation.Tests/Program.cs`

**Interfaces:**
- Produces `CrisisDetectionResult Detect(RoundSnapshot snapshot, DlrcEvaluationResult result, CrisisState state, CrisisContext context)` for every detector.
- Produces `CrisisOptions.FromConfig(Config config)` with A–E BIO and SEC threshold accessors.

- [ ] **Step 1: Write failing Module 04 pure tests** for all A–E BIO boundaries, SYS tiers including invalid values, SEC hostile guard, WAR states, GOI future hook, threshold defaults, and global/crisis independence.
- [ ] **Step 2: Run `dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj -- M04`** and confirm the new tests fail because the contracts do not exist.
- [ ] **Step 3: Implement the smallest immutable contracts, options, configuration values, and five stateless detectors** without touching evaluator scoring or scheduling.
- [ ] **Step 4: Run `dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj -- M04`** and confirm the Task 1 tests pass.

### Task 2: Stateful CON and END detectors plus fact-only snapshot fields

**Files:**
- Create: `Crisis/CrisisState.cs`, `Crisis/CrisisContext.cs`, `Crisis/Detectors/ConCrisisDetector.cs`, `Crisis/Detectors/EndCrisisDetector.cs`
- Modify: `Evaluation/EvaluationModels.cs`, `Evaluation/SnapshotCollector.cs`, `Evaluation/EvaluationLogFormatter.cs`, `Evaluation.Tests/Program.cs`

**Interfaces:**
- Produces `CrisisState.Reset()` and continuous-checkpoint APIs used only by CON and END.
- Extends `RoundSnapshot` with `HostileThirdPartyActive`, `HostileThirdPartyCombatants`, `SurfaceFoundationCombatants`, `SurfaceChaosCombatants`, `SurfaceMainScp`, and `SurfaceOtherHostiles` facts.

- [ ] **Step 1: Write failing tests** for second-wave capture, 4:59/5:00 CON boundaries, success reset, three consecutive failures, END 4:59/5:00/8:00/12:00 boundaries, and END stale-mate interruption.
- [ ] **Step 2: Run the Module 04 harness** and confirm each new test fails for missing stateful behavior.
- [ ] **Step 3: Implement fact-only snapshot collection and the minimum CON/END state transitions** with no independent crisis timer.
- [ ] **Step 4: Run the Module 04 harness** and confirm all Task 2 tests pass.

### Task 3: Crisis assessment, formatting, Module 03 completion integration, and cleanup

**Files:**
- Create: `Crisis/CrisisAssessment.cs`, `Crisis/CrisisManager.cs`, `Crisis/CrisisLogFormatter.cs`, `Crisis/DlrcEvaluationCompletedEvent.cs`
- Modify: `Evaluation/DlrcEvaluatorService.cs`, `Plugin.cs`, `Evaluation.Tests/Program.cs`

**Interfaces:**
- Produces `event Action<DlrcEvaluationCompletedEvent>? EvaluationCompleted` after each valid Module 03 result.
- Produces `CrisisAssessment CurrentCrisisAssessment`, `bool IsActive(CrisisTag tag)`, `CrisisSeverity GetSeverity(CrisisTag tag)`, `IReadOnlyList<CrisisTag> ActiveTags`, and `string Code`.

- [ ] **Step 1: Write failing tests** for fixed tag ordering, final-code composition, one crisis evaluation per completed D-LRC result, periodic/post-major provenance, state-change detection, invalid upstream skip, and round cleanup.
- [ ] **Step 2: Run the Module 04 harness** and confirm the integration tests fail for absent publication/assessment behavior.
- [ ] **Step 3: Implement event publication after successful `EvaluateOnce`, manager subscription, single assessment creation, detailed/change logging, plugin lifecycle wiring, and cleanup.**
- [ ] **Step 4: Run the Module 04 harness** and confirm all Module 04 tests pass.

### Task 4: Regression, release build, isolated-server deployment, and review

**Files:**
- Modify only files found necessary by failures in Tasks 1–3.

- [ ] **Step 1: Run `dotnet run --project Evaluation.Tests/Evaluation.Tests.csproj -- M01`, `M02`, `M03`, and `M04`** and retain complete counts.
- [ ] **Step 2: Run `dotnet build EmergencyEvents.csproj --no-restore -c Release` with `SL_REFERENCES` set to `.test-server\SCPSL_Data\Managed`** and confirm zero warnings/errors.
- [ ] **Step 3: Deploy only the built plugin DLL to the isolated server and restart only `.test-server\SCPSL.exe`; confirm the user client process remains untouched.**
- [ ] **Step 4: Inspect the newest LocalAdmin log for plugin load, Module 04 registration, and runtime exceptions.**
- [ ] **Step 5: Request code review, resolve critical or important findings, re-run all verification commands, and report unverified live-only scenarios.**
