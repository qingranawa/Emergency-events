# EMERGENCY EVENTS BALANCE ANALYSIS PHASE 1 REPORT

> 历史快照：本报告与下方 CSV 生成于 2026-08-27，记录的是当日代码快照的离线分析结果。它们是历史分析资料，不是当前 Gameplay 实现说明；当前实现与运行时契约以源码和 `docs/RUNTIME_CONTRACTS.md`、`docs/TESTING.md` 为准。

本报告只包含 2026-08-27 代码快照的离线模拟，不修改正式 Gameplay、D-LRC threshold、FDI recovery、Director cadence 或 Event Pack。所有分布均为 SIMULATED DISTRIBUTION，不是实服分布。

## Analysis boundary

- CONFIRMED CODE FACT AT SNAPSHOT：D-LRC 使用当日 `ResponseScoreCalculator`、`ControlEvaluator`、`LevelResolver` 与 `EvaluationOptions`；当日 FDI 生产模型仍是首次存量加近期瞬时量、后续纯增量，无 Order Recovery 生产实现。
- SIMULATION RESULT：本报告中的场景、随机状态和事件流由固定 seed 与明确假设生成。
- DESIGN INFERENCE：候选恢复模型和参数只是 PROPOSAL ONLY。
- PENDING LIVE VALIDATION：未使用真人或实服战局数据，不能推断真实发生率或玩家体感。

## A. D-LRC BASELINE

SOURCE FORMULA VERIFIED: YES
TIERS ANALYZED: E / D / C / B / A
SEMANTIC SCENARIOS: 50
RANDOM STATES PER TIER: 150,000 (37,500 per bucket; 3 seeds)
SEEDS: 101, 202, 303

### Level reachability

数学上，0–100 分制配合当前五档阈值使 L0–L5 的 Theoretical Level 对 E/D/C/B/A 都可达；Final L5 也可由高分加 COLLAPSE cap=5 的合法状态构造。以下是本次语义场景与分层随机模拟的实际观察结果，‘COMMON’ 定义为该 tier/bucket 中 >=1%，‘RARE’ 定义为 >0% 且 <1%，‘EXTREME ONLY’ 表示只在语义极端场景中观察到。

| Level | E | D | C | B | A |
|---|---|---|---|---|---|
| L0 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION |
| L1 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION |
| L2 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION |
| L3 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION |
| L4 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION |
| L5 | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | COMMON IN SIMULATION | RARE |

### Semantic scenario results

