# Emergency Events Round Core Design Specification

**Status:** Implemented in `emergency-events` (pure resolver plus EXILED runtime boundary)

## Goal

先完成 `emergency-events` 的第一个可独立验收模块：Round Core 的纯逻辑部分。输入回合开始人口，输出冻结的人口编制档位和精确开局组成；这一层不访问 EXILED、服务器、玩家列表或游戏状态。

## Runtime baseline

- Project: `emergency-events`
- Assembly: `EmergencyEvents`
- Target framework: `.NET Framework 4.8`
- Language version: `C# 12`
- Plugin framework: `ExMod.Exiled 9.14.2`
- State code convention: `DLRC-A4-BIO`
- Current project file: `EmergencyEvents.csproj`

## Population tiers

| Tier | Supported round-start population |
|---|---:|
| E | 16–19 |
| D | 20–25 |
| C | 26–31 |
| B | 32–37 |
| A | 38–45 |

The tier is determined once from the round-start population and remains locked for the whole round. Later joins, leaves, deaths, escapes and reconnects must not recalculate the tier.

## Exact composition table

The table below is the only source of truth for the first milestone. The field order is always `SCP, Security, Chaos, ClassD, Scientist`.

| Population | Tier | SCP | Security | Chaos infiltrator | Class-D | Scientist |
|---:|:---:|---:|---:|---:|---:|---:|
| 16 | E | 3 | 2 | 2 | 6 | 3 |
| 17 | E | 3 | 2 | 2 | 7 | 3 |
| 18 | E | 3 | 2 | 2 | 7 | 4 |
| 19 | E | 3 | 2 | 2 | 8 | 4 |
| 20 | D | 4 | 3 | 3 | 7 | 3 |
| 21 | D | 4 | 3 | 3 | 7 | 4 |
| 22 | D | 4 | 3 | 3 | 8 | 4 |
| 23 | D | 4 | 3 | 3 | 9 | 4 |
| 24 | D | 4 | 3 | 3 | 9 | 5 |
| 25 | D | 4 | 3 | 3 | 10 | 5 |
| 26 | C | 4 | 4 | 4 | 9 | 5 |
| 27 | C | 4 | 4 | 4 | 10 | 5 |
| 28 | C | 4 | 4 | 4 | 11 | 5 |
| 29 | C | 4 | 4 | 4 | 11 | 6 |
| 30 | C | 5 | 4 | 4 | 11 | 6 |
| 31 | C | 5 | 4 | 4 | 12 | 6 |
| 32 | B | 5 | 5 | 5 | 11 | 6 |
| 33 | B | 5 | 5 | 5 | 12 | 6 |
| 34 | B | 5 | 5 | 5 | 13 | 6 |
| 35 | B | 5 | 5 | 5 | 13 | 7 |
| 36 | B | 6 | 5 | 5 | 13 | 7 |
| 37 | B | 6 | 5 | 5 | 14 | 7 |
| 38 | A | 6 | 6 | 6 | 13 | 7 |
| 39 | A | 6 | 6 | 6 | 14 | 7 |
| 40 | A | 6 | 6 | 6 | 15 | 7 |
| 41 | A | 6 | 6 | 6 | 15 | 8 |
| 42 | A | 6 | 6 | 6 | 16 | 8 |
| 43 | A | 7 | 6 | 6 | 16 | 8 |
| 44 | A | 7 | 6 | 6 | 17 | 8 |
| 45 | A | 7 | 6 | 6 | 17 | 9 |

Every row must satisfy:

```text
SCP + Security + Chaos + ClassD + Scientist = Population
Security = Chaos
```

## Out-of-range behavior

The supported exact-composition range is 16–45.

- Population below 16: tier fallback is E, `WasClamped=true`, exact composition is unsupported.
- Population above 45: tier fallback is A, `WasClamped=true`, exact composition is unsupported.
- The resolver must never pretend that 15 is a valid 16-person composition or that 46 is a valid 45-person composition.
- Unsupported results must carry a stable reason such as `UnsupportedPopulation` for later WARN logging.

## Pure-logic contract

The first implementation should expose a deterministic API equivalent to:

```csharp
CompositionResolution GetComposition(int population);
```

`CompositionResolution` must expose the input population, resolved tier, support status, clamp status, an optional `RoundComposition`, and a stable unsupported reason. `RoundComposition.Total` is calculated from its five component counts rather than stored as a second mutable value.

The core resolver must not access `Player.List`, `Round`, `Server`, EXILED events, EXILED logging, timers, random state or configuration.

## Runtime boundary for later work

After the pure logic milestone passes, a separate Round Core runtime layer will:

1. Capture the round-start population and generate a unique `RoundId`.
2. Lock the tier and expected composition.
3. Assign SCP, Facility Guard, Chaos Conscript, Class-D and Scientist roles.
4. Randomly swap Foundation and Chaos between HCZ Elevator System A/B.
5. Apply one shared opening loadout to Security and Chaos infiltrators.
6. Append `安保人员` and `混沌渗透者` to existing titles without replacing them.
7. Re-scan actual roles, spawns, titles and loadouts and report PASS/FAIL.
8. Clean all round state at round end.

This runtime layer is not part of the first pure-logic implementation.

## Out of scope for this milestone

Do not create or implement Reinforcement, D-LRC Evaluator, Crisis, Event Director, O4, BIO, SYS, CON, SEC, GOI, WAR, END, RA commands, database storage or custom victory conditions.

## Acceptance criteria

- All 30 exact inputs from 16 through 45 pass.
- Tier boundary tests pass for 16/19, 20/25, 26/31, 32/37 and 38/45.
- Every exact result totals to its input population.
- Every exact result has mirrored Security and Chaos counts.
- Out-of-range fallback behavior is explicit and tested for 15 and 46.
- `EmergencyEvents.csproj` builds with zero warnings and zero errors.
- No EXILED server API is required to run the pure-logic tests.
