# Round Restart Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 强制重启回合时清理 EmergencyEvents 的上一局状态和定时任务，避免新回合继承旧的开局与支援状态。

**Architecture:** 将三项管理器的回合清理顺序收敛到一个无 EXILED 依赖的小型协调器，使其能由纯逻辑测试覆盖。插件订阅 EXILED 的 `RestartingRound` 事件，并在事件到达时调用协调器；普通回合结束继续复用同一清理路径。

**Tech Stack:** C#、.NET Framework 4.8、EXILED 9.14.2、.NET 8 纯逻辑测试控制台。

**Spec:** `docs/superpowers/specs/2026-08-23-dlrc-evaluator-design.md`

## Global Constraints

- 仅修复第三模块的回合生命周期状态清理。
- 不实现第四模块或新增 Crisis、Event Director、O4 功能。
- 不提交或推送 Git。

---

### Task 1: 可测试的重启清理协调器

**Files:**
- Create: `RoundCore/RoundRestartResetter.cs`
- Modify: `Evaluation.Tests/Evaluation.Tests.csproj`
- Modify: `Evaluation.Tests/Program.cs`

**Interfaces:**
- Produces: `RoundRestartResetter.Reset(Action<string>, Action, Action)`，按 D-LRC、Reinforcement、RoundCore 的顺序调用清理。

- [ ] **Step 1: Write the failing test**

```csharp
RoundRestartResetter.Reset(
    reason => calls.Add("DLRC:" + reason),
    () => calls.Add("Reinforcement"),
    () => calls.Add("RoundCore"));
AssertSequence(
    new[] { "DLRC:RestartingRound", "Reinforcement", "RoundCore" },
    calls,
    "强制重启必须按完整顺序清理上一局状态");
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet run --project Evaluation.Tests\\Evaluation.Tests.csproj -c Release --no-restore --nologo`

Expected: 编译失败，因为 `RoundRestartResetter` 尚不存在。

- [ ] **Step 3: Write minimal implementation**

```csharp
public static class RoundRestartResetter
{
    public static void Reset(Action<string> cleanupDlrc, Action cleanupReinforcement, Action cleanupRoundCore)
    {
        cleanupDlrc("RestartingRound");
        cleanupReinforcement();
        cleanupRoundCore();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet run --project Evaluation.Tests\\Evaluation.Tests.csproj -c Release --no-restore --nologo`

Expected: 新测试与既有全部测试通过。

### Task 2: 订阅强制重启事件

**Files:**
- Modify: `Plugin.cs`

**Interfaces:**
- Consumes: `RoundRestartResetter.Reset(Action<string>, Action, Action)`。
- Produces: `ServerEvents.RestartingRound` 的订阅、反订阅与 `OnRestartingRound` 处理器。

- [ ] **Step 1: Route the restart event to the resetter**

```csharp
ServerEvents.RestartingRound += OnRestartingRound;

private void OnRestartingRound()
{
    RoundRestartResetter.Reset(
        reason => dlrcEvaluatorService?.CleanupRound(reason),
        () => reinforcementManager?.CleanupRound(),
        () => roundCoreManager?.CleanupRound());
}
```

- [ ] **Step 2: Build the plugin**

Run: `$env:SCPSL_SERVER_PATH = (Resolve-Path '.test-server').Path; dotnet build .\\EmergencyEvents.csproj -c Release --no-restore --nologo`

Expected: 零错误生成 `EmergencyEvents.dll`。

### Task 3: 部署与现场回归

**Files:**
- Modify: `.test-server/AppData/EXILED/Plugins/EmergencyEvents.dll`

- [ ] **Step 1: Copy the verified DLL into隔离服务器插件目录**

```powershell
Copy-Item -LiteralPath 'bin\\Release\\net48\\EmergencyEvents.dll' -Destination '.test-server\\AppData\\EXILED\\Plugins\\EmergencyEvents.dll' -Force
```

- [ ] **Step 2: Start a fresh round and invoke Remote Admin Restart Round once**

Expected: 日志先记录 `RestartingRound` 清理，再出现新的 `RoundId` 与完整开局记录，且无虚空视角或玩家堆叠。

- [ ] **Step 3: Re-run pure tests and inspect the final server log**

Run: `dotnet run --project Evaluation.Tests\\Evaluation.Tests.csproj -c Release --no-restore --nologo`

Expected: 全部通过，日志无 `ApplyFailed`、`TeleportFailed` 或过期回合状态。