| Tier | Scenario | SCP Threat | Foundation Pressure | Reinforcement Failure | Time | Strategic | Natural | Effective | Theoretical | Control | Cap | Final | Diagnosis |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|---|
| E | S0 | 17.67 | 0 | 0 | 0 | 0 | 17.67 | 17.67 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| E | S1 | 20.17 | 1 | 0 | 2 | 0 | 23.17 | 23.17 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| E | S2 | 33.5 | 8 | 0 | 4 | 0 | 45.5 | 45.5 | L2 | CONTROLLED | 3 | L2 | SCORE / THRESHOLD BOTTLENECK |
| E | S3 | 34 | 5 | 15 | 4 | 0 | 58 | 58 | L3 | UNCONTROLLED | 4 | L3 | SCORE / THRESHOLD BOTTLENECK |
| E | S4 | 38.5 | 15 | 15 | 6 | 5 | 79.5 | 79.5 | L4 | UNCONTROLLED | 4 | L4 | NO CAP LOSS |
| E | S5 | 40 | 16 | 20 | 8 | 10 | 94 | 94 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| E | S6 | 39.5 | 20 | 20 | 10 | 10 | 99.5 | 99.5 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| E | S7 | 7.83 | 0 | 0 | 10 | 0 | 17.83 | 17.83 | L0 | ADVANTAGE | 2 | L0 | SCORE / THRESHOLD BOTTLENECK |
| E | S8 | 20.17 | 0 | 0 | 2 | 0 | 22.17 | 22.17 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| E | S9 | 20.17 | 17 | 0 | 2 | 0 | 39.17 | 39.17 | L2 | COLLAPSE | 5 | L2 | SCORE / THRESHOLD BOTTLENECK |
| D | S0 | 13.25 | 0 | 0 | 0 | 0 | 13.25 | 13.25 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| D | S1 | 22.42 | 4 | 0 | 2 | 0 | 28.42 | 28.42 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| D | S2 | 33.5 | 8 | 0 | 4 | 0 | 45.5 | 45.5 | L2 | CONTROLLED | 3 | L2 | SCORE / THRESHOLD BOTTLENECK |
| D | S3 | 34 | 5 | 15 | 4 | 0 | 58 | 58 | L3 | UNCONTROLLED | 4 | L3 | SCORE / THRESHOLD BOTTLENECK |
| D | S4 | 38.5 | 15 | 15 | 6 | 5 | 79.5 | 79.5 | L4 | UNCONTROLLED | 4 | L4 | NO CAP LOSS |
| D | S5 | 40 | 16 | 20 | 8 | 10 | 94 | 94 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| D | S6 | 39.5 | 20 | 20 | 10 | 10 | 99.5 | 99.5 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| D | S7 | 5.88 | 0 | 0 | 10 | 0 | 15.88 | 15.88 | L0 | ADVANTAGE | 2 | L0 | SCORE / THRESHOLD BOTTLENECK |
| D | S8 | 22.42 | 1 | 0 | 2 | 0 | 25.42 | 25.42 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| D | S9 | 15.83 | 18 | 0 | 2 | 0 | 35.83 | 35.83 | L2 | COLLAPSE | 5 | L2 | SCORE / THRESHOLD BOTTLENECK |
| C | S0 | 13.25 | 0 | 0 | 0 | 0 | 13.25 | 13.25 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| C | S1 | 22.42 | 1 | 0 | 2 | 0 | 25.42 | 25.42 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| C | S2 | 33.5 | 8 | 0 | 4 | 0 | 45.5 | 45.5 | L2 | CONTROLLED | 3 | L2 | SCORE / THRESHOLD BOTTLENECK |
| C | S3 | 34 | 5 | 15 | 4 | 0 | 58 | 58 | L3 | UNCONTROLLED | 4 | L3 | SCORE / THRESHOLD BOTTLENECK |
| C | S4 | 38.5 | 13 | 15 | 6 | 5 | 77.5 | 77.5 | L4 | UNCONTROLLED | 4 | L4 | NO CAP LOSS |
| C | S5 | 40 | 16 | 20 | 8 | 10 | 94 | 94 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| C | S6 | 39.5 | 20 | 20 | 10 | 10 | 99.5 | 99.5 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| C | S7 | 5.88 | 0 | 0 | 10 | 0 | 15.88 | 15.88 | L0 | ADVANTAGE | 2 | L0 | SCORE / THRESHOLD BOTTLENECK |
| C | S8 | 22.42 | 0 | 0 | 2 | 0 | 24.42 | 24.42 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| C | S9 | 15.83 | 17 | 0 | 2 | 0 | 34.83 | 34.83 | L1 | COLLAPSE | 5 | L1 | SCORE / THRESHOLD BOTTLENECK |
| B | S0 | 15.9 | 0 | 0 | 0 | 0 | 15.9 | 15.9 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| B | S1 | 23.77 | 1 | 0 | 2 | 0 | 26.77 | 26.77 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| B | S2 | 27.8 | 5 | 0 | 4 | 0 | 36.8 | 36.8 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| B | S3 | 28.2 | 5 | 15 | 4 | 0 | 52.2 | 52.2 | L2 | UNCONTROLLED | 4 | L2 | SCORE / THRESHOLD BOTTLENECK |
| B | S4 | 38.5 | 13 | 15 | 6 | 5 | 77.5 | 77.5 | L4 | UNCONTROLLED | 4 | L4 | NO CAP LOSS |
| B | S5 | 40 | 16 | 20 | 8 | 10 | 94 | 94 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| B | S6 | 39.5 | 20 | 20 | 10 | 10 | 99.5 | 99.5 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| B | S7 | 4.7 | 0 | 0 | 10 | 0 | 14.7 | 14.7 | L0 | ADVANTAGE | 2 | L0 | SCORE / THRESHOLD BOTTLENECK |
| B | S8 | 18.37 | 0 | 0 | 2 | 0 | 20.37 | 20.37 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| B | S9 | 18.43 | 18 | 0 | 2 | 0 | 38.43 | 38.43 | L2 | COLLAPSE | 5 | L2 | SCORE / THRESHOLD BOTTLENECK |
| A | S0 | 13.25 | 0 | 0 | 0 | 0 | 13.25 | 13.25 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| A | S1 | 20.17 | 1 | 0 | 2 | 0 | 23.17 | 23.17 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| A | S2 | 28.75 | 5 | 0 | 4 | 0 | 37.75 | 37.75 | L1 | CONTROLLED | 3 | L1 | SCORE / THRESHOLD BOTTLENECK |
| A | S3 | 29.17 | 5 | 15 | 4 | 0 | 53.17 | 53.17 | L2 | UNCONTROLLED | 4 | L2 | SCORE / THRESHOLD BOTTLENECK |
| A | S4 | 38.5 | 13 | 15 | 6 | 5 | 77.5 | 77.5 | L4 | UNCONTROLLED | 4 | L4 | NO CAP LOSS |
| A | S5 | 40 | 16 | 20 | 8 | 10 | 94 | 94 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| A | S6 | 39.5 | 20 | 20 | 10 | 10 | 99.5 | 99.5 | L5 | COLLAPSE | 5 | L5 | NO CAP LOSS |
| A | S7 | 7.83 | 0 | 0 | 10 | 0 | 17.83 | 17.83 | L0 | ADVANTAGE | 2 | L0 | SCORE / THRESHOLD BOTTLENECK |
| A | S8 | 20.17 | 0 | 0 | 2 | 0 | 22.17 | 22.17 | L0 | CONTROLLED | 3 | L0 | SCORE / THRESHOLD BOTTLENECK |
| A | S9 | 15.83 | 17 | 0 | 2 | 0 | 34.83 | 34.83 | L1 | COLLAPSE | 5 | L1 | SCORE / THRESHOLD BOTTLENECK |

