# Emergency Events Round Core Implementation Plan

> **Current execution status:** Implemented directly as the first complete module per user instruction. The test-project and xUnit steps below are retained as the original design record but were intentionally skipped; no test project or test source was added. Runtime EXILED integration was implemented in the same milestone.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build and verify the pure-logic Round Core that maps round-start population 16–45 to the locked A–E tier and exact opening composition for `emergency-events`.

**Architecture:** Keep the composition resolver deterministic and framework-independent, then connect it to an EXILED runtime boundary in the same first module. The runtime captures and locks the opening roster, assigns the exact composition, mirrors the two opening human teams, swaps their HCZ elevator sides, validates the result and cleans up round state.

**Tech Stack:** C# 12, .NET Framework 4.8, `ExMod.Exiled` 9.14.2, EXILED 9.14.2 runtime assemblies and the server `UnityEngine.CoreModule.dll` supplied through `SL_REFERENCES`. Tests were intentionally omitted for this implementation pass.

**Spec:** `docs/superpowers/specs/2026-08-22-round-core-design.md`

## Global Constraints

- Use `EmergencyEvents` as the assembly and root namespace.
- Use `.NET Framework 4.8` and `C# 12`.
- Keep `ExMod.Exiled` pinned at `9.14.2`.
- Treat the 16–45 table in the spec as the only exact composition source.
- Freeze the population tier at round start; never recalculate it from later population changes.
- For populations outside 16–45, clamp only the fallback tier and return an explicit unsupported-composition result.
- Keep core resolution free of EXILED, server, player, timer, random and logger dependencies.
- Do not implement Reinforcement, D-LRC Evaluator, Crisis, Event Director, O4, event packs or RA commands in this plan.
- No database and no custom victory logic.
- Do not claim live-server validation until the later runtime milestone has actually run on a test server.

---

### Task 1: Create the testable solution structure

**Files:**
- Modify: `EmergencyEvents.csproj`
- Create: `EmergencyEvents.sln`
- Create: `Tests/EmergencyEvents.Tests/EmergencyEvents.Tests.csproj`

**Interfaces:**
- Consumes: the already-restored `EmergencyEvents.csproj` and `ExMod.Exiled 9.14.2`.
- Produces: a `net48` xUnit test project referencing the main project.

- [ ] **Step 1: Add the solution and test project without adding production behavior**

Use the standard .NET solution/project commands or equivalent project files. The test project must contain:

```xml
<TargetFramework>net48</TargetFramework>
<IsPackable>false</IsPackable>
<PlatformTarget>x64</PlatformTarget>
```

Add these exact test packages:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Add:

```xml
<ProjectReference Include="..\..\EmergencyEvents.csproj" />
```

- [ ] **Step 2: Restore and build the empty solution**

Run:

```text
dotnet restore EmergencyEvents.sln
dotnet build EmergencyEvents.sln --no-restore
```

Expected: restore succeeds and both projects build with zero warnings and zero errors. No test behavior is claimed yet.

---

### Task 2: Write the exact composition contract first

**Files:**
- Create: `Tests/EmergencyEvents.Tests/RoundCore/CompositionResolverTests.cs`

**Interfaces:**
- Consumes: the public contract described in the spec.
- Produces: tests that call `CompositionResolver.GetComposition(int)` and inspect `CompositionResolution` and `RoundComposition`.

- [ ] **Step 1: Write the failing exact-case test**

Use one xUnit theory with the complete cases from the spec. The test shape must be:

