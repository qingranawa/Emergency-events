# Module 04 — Crisis System Design

**Status:** Approved by the Module 04 request dated 2026-08-24.

## Goal

Build a read-only Crisis System that classifies professional crisis conditions from one successful Module 03 evaluation, its matching `RoundSnapshot`, and small per-round detector state.

## Boundary

Module 04 produces `CrisisAssessment` and a display code such as `DLRC-C3-BIO+SYS`. It does not alter D-LRC score, control state, wave timers, respawn selection, player roles, events, O4, or special response teams.

## Data flow

`DlrcEvaluatorService` will publish one `DlrcEvaluationCompletedEvent` only after it has successfully produced and stored a valid result. The event includes the existing evaluation result, the exact snapshot used for it, an increasing evaluation id, and `PERIODIC` or `POST_MAJOR_WAVE` trigger provenance. `CrisisManager` subscribes to that event; it never scans `Player.Enumerable` and never owns a timer.

`SnapshotCollector` will add fact-only fields for hostile third-party hooks and surface combatant categories. It will classify only connected, alive, non-spectator, non-overwatch players; no precise positions are persisted. The current implementation has no registered hostile third party, so both third-party fields remain false or zero.

## Crisis model

Each detector implements `ICrisisDetector.Detect(RoundSnapshot, DlrcEvaluationResult, CrisisState, CrisisContext)`. A detection returns its tag, active flag, severity from 0 through 5, reason, and metrics. `CrisisManager` runs the seven detectors, orders active tags as `BIO`, `SYS`, `CON`, `SEC`, `GOI`, `WAR`, `END`, builds `CrisisAssessment`, publishes state changes, and exposes `CurrentCrisisAssessment` for Module 05.

`CrisisState` is reset on every round cleanup. Its only durable per-round values are containment checkpoint baseline/time/failure streak and warhead/surface-stalemate observations needed by CON and END.

## Detector rules

- BIO uses configurable A–E zombie thresholds. SCP-049 itself is not required.
- SYS maps valid SCP-079 Tier 3/4/5 to severity 3/4/5. Invalid source values are reported as unavailable rather than promoted to severity 5.
- CON starts from the second actual successful primary-wave record, uses `MainScpAlive + Scp0492Count / 3d`, and compares independent five-minute checkpoints. A reduction of at least 1.0 resolves and resets the streak; failures map to 3, 4, then 5.
- SEC requires both a configurable Foundation threshold and a present main-SCP, Chaos, or hostile-third-party threat. It checks L5, L4, then L3.
- GOI remains inactive unless the future fact hook says a hostile third party is active, the D-LRC result is level 3 or higher, and `FoundationStrength` is `WEAK` or `CRITICAL`.
- WAR is severity 3 when unlocked and severity 4 during an active countdown. A detonation clears WAR; no speculative severity 5 is generated.
- END starts only after a detected warhead detonation. It requires a continuously hostile surface coexistence for configurable 5, 8, and 12 minute thresholds; breaking coexistence resets the timer.

## Configuration and validation

Module 04 adds an enable flag, per-tier BIO and SEC threshold objects, and CON/END duration settings. Invalid thresholds fall back to the documented defaults. Threshold validation preserves the intended descending severity order and does not silently make a less-severe boundary more restrictive.

## Verification

The pure test harness will gain dedicated Module 04 tests for every specified BIO, SYS, CON, SEC, WAR, END, ordering, independence, event, and cleanup condition. Module 01, Module 02, and the original 43 Module 03 assertions remain unchanged and must pass. A release build against the isolated server references and a server-load log check are separate deployment evidence, not a substitute for live crisis scenarios.