### Distribution level results

#### Tier E

| Bucket | States | Final L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L3 -> Final below | Theoretical L4 -> Final below | Theoretical L5 -> Final below |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| NORMAL | 37,500 | 43.3% | 54.17% | 2.52% | 0% | 0% | 0% | 43.3% | 54.17% | 2.52% | 0% | 0% | 0% | 0 | 0 | 0 |
| PRESSURED | 37,500 | 8.16% | 59.33% | 32.2% | 0.31% | 0% | 0% | 8.16% | 59.33% | 32.2% | 0.31% | 0% | 0% | 0 | 0 | 0 |
| SERIOUS | 37,500 | 0% | 0.05% | 10.7% | 63.76% | 25.45% | 0.04% | 0% | 0.05% | 10.34% | 58.59% | 30.15% | 0.87% | 135 | 2,075 | 311 |
| CRITICAL | 37,500 | 0% | 0% | 0.11% | 9.11% | 68.61% | 22.17% | 0% | 0% | 0.11% | 8.82% | 63.81% | 27.27% | 0 | 112 | 1,913 |

进入 L3/L4/L5 的分项贡献统计（按 Final >= 目标等级筛选，mean / median / P75 / P90）：
- Final >= L3: SCP Threat [71,044] mean=30.73, median=32.01, P75=34.7, P90=36.97; Foundation Pressure [71,044] mean=11.93, median=12, P75=16, P90=18; Wave Failure [71,044] mean=15.31, median=15, P75=20, P90=20; Time [71,044] mean=6.63, median=8, P75=10, P90=10; Strategic Hazard [71,044] mean=5.14, median=5, P75=10, P90=10
- Final >= L4: SCP Threat [43,598] mean=33, median=33.45, P75=35.79, P90=37.74; Foundation Pressure [43,598] mean=13.97, median=14, P75=16, P90=18; Wave Failure [43,598] mean=16.94, median=17, P75=20, P90=20; Time [43,598] mean=7.28, median=8, P75=10, P90=10; Strategic Hazard [43,598] mean=5.58, median=5, P75=10, P90=10
- Final >= L5: SCP Threat [8,327] mean=35.08, median=35.22, P75=37.25, P90=38.59; Foundation Pressure [8,327] mean=16.37, median=16, P75=18, P90=20; Wave Failure [8,327] mean=19.31, median=20, P75=20, P90=20; Time [8,327] mean=8.61, median=10, P75=10, P90=10; Strategic Hazard [8,327] mean=7.5, median=10, P75=10, P90=10
Control cap loss: L3: 135 (ADV=135, CONTROLLED=0, UNCONTROLLED=0, COLLAPSE=0); L4: 2,187 (ADV=1, CONTROLLED=2,186, UNCONTROLLED=0, COLLAPSE=0); L5: 2,224 (ADV=0, CONTROLLED=2, UNCONTROLLED=2,222, COLLAPSE=0)
L4 threshold margin in SERIOUS/CRITICAL states below threshold: 0-4=7614, 5-9=8562, 10-19=10685, 20+=77354

#### Tier D