```csharp
[Theory]
[InlineData(16, PopulationTier.E, 3, 2, 2, 6, 3)]
[InlineData(17, PopulationTier.E, 3, 2, 2, 7, 3)]
[InlineData(18, PopulationTier.E, 3, 2, 2, 7, 4)]
[InlineData(19, PopulationTier.E, 3, 2, 2, 8, 4)]
[InlineData(20, PopulationTier.D, 4, 3, 3, 7, 3)]
[InlineData(21, PopulationTier.D, 4, 3, 3, 7, 4)]
[InlineData(22, PopulationTier.D, 4, 3, 3, 8, 4)]
[InlineData(23, PopulationTier.D, 4, 3, 3, 9, 4)]
[InlineData(24, PopulationTier.D, 4, 3, 3, 9, 5)]
[InlineData(25, PopulationTier.D, 4, 3, 3, 10, 5)]
[InlineData(26, PopulationTier.C, 4, 4, 4, 9, 5)]
[InlineData(27, PopulationTier.C, 4, 4, 4, 10, 5)]
[InlineData(28, PopulationTier.C, 4, 4, 4, 11, 5)]
[InlineData(29, PopulationTier.C, 4, 4, 4, 11, 6)]
[InlineData(30, PopulationTier.C, 5, 4, 4, 11, 6)]
[InlineData(31, PopulationTier.C, 5, 4, 4, 12, 6)]
[InlineData(32, PopulationTier.B, 5, 5, 5, 11, 6)]
[InlineData(33, PopulationTier.B, 5, 5, 5, 12, 6)]
[InlineData(34, PopulationTier.B, 5, 5, 5, 13, 6)]
[InlineData(35, PopulationTier.B, 5, 5, 5, 13, 7)]
[InlineData(36, PopulationTier.B, 6, 5, 5, 13, 7)]
[InlineData(37, PopulationTier.B, 6, 5, 5, 14, 7)]
[InlineData(38, PopulationTier.A, 6, 6, 6, 13, 7)]
[InlineData(39, PopulationTier.A, 6, 6, 6, 14, 7)]
[InlineData(40, PopulationTier.A, 6, 6, 6, 15, 7)]
[InlineData(41, PopulationTier.A, 6, 6, 6, 15, 8)]
[InlineData(42, PopulationTier.A, 6, 6, 6, 16, 8)]
[InlineData(43, PopulationTier.A, 7, 6, 6, 16, 8)]
[InlineData(44, PopulationTier.A, 7, 6, 6, 17, 8)]
[InlineData(45, PopulationTier.A, 7, 6, 6, 17, 9)]
public void GetComposition_returns_the_locked_exact_row(
    int population,
    PopulationTier expectedTier,
    int expectedScp,
    int expectedSecurity,
    int expectedChaos,
    int expectedClassD,
    int expectedScientist)
{
    CompositionResolution result = CompositionResolver.GetComposition(population);

    Assert.True(result.IsSupported);
    Assert.NotNull(result.Composition);
    Assert.Equal(expectedTier, result.Tier);
    Assert.Equal(expectedScp, result.Composition!.ScpCount);
    Assert.Equal(expectedSecurity, result.Composition.SecurityCount);
    Assert.Equal(expectedChaos, result.Composition.ChaosInfiltratorCount);
    Assert.Equal(expectedClassD, result.Composition.ClassDCount);
    Assert.Equal(expectedScientist, result.Composition.ScientistCount);
    Assert.Equal(population, result.Composition.Total);
}
```

These are the complete 30 rows; do not add formula-derived rows or replace them with a tier-level formula.

- [ ] **Step 2: Add boundary and invariant tests**

Add separate tests for:

```csharp
[Theory]
[InlineData(16, PopulationTier.E)]
[InlineData(19, PopulationTier.E)]
[InlineData(20, PopulationTier.D)]
[InlineData(25, PopulationTier.D)]
[InlineData(26, PopulationTier.C)]
[InlineData(31, PopulationTier.C)]
[InlineData(32, PopulationTier.B)]
[InlineData(37, PopulationTier.B)]
[InlineData(38, PopulationTier.A)]
[InlineData(45, PopulationTier.A)]
public void GetComposition_resolves_tier_boundaries(int population, PopulationTier expectedTier)
```

And one 16–45 loop test that asserts `Total == population` and `SecurityCount == ChaosInfiltratorCount` for every input. Keep this separate from the exact-row test so a failure identifies whether the table or an invariant is wrong.

- [ ] **Step 3: Add explicit unsupported-population tests**

Add tests for 15 and 46:

```csharp
[Theory]
[InlineData(15, PopulationTier.E)]
[InlineData(46, PopulationTier.A)]
public void GetComposition_clamps_only_the_fallback_tier_and_rejects_exact_composition(
    int population,
    PopulationTier expectedFallbackTier)
{
    CompositionResolution result = CompositionResolver.GetComposition(population);

    Assert.False(result.IsSupported);
    Assert.True(result.WasClamped);
    Assert.Equal(expectedFallbackTier, result.Tier);
    Assert.Null(result.Composition);
    Assert.Equal("UnsupportedPopulation", result.UnsupportedReason);
}
```

- [ ] **Step 4: Run the test project and verify the red state**

Run:

```text
dotnet test Tests/EmergencyEvents.Tests/EmergencyEvents.Tests.csproj --no-restore --verbosity normal
```

Expected red result: the test project cannot resolve the not-yet-created `PopulationTier`, `CompositionResolver`, `CompositionResolution` and `RoundComposition` API. If the failure is instead a package, target-framework or test-runner error, fix that setup error before implementing production types.

---

### Task 3: Implement the minimal pure Round Core types

**Files:**
- Create: `RoundCore/PopulationTier.cs`
- Create: `RoundCore/RoundComposition.cs`
- Create: `RoundCore/CompositionTable.cs`
- Create: `RoundCore/CompositionResolver.cs`

**Interfaces:**
- Consumes: the failing tests from Task 2.
- Produces: `PopulationTier`, `RoundComposition`, `CompositionResolution` and `CompositionResolver.GetComposition(int)`.

- [ ] **Step 1: Add the tier enum**

Define exactly five values in population order:

```csharp
public enum PopulationTier
{
    E,
    D,
    C,
    B,
    A,
}
```

- [ ] **Step 2: Add immutable composition and resolution models**

`RoundComposition` must expose read-only values for `Population`, `Tier`, `ScpCount`, `SecurityCount`, `ChaosInfiltratorCount`, `ClassDCount` and `ScientistCount`. Implement:

```csharp
public int Total =>
    ScpCount + SecurityCount + ChaosInfiltratorCount + ClassDCount + ScientistCount;
```