| Bucket | States | Final L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L3 -> Final below | Theoretical L4 -> Final below | Theoretical L5 -> Final below |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| NORMAL | 37,500 | 55.84% | 43.67% | 0.5% | 0% | 0% | 0% | 55.84% | 43.67% | 0.5% | 0% | 0% | 0% | 0 | 0 | 0 |
| PRESSURED | 37,500 | 6.79% | 68.65% | 24.49% | 0.07% | 0% | 0% | 6.79% | 68.65% | 24.49% | 0.07% | 0% | 0% | 0 | 0 | 0 |
| SERIOUS | 37,500 | 0% | 0.01% | 11.4% | 67.26% | 21.31% | 0.02% | 0% | 0.01% | 11.27% | 64.19% | 24.16% | 0.37% | 47 | 1,200 | 133 |
| CRITICAL | 37,500 | 0% | 0% | 0.13% | 15.51% | 70.33% | 14.02% | 0% | 0% | 0.13% | 15.21% | 68.37% | 16.29% | 0 | 114 | 851 |

进入 L3/L4/L5 的分项贡献统计（按 Final >= 目标等级筛选，mean / median / P75 / P90）：
- Final >= L3: SCP Threat [70,697] mean=30.25, median=30.36, P75=33.69, P90=36.44; Foundation Pressure [70,697] mean=12.15, median=12, P75=16, P90=18; Wave Failure [70,697] mean=15.33, median=15, P75=20, P90=20; Time [70,697] mean=6.64, median=8, P75=10, P90=10; Strategic Hazard [70,697] mean=5.2, median=5, P75=10, P90=10
- Final >= L4: SCP Threat [39,630] mean=32.1, median=32.46, P75=35.19, P90=37.33; Foundation Pressure [39,630] mean=14.38, median=15, P75=16, P90=18; Wave Failure [39,630] mean=17.1, median=17, P75=20, P90=20; Time [39,630] mean=7.5, median=8, P75=10, P90=10; Strategic Hazard [39,630] mean=5.87, median=5, P75=10, P90=10
- Final >= L5: SCP Threat [5,264] mean=34.84, median=35.06, P75=37.2, P90=38.63; Foundation Pressure [5,264] mean=16.79, median=17, P75=18, P90=20; Wave Failure [5,264] mean=19.31, median=20, P75=20, P90=20; Time [5,264] mean=8.91, median=10, P75=10, P90=10; Strategic Hazard [5,264] mean=8.14, median=10, P75=10, P90=10
Control cap loss: L3: 47 (ADV=47, CONTROLLED=0, UNCONTROLLED=0, COLLAPSE=0); L4: 1,314 (ADV=0, CONTROLLED=1,314, UNCONTROLLED=0, COLLAPSE=0); L5: 984 (ADV=0, CONTROLLED=1, UNCONTROLLED=983, COLLAPSE=0)
L4 threshold margin in SERIOUS/CRITICAL states below threshold: 0-4=8970, 5-9=10116, 10-19=12317, 20+=77653

#### Tier C

| Bucket | States | Final L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L3 -> Final below | Theoretical L4 -> Final below | Theoretical L5 -> Final below |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| NORMAL | 37,500 | 66.9% | 33.07% | 0.03% | 0% | 0% | 0% | 66.9% | 33.07% | 0.03% | 0% | 0% | 0% | 0 | 0 | 0 |
| PRESSURED | 37,500 | 16.23% | 68.38% | 15.31% | 0.09% | 0% | 0% | 16.23% | 68.38% | 15.31% | 0.09% | 0% | 0% | 0 | 0 | 0 |
| SERIOUS | 37,500 | 0% | 0.02% | 12.81% | 67.44% | 19.72% | 0.01% | 0% | 0.02% | 12.81% | 67.14% | 19.89% | 0.14% | 0 | 113 | 50 |
| CRITICAL | 37,500 | 0% | 0% | 0.46% | 24.11% | 67.18% | 8.25% | 0% | 0% | 0.46% | 23.9% | 66.42% | 9.22% | 0 | 77 | 363 |

进入 L3/L4/L5 的分项贡献统计（按 Final >= 目标等级筛选，mean / median / P75 / P90）：
- Final >= L3: SCP Threat [70,050] mean=30.29, median=30.39, P75=33.71, P90=36.49; Foundation Pressure [70,050] mean=11.27, median=12, P75=15, P90=18; Wave Failure [70,050] mean=16.26, median=15, P75=20, P90=20; Time [70,050] mean=6.67, median=8, P75=10, P90=10; Strategic Hazard [70,050] mean=5.24, median=5, P75=10, P90=10
- Final >= L4: SCP Threat [35,688] mean=32.45, median=32.78, P75=35.42, P90=37.58; Foundation Pressure [35,688] mean=13.79, median=14, P75=16, P90=18; Wave Failure [35,688] mean=17.39, median=17, P75=20, P90=20; Time [35,688] mean=7.68, median=10, P75=10, P90=10; Strategic Hazard [35,688] mean=6.16, median=5, P75=10, P90=10
- Final >= L5: SCP Threat [3,098] mean=35.4, median=35.71, P75=37.77, P90=38.84; Foundation Pressure [3,098] mean=16.9, median=17, P75=20, P90=20; Wave Failure [3,098] mean=19.44, median=20, P75=20, P90=20; Time [3,098] mean=9.1, median=10, P75=10, P90=10; Strategic Hazard [3,098] mean=8.57, median=10, P75=10, P90=10
Control cap loss: L3: 0 (ADV=0, CONTROLLED=0, UNCONTROLLED=0, COLLAPSE=0); L4: 190 (ADV=0, CONTROLLED=190, UNCONTROLLED=0, COLLAPSE=0); L5: 413 (ADV=0, CONTROLLED=0, UNCONTROLLED=413, COLLAPSE=0)
L4 threshold margin in SERIOUS/CRITICAL states below threshold: 0-4=9999, 5-9=11561, 10-19=14429, 20+=78133

#### Tier B

| Bucket | States | Final L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L3 -> Final below | Theoretical L4 -> Final below | Theoretical L5 -> Final below |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| NORMAL | 37,500 | 76.87% | 23.13% | 0% | 0% | 0% | 0% | 76.87% | 23.13% | 0% | 0% | 0% | 0% | 0 | 0 | 0 |
| PRESSURED | 37,500 | 26.15% | 66.06% | 7.79% | 0% | 0% | 0% | 26.15% | 66.06% | 7.79% | 0% | 0% | 0% | 0 | 0 | 0 |
| SERIOUS | 37,500 | 0% | 0.69% | 27.36% | 63.2% | 8.74% | 0% | 0% | 0.69% | 27.29% | 62.44% | 9.54% | 0.03% | 26 | 312 | 12 |
| CRITICAL | 37,500 | 0% | 0% | 0.6% | 32.71% | 62.46% | 4.23% | 0% | 0% | 0.6% | 32.61% | 62.22% | 4.57% | 0 | 36 | 129 |

进入 L3/L4/L5 的分项贡献统计（按 Final >= 目标等级筛选，mean / median / P75 / P90）：
- Final >= L3: SCP Threat [64,254] mean=30.43, median=30.47, P75=33.33, P90=36.15; Foundation Pressure [64,254] mean=11.56, median=12, P75=15, P90=18; Wave Failure [64,254] mean=15.72, median=15, P75=20, P90=20; Time [64,254] mean=6.82, median=8, P75=10, P90=10; Strategic Hazard [64,254] mean=5.41, median=5, P75=10, P90=10
- Final >= L4: SCP Threat [28,289] mean=32.37, median=32.5, P75=35.09, P90=37.42; Foundation Pressure [28,289] mean=14.35, median=15, P75=17, P90=18; Wave Failure [28,289] mean=17.62, median=20, P75=20, P90=20; Time [28,289] mean=7.92, median=10, P75=10, P90=10; Strategic Hazard [28,289] mean=6.33, median=5, P75=10, P90=10
- Final >= L5: SCP Threat [1,586] mean=35.71, median=36.1, P75=37.99, P90=38.98; Foundation Pressure [1,586] mean=17.32, median=18, P75=20, P90=20; Wave Failure [1,586] mean=19.56, median=20, P75=20, P90=20; Time [1,586] mean=9.29, median=10, P75=10, P90=10; Strategic Hazard [1,586] mean=9.01, median=10, P75=10, P90=10
Control cap loss: L3: 26 (ADV=26, CONTROLLED=0, UNCONTROLLED=0, COLLAPSE=0); L4: 348 (ADV=0, CONTROLLED=348, UNCONTROLLED=0, COLLAPSE=0); L5: 141 (ADV=0, CONTROLLED=0, UNCONTROLLED=141, COLLAPSE=0)
L4 threshold margin in SERIOUS/CRITICAL states below threshold: 0-4=9309, 5-9=11261, 10-19=17823, 20+=82970