`CompositionResolution` must expose `IsSupported`, `WasClamped`, `Tier`, `RoundComposition? Composition` and `string? UnsupportedReason`. Do not store a mutable `Total` duplicate.

- [ ] **Step 3: Add the read-only exact table**

Store all 30 rows in one focused `CompositionTable` type. Use a read-only lookup keyed by population. Do not derive counts from a formula, percentage, interpolation or tier-level average; the approved table is authoritative.

- [ ] **Step 4: Implement `GetComposition` with only the required branches**

The method must:

1. Resolve E/D/C/B/A for supported inputs.
2. Return the exact table row for 16–45.
3. Return E + unsupported for values below 16.
4. Return A + unsupported for values above 45.
5. Return `UnsupportedPopulation` rather than throwing an indexing exception.
6. Validate the table row total before returning a supported result; if an internal row is invalid, fail loudly with a clear exception instead of returning corrupted data.

- [ ] **Step 5: Run the focused tests**

Run:

```text
dotnet test Tests/EmergencyEvents.Tests/EmergencyEvents.Tests.csproj --no-restore --filter FullyQualifiedName~CompositionResolverTests --verbosity normal
```

Expected: all exact, boundary, invariant and unsupported-population tests pass. The output must show 30 exact composition cases passing.

---

### Task 4: Refactor without changing the contract

**Files:**
- Modify: `RoundCore/CompositionResolver.cs`
- Modify: `RoundCore/CompositionTable.cs`
- Modify: `Tests/EmergencyEvents.Tests/RoundCore/CompositionResolverTests.cs`

**Interfaces:**
- Consumes: the green resolver and its tests.
- Produces: the same public API with clearer diagnostics and no duplicated rules.

- [ ] **Step 1: Review the table and resolver for duplicated truth**

Keep population ranges in one resolver rule and exact rows in one table. Do not copy the composition table into production code and tests in a way that could silently drift; tests intentionally contain expected values, while production contains the data source.

- [ ] **Step 2: Add diagnostic assertions without EXILED logging**

Assert that supported results have `WasClamped == false` and `UnsupportedReason == null`. Assert that unsupported results have a stable reason. The resolver remains a pure function; runtime log formatting comes later.

- [ ] **Step 3: Run all tests and build**

Run:

```text
dotnet test EmergencyEvents.sln --no-restore --verbosity normal
dotnet build EmergencyEvents.sln --no-restore
```

Expected: all tests pass, build has zero warnings and zero errors.

---

### Task 5: Freeze the first milestone and prepare runtime integration

**Files:**
- Create later, only after Tasks 1–4 pass: `RoundCore/RoundCoreState.cs`
- Create later, only after Tasks 1–4 pass: `RoundCore/RoundCoreManager.cs`
- Create later, only after Tasks 1–4 pass: `Plugin.cs`
- Create later, only after Tasks 1–4 pass: `Config.cs`

**Interfaces:**
- Consumes: `CompositionResolution` from the pure core.
- Produces later: a round state that stores `RoundId`, start population, locked tier, expected composition, initialization state and validation state.

- [ ] **Step 1: Stop after the pure milestone**

Do not create the runtime files in this task yet. Record the pure test/build results and confirm the first acceptance gate:

```text
30/30 exact composition cases PASS
Tier boundaries PASS
Unsupported populations PASS
Build: 0 warnings, 0 errors
No server integration claimed
```

- [ ] **Step 2: Review the runtime boundary before implementation**

The next approved design task will inspect the actual EXILED 9.14.2 event names and role/spawn APIs from the restored assemblies before writing `Plugin.cs`. No API signature should be guessed from older EXILED documentation.

---

## Later module roadmap

These modules are intentionally separate plans. They do not belong in the first Round Core change.

1. **Round Core runtime integration:** capture round-start population, freeze state, assign roles, A/B HCZ spawn swap, mirrored loadout, title append and runtime self-validation.
2. **Reinforcement System:** DD/Scientist escape scoring, first-wave window, 06:30 deadline, 25% score retention, ordinary-wave scheduling and duplicate-score protection.
3. **D-LRC Evaluator:** 30-second snapshots, response score components, control state and A–E-specific level thresholds.
4. **Crisis System:** BIO/SYS/CON/SEC/GOI/WAR/END detectors with independent severity and lifecycle logs.
5. **Event Director:** 120-second candidate evaluation, exact-level filtering, personnel cooldown, cost transaction, favorability and active-event uniqueness.
6. **O4 Command:** public dynamic panel and voting only among already-valid candidates; no location or probability leaks.
7. **Event packs:** implement one event at a time, starting with BIO Level 3, each with A–E scale, start/success/failure/termination rules and dedicated tests.
8. **Live-server validation and release:** test-server logs, round replay checks, DLL packaging, README and compatibility notes.

## Stop conditions

Stop and report instead of guessing if any of these occur:

- The exact composition table changes.
- EXILED 9.14.2 cannot load or compile against the current server installation.
- A runtime API signature differs from the verified assembly.
- A table row does not total to its input population.
- A test passes before the intended production behavior exists.
- Live-server verification is unavailable; report unit-test status separately from integration status.