#### Tier A

| Bucket | States | Final L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L0 | L1 | L2 | L3 | L4 | L5 | Theoretical L3 -> Final below | Theoretical L4 -> Final below | Theoretical L5 -> Final below |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| NORMAL | 37,500 | 84.98% | 15.02% | 0% | 0% | 0% | 0% | 84.98% | 15.02% | 0% | 0% | 0% | 0% | 0 | 0 | 0 |
| PRESSURED | 37,500 | 35.93% | 60.31% | 3.76% | 0% | 0% | 0% | 35.93% | 60.31% | 3.76% | 0% | 0% | 0% | 0 | 0 | 0 |
| SERIOUS | 37,500 | 0% | 1.31% | 35.39% | 57.99% | 5.31% | 0% | 0% | 1.31% | 35.21% | 57.82% | 5.66% | 0.01% | 70 | 134 | 4 |
| CRITICAL | 37,500 | 0% | 0% | 1.1% | 40.83% | 56.04% | 2.03% | 0% | 0% | 1.1% | 40.76% | 56.04% | 2.1% | 0 | 28 | 26 |

进入 L3/L4/L5 的分项贡献统计（按 Final >= 目标等级筛选，mean / median / P75 / P90）：
- Final >= L3: SCP Threat [60,822] mean=30.62, median=30.62, P75=33.37, P90=35.81; Foundation Pressure [60,822] mean=11.76, median=12, P75=16, P90=18; Wave Failure [60,822] mean=15.93, median=15, P75=20, P90=20; Time [60,822] mean=6.91, median=8, P75=10, P90=10; Strategic Hazard [60,822] mean=5.47, median=5, P75=10, P90=10
- Final >= L4: SCP Threat [23,765] mean=32.54, median=32.63, P75=34.87, P90=37.23; Foundation Pressure [23,765] mean=14.66, median=15, P75=17, P90=18; Wave Failure [23,765] mean=17.83, median=20, P75=20, P90=20; Time [23,765] mean=8.09, median=10, P75=10, P90=10; Strategic Hazard [23,765] mean=6.6, median=5, P75=10, P90=10
- Final >= L5: SCP Threat [760] mean=36.04, median=36.18, P75=38.15, P90=39.27; Foundation Pressure [760] mean=17.69, median=18, P75=20, P90=20; Wave Failure [760] mean=19.75, median=20, P75=20, P90=20; Time [760] mean=9.51, median=10, P75=10, P90=10; Strategic Hazard [760] mean=9.46, median=10, P75=10, P90=10
Control cap loss: L3: 70 (ADV=70, CONTROLLED=0, UNCONTROLLED=0, COLLAPSE=0); L4: 162 (ADV=0, CONTROLLED=162, UNCONTROLLED=0, COLLAPSE=0); L5: 30 (ADV=0, CONTROLLED=0, UNCONTROLLED=30, COLLAPSE=0)
L4 threshold margin in SERIOUS/CRITICAL states below threshold: 0-4=9311, 5-9=11431, 10-19=19554, 20+=85777

### Bottleneck analysis

当前上限结构是 SCP Threat 40、Foundation Pressure 20、Wave Failure 20、Time 10、Strategic Hazard 10。语义表显示高等级需要多个分项同时叠加；单独拉长时间只能贡献最多 10 分，单独 Chaos 或单独普通死亡也不足以稳定跨越 L4。随机桶中的瓶颈以每档 component summary 和 threshold margin 为证据，不把随机比例解释成真实战局概率。

### Tier fairness

同一相对严重度：Foundation 约为开局人口 25%、80% 初始 SCP 存活、一次 catastrophic Foundation wave、15 分钟、无战略风险。

| Tier | Natural | Theoretical | Control | Final |
|---|---:|---:|---|---:|
| E | 38.67 | L2 | CONTROLLED | L2 |
| D | 41 | L2 | CONTROLLED | L2 |
| C | 41 | L2 | CONTROLLED | L2 |
| B | 41.4 | L2 | CONTROLLED | L2 |
| A | 43.33 | L2 | CONTROLLED | L2 |

BASELINE CONCLUSION: NEEDS TUNING REVIEW ONLY。当前模拟可以判断可达性和瓶颈，但不能在没有实服分布/体感数据的情况下直接修改正式阈值。人口档位阈值随 E→A 上升，确实会让同一绝对分数在高人口档位更难跨级；是否符合设计需结合预期‘相对严重度’与真人数据继续决定。

### D-LRC candidate options (PROPOSAL ONLY)

- OPTION A — 只调各档 Threshold：改动最小、保留 100 分制与 ControlState；副作用是对所有组成相同的战况整体平移，不能修复具体分项不足。PROPOSAL ONLY。
- OPTION B — 只调 Foundation Pressure / Reinforcement Failure scale：保留等级语义但改变兵力与波次失败的影响；副作用是直接改变基础战况权重，必须有实服数据。PROPOSAL ONLY。
- OPTION C — 小幅 threshold + 小幅公式调整：最容易同时处理 tier fairness 与高等级可达性，但验证面最大，不能在本轮直接实施。PROPOSAL ONLY。
推荐方向：先采集真人战局，再在 A/B/C 中选择；本轮 PRODUCTION CHANGES APPLIED: NO。
## C. FDI BASELINE

SNAPSHOT MODEL VERIFIED: YES。2026-08-27 快照中的生产实现保留 `PreviousFDI + NewEventDelta`；没有新事件时不会自动恢复，因而是累计失序记忆。首次结算仍是 `InitialBase + CurrentStockAdjustment + Recent120sTransientDelta`。当日 Recovery 未实装。
NO-RECOVERY BEHAVIOR: FDI=80 在 5/10 分钟无新事件时仍为 80；这正是本轮 Order Recovery 分析要解决的设计问题。

## D. FDI RECOVERY MODELS

模型定义：A=Quiet Window；B=State-Gated；C=Band-Aware；D=State + Band。每个模型均只在模拟的正式 30 秒周期上结算；窗口消费使用 LastPositive / LastRecovery 等等价状态，未对生产代码做修改。
- MODEL None: F0 3m=80,5m=80,10m=80,final=80,transitions=0,recoveries=0; F1 3m=80,5m=80,10m=80,final=80,transitions=0,recoveries=0; F2 3m=70,5m=70,10m=70,final=70,transitions=0,recoveries=0; F3 3m=62,5m=62,10m=62,final=62,transitions=0,recoveries=0; F4 3m=66,5m=74,10m=100,final=100,transitions=1,recoveries=0; F5 3m=68,5m=68,10m=78,final=78,transitions=1,recoveries=0; F6 3m=20,5m=20,10m=20,final=20,transitions=0,recoveries=0; F7 3m=45,5m=45,10m=45,final=45,transitions=0,recoveries=0; F8 3m=60,5m=60,10m=60,final=60,transitions=0,recoveries=0
- MODEL QuietWindow: F0 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F1 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F2 3m=66,5m=64,10m=58,final=58,transitions=1,recoveries=6; F3 3m=58,5m=56,10m=50,final=50,transitions=1,recoveries=6; F4 3m=66,5m=74,10m=100,final=100,transitions=1,recoveries=0; F5 3m=66,5m=64,10m=70,final=70,transitions=1,recoveries=4; F6 3m=16,5m=14,10m=8,final=8,transitions=0,recoveries=6; F7 3m=41,5m=39,10m=33,final=33,transitions=0,recoveries=6; F8 3m=60,5m=60,10m=60,final=60,transitions=0,recoveries=0
- MODEL StateGated: F0 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F1 3m=80,5m=80,10m=80,final=80,transitions=0,recoveries=0; F2 3m=70,5m=70,10m=70,final=70,transitions=0,recoveries=0; F3 3m=62,5m=58,10m=52,final=52,transitions=1,recoveries=5; F4 3m=66,5m=74,10m=100,final=100,transitions=1,recoveries=0; F5 3m=66,5m=64,10m=70,final=70,transitions=1,recoveries=4; F6 3m=16,5m=14,10m=8,final=8,transitions=0,recoveries=6; F7 3m=41,5m=39,10m=33,final=33,transitions=0,recoveries=6; F8 3m=60,5m=60,10m=60,final=60,transitions=0,recoveries=0
- MODEL BandAware: F0 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F1 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F2 3m=66,5m=64,10m=58,final=58,transitions=1,recoveries=6; F3 3m=58,5m=57,10m=54,final=54,transitions=1,recoveries=6; F4 3m=66,5m=74,10m=100,final=100,transitions=1,recoveries=0; F5 3m=66,5m=64,10m=70,final=70,transitions=1,recoveries=4; F6 3m=20,5m=20,10m=20,final=20,transitions=0,recoveries=6; F7 3m=43,5m=42,10m=39,final=39,transitions=0,recoveries=6; F8 3m=60,5m=60,10m=60,final=60,transitions=0,recoveries=0
- MODEL StateAndBandAware: F0 3m=76,5m=74,10m=68,final=68,transitions=0,recoveries=6; F1 3m=80,5m=80,10m=80,final=80,transitions=0,recoveries=0; F2 3m=70,5m=70,10m=70,final=70,transitions=0,recoveries=0; F3 3m=62,5m=58,10m=55,final=55,transitions=1,recoveries=5; F4 3m=66,5m=74,10m=100,final=100,transitions=1,recoveries=0; F5 3m=66,5m=64,10m=70,final=70,transitions=1,recoveries=4; F6 3m=20,5m=20,10m=20,final=20,transitions=0,recoveries=6; F7 3m=43,5m=42,10m=39,final=39,transitions=0,recoveries=6; F8 3m=60,5m=60,10m=60,final=60,transitions=0,recoveries=0

## E. FDI FIXED SCENARIOS

F0 HIGH BUT QUIET：所有模型会随窗口下降；A 下降最快，C/D 在进入 MEDIUM 后变慢。F1 BIO Active：B/D 的 state gate 阻止恢复，A/C 仍会恢复，因此 B/D 更符合‘危机仍在不能自然归零’语义。F2 CHAOS STRONG：B/D 被 gate 阻止，A/C 仍恢复。F3 FOUNDATION RECOVERS：固定事件流包含负 Delta，所有允许恢复的模型会叠加已有负 Delta；这证明必须避免同周期过度跳水。F4 REPEATED COMBAT：正向事件持续重置 quiet window，恢复次数和下降幅度应受限。F5 OSCILLATION：参数过激时可能在 30/60 band 附近抖动，需看 sweep 的 transitions。F6/F7：Band-aware 在 LOW 停止或减慢，避免 20→0；F8 DESTROYED：当前建议停止普通设施恢复并由 destroyed 专用规则决定，尚未实装。

## F. FDI PARAMETER SWEEP

扫描参数：QuietWindow 30/60/90/120 秒；RecoveryDelta -1/-2/-3/-4。完整逐组合结果在 `analysis/fdi-recovery-sweep.csv`。以下选择每个模型的 W=90、Delta=-2 作为固定场景对照，所有数值都是 SIMULATED：
- MODEL None: F0 3m=80, 5m=80, 10m=80; F4 transitions=1, final=100。
- MODEL QuietWindow: F0 3m=76, 5m=74, 10m=68; F4 transitions=1, final=100。
- MODEL StateGated: F0 3m=76, 5m=74, 10m=68; F4 transitions=1, final=100。
- MODEL BandAware: F0 3m=76, 5m=74, 10m=68; F4 transitions=1, final=100。
- MODEL StateAndBandAware: F0 3m=76, 5m=74, 10m=68; F4 transitions=1, final=100。

## G. FDI RECOMMENDATION

RECOMMENDED MODEL: MODEL D — STATE-GATED + BAND-AWARE，初始参数范围建议 QuietWindow 60–120s、Delta -1 to -2，且必须只在正式 PERIODIC 成功结算时运行。WHY：它同时阻止 Active crisis / strong Chaos 下的无条件恢复，并在 LOW band 减慢或停止，降低‘危机仍在却快速归零’与 band 抖动风险。该选择只是 PROPOSAL ONLY，未实现正式 Gameplay。
REJECTED / DISFAVORED：MODEL A 在 active crisis 与 strong Chaos 场景仍恢复，语义风险高；MODEL C 忽略状态 gate，虽能抑制 LOW 归零但仍可能在持续危机中下降；过短窗口或 -3/-4 在 sweep 中应视为 too aggressive，若出现大量 band transitions 则拒绝。MODEL B 单独 state gate 较安全，但不能利用当前 band 控制 LOW 的长尾。
PRODUCTION RECOVERY IMPLEMENTED: NO。

## Cross-check

D-LRC 与 FDI 保持独立：模拟没有把 FDI 输入 ResponseScore，也没有让 D-LRC 改变 FDI Recovery。Crisis 只按 Active/Inactive 语义建模，没有重新引入 Crisis Severity。PRODUCTION CHANGES APPLIED: NO。

## Regression and Git

本轮分析 Harness 是独立 `analysis/BalanceAnalysis` 项目，只链接 D-LRC 纯逻辑源码，不进入 EmergencyEvents 正式执行路径。正式回归、Release Build 和 Git 状态由外层命令单独核验；本报告不执行 commit/push。
