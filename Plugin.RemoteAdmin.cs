using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RemoteAdminCommands;
using EmergencyEvents.RoundCore;
using EmergencyEvents.Runtime;
using Exiled.API.Features;

namespace EmergencyEvents;

/// <summary>
/// EmergencyEvents 统一 RA 查询和诊断入口。
/// </summary>
public sealed partial class Plugin
{
    internal bool DebugCommandsEnabled => Config.DebugCommandsEnabled;

    internal bool TryExecuteRemoteAdminCommand(
        EmergencyEventsCommandRequest request,
        out string response)
    {
        PluginRuntimeState runtimeState = runtimeCoordinator?.State ?? PluginRuntimeState.ERROR;
        if (!EmergencyEventsCommandGuard.IsAllowed(request.Kind, runtimeState))
        {
            response = FormatInactiveCommandResponse(runtimeState);
            return false;
        }

        return request.Kind switch
        {
            EmergencyEventsCommandKind.Help => TryFormatHelp(request.Target, out response),
            EmergencyEventsCommandKind.Status => TryFormatStatus(out response),
            EmergencyEventsCommandKind.Enable => TryEnableEmergencyEvents(out response),
            EmergencyEventsCommandKind.Disable => TryDisableEmergencyEvents(out response),
            EmergencyEventsCommandKind.Version => TryFormatVersion(out response),
            EmergencyEventsCommandKind.Config => TryFormatConfig(out response),
            EmergencyEventsCommandKind.Health => TryFormatHealth(out response),
            EmergencyEventsCommandKind.Modules => TryFormatModules(out response),
            EmergencyEventsCommandKind.ModuleDetail => TryFormatModuleDetail(request.Target, out response),
            EmergencyEventsCommandKind.Round => TryFormatRound(out response),
            EmergencyEventsCommandKind.WaveState => TryFormatWaveState(out response),
            EmergencyEventsCommandKind.WaveCurrent => TryFormatWaveRecord("当前主支援", reinforcementManager?.State?.MajorWaveHistory.CurrentWave, out response),
            EmergencyEventsCommandKind.WaveLast => TryFormatWaveRecord("最近主支援", reinforcementManager?.State?.MajorWaveHistory.LastMajorWave, out response),
            EmergencyEventsCommandKind.WavePrevious => TryFormatWaveRecord("上一主支援", reinforcementManager?.State?.MajorWaveHistory.PreviousMajorWave, out response),
            EmergencyEventsCommandKind.WaveHistory => TryFormatWaveHistory(request.Number, out response),
            EmergencyEventsCommandKind.WaveHistoryDetail => TryFormatWaveHistoryDetail(request.Target, out response),
            EmergencyEventsCommandKind.WaveTimers => TryFormatWaveTimers(out response),
            EmergencyEventsCommandKind.WaveCap => TryFormatWaveCap(out response),
            EmergencyEventsCommandKind.WaveSurvival => TryFormatWaveSurvival(out response),
            EmergencyEventsCommandKind.DlrcState => TryFormatDlrcState(out response),
            EmergencyEventsCommandKind.DlrcEvaluate => TryEvaluateDlrcAndFormat(out response),
            EmergencyEventsCommandKind.DlrcStage => TryFormatDlrcStage(includeDetails: false, out response),
            EmergencyEventsCommandKind.DlrcStageFull => TryFormatDlrcStage(includeDetails: true, out response),
            EmergencyEventsCommandKind.DlrcStageRaw => TryFormatDlrcRaw(out response),
            EmergencyEventsCommandKind.DlrcBreakdown => TryFormatDlrcBreakdown(out response),
            EmergencyEventsCommandKind.DlrcControl => TryFormatDlrcControl(out response),
            EmergencyEventsCommandKind.DlrcHistory => TryFormatDlrcHistory(request.Number, out response),
            EmergencyEventsCommandKind.DlrcSnapshot => TryFormatDlrcStage(includeDetails: false, out response),
            EmergencyEventsCommandKind.CrisisState => TryFormatCrisisState(out response),
            EmergencyEventsCommandKind.CrisisList => TryFormatCrisisList(out response),
            EmergencyEventsCommandKind.CrisisCheck => TryCheckCrisis(request.Target, isDryRun: false, out response),
            EmergencyEventsCommandKind.DisorderState => TryFormatDisorderState(out response),
            EmergencyEventsCommandKind.DisorderEvents => TryFormatDisorderEvents(out response),
            EmergencyEventsCommandKind.DisorderHistory => TryFormatDisorderHistory(request.Number, out response),
            EmergencyEventsCommandKind.DisorderExplain => TryFormatDisorderExplain(out response),
            EmergencyEventsCommandKind.TestCrisisAll => TryCheckCrisis("all", isDryRun: true, out response),
            EmergencyEventsCommandKind.TestCrisisCheck => TryCheckCrisis(request.Target, isDryRun: true, out response),
            EmergencyEventsCommandKind.TestCrisisBioZombies => TryRunBioSimulation(request.Number ?? 0, out response),
            EmergencyEventsCommandKind.TestCrisisSysTier => TryRunSysSimulation(request.Number ?? 0, out response),
            EmergencyEventsCommandKind.TestCrisisSec => TryRunSecuritySimulation(request.Number ?? 0, request.Flag == true, out response),
            EmergencyEventsCommandKind.TestCrisisWar => TryRunWarSimulation(request.Target, out response),
            EmergencyEventsCommandKind.TestCrisisConCheckpoint => TryRunContainmentCheckpoint(commit: false, out response),
            EmergencyEventsCommandKind.TestCrisisConCheckpointCommit => TryRunContainmentCheckpoint(commit: true, out response),
            EmergencyEventsCommandKind.TestCrisisEndCheck => TryCheckCrisis("end", isDryRun: true, out response),
            EmergencyEventsCommandKind.TestCrisisEndSimulate => TryRunEndSimulation(request.Number ?? 0, out response),
            EmergencyEventsCommandKind.TestDisorderEvent => TryRunDisorderEvent(request.Target, request.Number ?? 0, out response),
            EmergencyEventsCommandKind.Cleanup => TryFormatCleanup(out response),
            EmergencyEventsCommandKind.TestCleanupVerify => TryVerifyCleanup(out response),
            _ => RejectUnknownRequest(out response),
        };
    }

    private bool TryFormatHelp(string target, out string response)
    {
        response = target switch
        {
            "wave" => "【ee wave】\nstate | current | last | previous | history [数量] | history <WaveId> detail | timers | cap | survival",
            "dlrc" => "【ee dlrc】\nstate | evaluate | stage [full|raw] | breakdown | control | snapshot | history [数量]",
            "crisis" => "【ee crisis】\nstate | list | check all|bio|sys|con|sec|goi|war|end",
            "disorder" or "fdi" => "【ee disorder / ee fdi】\nstate | events | history [数量] | explain",
            "test" => "【ee test】\ncrisis ... | disorder event mtf-loss <数量> | cleanup verify",
            _ => "【EmergencyEvents】\nstatus | enable / disable | modules | round | wave | dlrc | crisis | disorder | health | config | version | test\n使用 ee help <wave|dlrc|crisis|disorder|test> 查看子命令。",
        };
        return true;
    }

    private bool TryFormatStatus(out string response)
    {
        StringBuilder builder = new StringBuilder();
        PluginRuntimeCoordinator? runtime = runtimeCoordinator;
        builder.AppendLine("【EmergencyEvents 状态】");
        builder.AppendLine($"运行状态：{runtime?.State.ToString() ?? "ERROR"}");
        builder.AppendLine($"回合时间：{FormatRoundElapsed()}");
        builder.AppendLine("人口：");
        builder.AppendLine($"开局：{runtime?.RoundStartPopulation ?? 0}");
        builder.AppendLine($"当前：{runtime?.CurrentPopulation ?? 0}");
        builder.AppendLine($"人口编制：{roundCoreManager?.State?.Resolution.Tier.ToString() ?? "暂无"}");
        builder.AppendLine($"最低要求：{runtime?.MinimumPlayers ?? Config.MinimumPlayers}");
        builder.AppendLine("模块：");
        AppendModuleSummary(builder, "M01 回合核心", Config.RoundCoreEnabled, roundCoreManager?.State is not null);
        AppendModuleSummary(builder, "M02 支援整合", Config.ReinforcementEnabled, reinforcementManager?.IsRoundActive == true);
        AppendModuleSummary(builder, "M03 D-LRC", Config.DlrcEvaluatorEnabled, dlrcEvaluatorService?.IsActive == true);
        AppendModuleSummary(builder, "M04 危机系统", Config.CrisisSystemEnabled, crisisManager is not null && runtime?.IsEmergencyEventsActiveForRound == true);
        AppendModuleSummary(builder, "M04.5 Facility Disorder", Config.FacilityDisorder.Enabled, facilityDisorderManager?.State.IsActive == true);
        builder.AppendLine("M05 事件导演：未实现");
        AppendCurrentDlrcSummary(builder);
        AppendLatestWaveSummary(builder);
        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryEnableEmergencyEvents(out string response)
    {
        if (runtimeCoordinator is null)
        {
            response = "EmergencyEvents 运行时尚未初始化。";
            return false;
        }

        bool appliedNow = runtimeCoordinator.Enable(runtimeCoordinator.IsRoundInProgress);
        response = appliedNow
            ? "EmergencyEvents 已启用，等待有效回合开始后介入。"
            : "EmergencyEvents 已启用，将于下一回合完整生效；当前回合不会重新执行 Round Core。";
        return true;
    }

    private bool TryDisableEmergencyEvents(out string response)
    {
        if (runtimeCoordinator is null)
        {
            response = "EmergencyEvents 运行时尚未初始化。";
            return false;
        }

        runtimeCoordinator.Disable();
        SuspendEmergencyEventsForRound("AdminDisabled");
        response = "EmergencyEvents 已禁用；插件 DLL 保持加载，本回合后续 EE 干预和调度已停止。";
        return true;
    }

    private bool TryFormatVersion(out string response)
    {
        response = $"【EmergencyEvents 版本】\n插件版本：{Version}\n目标框架：.NET Framework 4.8\nEXILED 要求版本：{RequiredExiledVersion}\n构建信息：运行时未嵌入 Git 信息";
        return true;
    }

    private bool TryFormatConfig(out string response)
    {
        PrimaryWaveCaps caps = Config.PrimaryWaveCaps ?? new PrimaryWaveCaps();
        FacilityDisorderConfig disorder = Config.FacilityDisorder;
        response = $"【EmergencyEvents 配置】\nMinimumPlayers={Config.MinimumPlayers}\nWaveCaps：E{caps.E} D{caps.D} C{caps.C} B{caps.B} A{caps.A}\nTimer Extension：刷新方 +{Config.SpawningFactionTimerExtensionSeconds} 秒；另一方 +{Config.OpposingFactionTimerExtensionSeconds} 秒\nD-LRC：开始时间 {FormatSeconds(Config.DlrcEvaluatorStartTimeSeconds)}；周期 {Config.DlrcEvaluatorIntervalSeconds} 秒\n危机：CON 检查 {Config.CrisisContainmentCheckpointSeconds} 秒；END 激活={Config.CrisisEndActivationSeconds} 秒\nFDI：Enabled={disorder.Enabled}; InitialBase={disorder.InitialBase:0.##}; Lookback={disorder.InitialLookbackSeconds}s; SettlementHistoryCapacity={disorder.SettlementHistoryCapacity}; EventHistoryCapacity={disorder.EventHistoryCapacity}; Bands={disorder.LowMaximum:0.##}/{disorder.MediumMaximum:0.##}/{disorder.HighMinimum:0.##}\n此命令只读，不支持 RA 修改配置。";
        return true;
    }

    private bool TryFormatHealth(out string response)
    {
        response = $"【EmergencyEvents 健康检查】\nRuntimeState：{runtimeCoordinator?.State.ToString() ?? "ERROR"}\nRoundContextValid：{FormatBoolean(roundCoreManager?.State is not null)}\nM03 Running：{FormatBoolean(dlrcEvaluatorService?.IsActive == true)}；Busy：{FormatBoolean(dlrcEvaluatorService?.IsEvaluating == true)}；QueuedManualEvaluation：{FormatBoolean(dlrcEvaluatorService?.HasQueuedManualEvaluation == true)}；LastEvaluationValid：{FormatBoolean(dlrcEvaluatorService?.LastResult?.IsValid == true)}\nM04 LastAssessmentValid：{FormatBoolean(crisisManager?.CurrentCrisisAssessment is not null)}\nM04.5 FDI Running：{FormatBoolean(facilityDisorderManager?.State.IsActive == true)}；Suspended：{FormatBoolean(facilityDisorderManager?.State.IsSuspended == true)}；EventCount：{facilityDisorderManager?.Events.Count ?? 0}\nM02 Running：{FormatBoolean(reinforcementManager?.IsRoundActive == true)}；Mini-Wave Interceptor：{FormatBoolean(Config.DisableMiniWaves)}；WaveHistoryCount：{reinforcementManager?.GetMajorWaveRecords().Count ?? 0}\n最近错误数：暂无集中计数\n最近警告数：暂无集中计数";
        return true;
    }

    private bool TryFormatModules(out string response)
    {
        StringBuilder builder = new StringBuilder("【EmergencyEvents 模块】\n");
        AppendModuleSummary(builder, "M01 Round Core", Config.RoundCoreEnabled, roundCoreManager?.State is not null);
        AppendModuleSummary(builder, "M02 Reinforcement", Config.ReinforcementEnabled, reinforcementManager?.IsRoundActive == true);
        AppendModuleSummary(builder, "M03 D-LRC", Config.DlrcEvaluatorEnabled, dlrcEvaluatorService?.IsActive == true);
        AppendModuleSummary(builder, "M04 Crisis", Config.CrisisSystemEnabled, crisisManager is not null && runtimeCoordinator?.IsEmergencyEventsActiveForRound == true);
        AppendModuleSummary(builder, "M04.5 Facility Disorder", Config.FacilityDisorder.Enabled, facilityDisorderManager?.State.IsActive == true);
        builder.AppendLine("M05 Director：FRAMEWORK_READY / PRODUCTION_DISABLED");
        builder.AppendLine("M06 O4 Panel：DEFERRED_BY_DESIGN");
        builder.Append("Formal Event Packs：NOT_STARTED");
        response = builder.ToString();
        return true;
    }

    private bool TryFormatModuleDetail(string target, out string response)
    {
        string normalized = target?.Trim().ToLowerInvariant() ?? string.Empty;
        response = normalized switch
        {
            "round" or "roundcore" or "m01" => $"【M01 Round Core】\n状态：{GetModuleState(Config.RoundCoreEnabled, roundCoreManager?.State is not null)}\n锁定回合：{roundCoreManager?.State?.RoundId.ToString() ?? "暂无"}\n开局人数：{roundCoreManager?.State?.StartPopulation.ToString() ?? "暂无"}",
            "reinforcement" or "wave" or "m02" => $"【M02 Reinforcement】\n状态：{GetModuleState(Config.ReinforcementEnabled, reinforcementManager?.IsRoundActive == true)}\nMini-Wave 禁用：{FormatBoolean(Config.DisableMiniWaves)}\nWaveHistory：{reinforcementManager?.GetMajorWaveRecords().Count ?? 0}",
            "dlrc" or "m03" => $"【M03 D-LRC】\n状态：{GetModuleState(Config.DlrcEvaluatorEnabled, dlrcEvaluatorService?.IsActive == true)}\n正在评估：{FormatBoolean(dlrcEvaluatorService?.IsEvaluating == true)}\n最后触发：{dlrcEvaluatorService?.LastTrigger.ToString() ?? "暂无"}",
            "crisis" or "m04" => $"【M04 Crisis】\n状态：{GetModuleState(Config.CrisisSystemEnabled, crisisManager is not null && runtimeCoordinator?.IsEmergencyEventsActiveForRound == true)}\n最近危机结果：{crisisManager?.CurrentCrisisAssessment?.Code ?? "暂无"}\nWAR：{FormatWarModuleState()}",
            "disorder" or "fdi" or "m045" => $"【M04.5 Facility Disorder】\n状态：{GetModuleState(Config.FacilityDisorder.Enabled, facilityDisorderManager?.State.IsActive == true)}\n当前值：{facilityDisorderManager?.State.CurrentFacilityDisorder:0.##}\n区间：{facilityDisorderManager?.State.DisorderBand}\n最近结算：{FormatNullableTime(facilityDisorderManager?.State.LastSettlementAt)}",
            _ => "未知模块。使用 ee module <round|reinforcement|dlrc|crisis|disorder>。",
        };
        return normalized is "round" or "roundcore" or "m01" or "reinforcement" or "wave" or "m02" or "dlrc" or "m03" or "crisis" or "m04" or "disorder" or "fdi" or "m045";
    }

    private bool TryFormatRound(out string response)
    {
        PluginRuntimeCoordinator? runtime = runtimeCoordinator;
        RoundCoreState? state = roundCoreManager?.State;
        RoundComposition? composition = state?.Resolution.Composition;
        response = $"【回合状态】\n回合编号：{state?.RoundId.ToString() ?? "暂无"}\n回合时间：{FormatRoundElapsed()}\n开局人数：{runtime?.RoundStartPopulation ?? 0}\n当前人数：{runtime?.CurrentPopulation ?? 0}\n锁定人口编制：{state?.Resolution.Tier.ToString() ?? "暂无"}\nEE 本局启用：{FormatBoolean(runtime?.IsEmergencyEventsActiveForRound == true)}\n低人口暂停：{FormatBoolean(runtime?.WasLowPopulationSuspended == true)}\nSCP 数量：{composition?.ScpCount.ToString() ?? "暂无"}\n安保数量：{composition?.SecurityCount.ToString() ?? "暂无"}\n混沌渗透者数量：{composition?.ChaosInfiltratorCount.ToString() ?? "暂无"}";
        return true;
    }

    private bool TryFormatWaveState(out string response)
    {
        ReinforcementState? state = reinforcementManager?.State;
        PrimaryWaveCaps caps = Config.PrimaryWaveCaps ?? new PrimaryWaveCaps();
        response = $"【支援波状态】\n状态：{GetModuleState(Config.ReinforcementEnabled, reinforcementManager?.IsRoundActive == true)}\n锁定档位：{state?.LockedPopulationTier.ToString() ?? "暂无"}\n当前 Primary Wave Cap：{(state is null ? "暂无" : caps.GetCap(state.LockedPopulationTier).ToString())}\n完整上限：E={caps.E} D={caps.D} C={caps.C} B={caps.B} A={caps.A}\nMini-Wave 禁用：{FormatBoolean(Config.DisableMiniWaves)}\nCurrentWave：{state?.MajorWaveHistory.CurrentWave?.WaveId ?? "暂无"}\nLastMajorWave：{state?.MajorWaveHistory.LastMajorWave?.WaveId ?? "暂无"}\nPreviousMajorWave：{state?.MajorWaveHistory.PreviousMajorWave?.WaveId ?? "暂无"}";
        return true;
    }

    private bool TryFormatWaveRecord(string title, MajorWaveRecord? record, out string response)
    {
        if (record is null)
        {
            response = $"【{title}】\n暂无已完成 Primary Wave 记录。";
            return true;
        }

        response = FormatWaveRecord(title, record, includeDetail: false);
        return true;
    }

    private bool TryFormatWaveHistory(int? requestedCount, out string response)
    {
        int count = Math.Min(Math.Max(requestedCount ?? 5, 1), 20);
        List<MajorWaveRecord> records = reinforcementManager?.GetMajorWaveRecords()
            .OrderByDescending(record => record.CompletedAt)
            .Take(count)
            .ToList()
            ?? new List<MajorWaveRecord>();
        StringBuilder builder = new StringBuilder($"【主支援历史】最近 {count} 波\n");
        if (records.Count == 0)
        {
            builder.Append("暂无记录。");
        }
        else
        {
            foreach (MajorWaveRecord record in records)
            {
                builder.AppendLine($"{record.WaveId} | {record.Faction} | 刷新 {record.ActualSpawnedCount} 人 | 120秒成熟：{FormatBoolean(record.IsSurvivalObservationComplete)} | 存活：{(record.IsSurvivalObservationComplete ? record.SurvivingCountAtObservation.ToString() : "待观察")}");
            }
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatWaveHistoryDetail(string waveId, out string response)
    {
        MajorWaveRecord? record = reinforcementManager?.GetMajorWaveRecords()
            .FirstOrDefault(item => string.Equals(item.WaveId, waveId, StringComparison.OrdinalIgnoreCase));
        if (record is null)
        {
            response = $"未找到 WaveId={waveId} 的主支援记录。";
            return false;
        }

        response = FormatWaveRecord("主支援详情", record, includeDetail: true);
        return true;
    }

    private bool TryFormatWaveTimers(out string response)
    {
        if (reinforcementManager?.TryGetPrimaryTimerSeconds(out double? foundation, out double? chaos) != true)
        {
            response = "【支援计时器】\n当前原版 Primary Wave 计时器 API 未提供可读数据。";
            return true;
        }

        response = $"【支援计时器】\n基金会刷新计时器：{FormatSeconds(foundation)}\n混沌刷新计时器：{FormatSeconds(chaos)}\n最近一次 Extension：刷新方 +{Config.SpawningFactionTimerExtensionSeconds} 秒；另一方 +{Config.OpposingFactionTimerExtensionSeconds} 秒\n此命令只读，不修改原版 Timer。";
        return true;
    }

    private bool TryFormatWaveCap(out string response)
    {
        PrimaryWaveCaps caps = Config.PrimaryWaveCaps ?? new PrimaryWaveCaps();
        response = $"【Primary Wave 人数上限】\nE ≤ {caps.E}\nD ≤ {caps.D}\nC ≤ {caps.C}\nB ≤ {caps.B}\nA ≤ {caps.A}\n规则：只截断原版已选择人数，不主动扩充原版波次。";
        return true;
    }

    private bool TryFormatWaveSurvival(out string response)
    {
        List<MajorWaveRecord> records = reinforcementManager?.GetMajorWaveRecords()
            .OrderByDescending(record => record.CompletedAt)
            .ToList()
            ?? new List<MajorWaveRecord>();
        StringBuilder builder = new StringBuilder("【支援波 120 秒存活】\n");
        if (records.Count == 0)
        {
            builder.Append("暂无记录。");
        }
        else
        {
            foreach (MajorWaveRecord record in records)
            {
                string survival = record.IsSurvivalObservationComplete
                    ? $"{record.SurvivingCountAtObservation} / {record.ActualSpawnedCount}"
                    : "尚未成熟";
                builder.AppendLine($"{record.WaveId}：{survival}");
            }
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatDlrcState(out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        CrisisAssessment? assessment = crisisManager?.CurrentCrisisAssessment;
        string code = GetDisplayCode(result!, assessment, out string crisisNote);
        response = $"【D-LRC 当前状态】\n完整代码：{code}\n人口编制：{result!.PopulationTier}\n响应分数：{result.EffectiveResponseScore:0.##} / 100\n理论响应级别：{result.TheoreticalLevel}\n最终响应级别：{result.FinalLevel}\n控制状态：{DlrcStageReportFormatter.FormatControlState(result.ControlState)}\n最近评估：{FormatRoundTime(result.Timestamp)}\n触发：{FormatTrigger(dlrcEvaluatorService?.LastTrigger)}{FormatOptionalLine("危机关联", crisisNote)}";
        return true;
    }

    private bool TryEvaluateDlrcAndFormat(out string response)
    {
        if (!TryEvaluateDlrcImmediately(out DlrcEvaluationResult? result, out string serviceResponse))
        {
            response = serviceResponse;
            return false;
        }

        if (result is null)
        {
            response = serviceResponse;
            return true;
        }

        CrisisAssessment? assessment = crisisManager?.CurrentCrisisAssessment;
        string code = GetDisplayCode(result, assessment, out string crisisNote);
        ResponseBreakdown score = result.ResponseBreakdown;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("【D-LRC 手动评估完成】");
        builder.AppendLine($"当前代码：{code}");
        builder.AppendLine($"人口编制：{result.PopulationTier}");
        builder.AppendLine($"响应分数：{result.EffectiveResponseScore:0.##} / 100");
        builder.AppendLine($"理论响应级别：{result.TheoreticalLevel}");
        builder.AppendLine($"最终响应级别：{result.FinalLevel}");
        builder.AppendLine($"控制状态：{DlrcStageReportFormatter.FormatControlState(result.ControlState)}");
        builder.AppendLine("—— 响应分数 ——");
        builder.AppendLine($"SCP威胁度：{score.ScpThreatTotal:0.##} / 40");
        builder.AppendLine($"基金会压力：{score.FoundationPressureTotal:0.##} / 20");
        builder.AppendLine($"支援失效度：{score.ReinforcementFailure:0.##} / 20");
        builder.AppendLine($"时间压力：{score.TimePressure:0.##} / 10");
        builder.AppendLine($"战略危险度：{score.StrategicHazard:0.##} / 10");
        AppendCrisisSummary(builder, assessment, result);
        builder.AppendLine("触发方式：RA手动评估");
        builder.AppendLine($"评估时间：{FormatRoundTime(result.Timestamp)}");
        if (!string.IsNullOrEmpty(crisisNote))
        {
            builder.AppendLine($"危机关联：{crisisNote}");
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatDlrcStage(bool includeDetails, out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        response = includeDetails
            ? DlrcStageReportFormatter.FormatFull(snapshot!, result!, crisisManager?.CurrentCrisisAssessment)
            : DlrcStageReportFormatter.FormatStandard(snapshot!, result!, crisisManager?.CurrentCrisisAssessment);
        return true;
    }

    private bool TryFormatDlrcRaw(out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        response = DlrcStateReportFormatter.Format(snapshot!, result!, crisisManager?.CurrentCrisisAssessment);
        return true;
    }

    private bool TryFormatDlrcBreakdown(out string response)
    {
        if (!TryGetLatestDlrc(out _, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        ResponseBreakdown score = result!.ResponseBreakdown;
        response = $"【D-LRC 响应分数明细】\nSCP存续压力：{score.ScpPresence:0.##}\nSCP生命压力：{score.ScpHealth:0.##}\n049-2压力：{score.ZombiePressure:0.##}\n079系统压力：{score.Scp079Pressure:0.##}\nSCP威胁度：{score.ScpThreatTotal:0.##}\n战斗压力：{score.CombatPressure:0.##}\n观察者压力：{score.SpectatorPressure:0.##}\n基金会压力：{score.FoundationPressureTotal:0.##}\n支援失效度：{score.ReinforcementFailure:0.##}\n时间压力：{score.TimePressure:0.##}\n战略危险度：{score.StrategicHazard:0.##}\n最终响应分数：{result.EffectiveResponseScore:0.##}";
        return true;
    }

    private bool TryFormatDlrcControl(out string response)
    {
        if (!TryGetLatestDlrc(out _, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        ControlAssessment control = result!.ControlAssessment;
        response = $"【D-LRC 控制评估】\n控制状态：{DlrcStageReportFormatter.FormatControlState(control.ControlState)}\n威胁趋势：{FormatThreatTrend(control.ThreatTrend)}\n基金会强度：{FormatFoundationStrength(control.FoundationStrength)}\n支援表现：{FormatWavePerformance(control.WavePerformance)}\n战场动量：{FormatMomentum(control.BattlefieldMomentum)}\n正向信号：{control.PositiveSignals}\n负向信号：{control.NegativeSignals}\n等级上限：{control.ControlLevelCap}";
        return true;
    }

    private bool TryFormatDlrcHistory(int? requestedCount, out string response)
    {
        int count = Math.Min(Math.Max(requestedCount ?? 5, 1), 20);
        List<DlrcEvaluationResult> items = dlrcEvaluatorService?.History.Items
            .OrderByDescending(item => item.Timestamp)
            .Take(count)
            .ToList()
            ?? new List<DlrcEvaluationResult>();
        StringBuilder builder = new StringBuilder($"【D-LRC 历史】最近 {count} 次\n");
        if (items.Count == 0)
        {
            builder.Append("暂无有效评估。");
        }
        else
        {
            foreach (DlrcEvaluationResult item in items)
            {
                builder.AppendLine($"{FormatRoundTime(item.Timestamp)} | {item.Code} | 分数 {item.EffectiveResponseScore:0.##} | 最终等级 {item.FinalLevel}");
            }
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatCrisisState(out string response)
    {
        CrisisAssessment? assessment = crisisManager?.CurrentCrisisAssessment;
        if (assessment is null)
        {
            response = "【危机状态】\n危机系统尚无本回合有效评估。";
            return true;
        }

        StringBuilder builder = new StringBuilder("【危机状态】\n");
        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            if (!assessment.Detections.TryGetValue(tag, out CrisisDetectionResult? detection))
            {
                builder.AppendLine($"{DlrcStageReportFormatter.FormatCrisisTag(tag)}：数据不足");
                continue;
            }

            string state = detection.IsActive ? "激活" : "未激活";
            builder.AppendLine($"{DlrcStageReportFormatter.FormatCrisisTag(tag)}：{state}");
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private string FormatWarModuleState()
    {
        CrisisDetectionResult? detection = crisisManager?.CurrentCrisisAssessment is CrisisAssessment assessment
            && assessment.Detections.TryGetValue(CrisisTag.WAR, out CrisisDetectionResult? warDetection)
            ? warDetection
            : null;
        return detection is null
            ? "暂无"
            : detection.IsActive
                ? "激活"
                : "未激活";
    }

    private bool TryFormatCrisisList(out string response)
    {
        response = "【危机列表】\n生化危机（BIO）\n系统危机（SYS）\n收容危机（CON）\n安全危机（SEC）\nGOI危机（GOI）\n核危机（WAR）\n终局危机（END）";
        return true;
    }

    private bool TryCheckCrisis(string target, bool isDryRun, out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        if (crisisManager is null)
        {
            response = "Crisis System 当前未启用，无法执行正式检测。";
            return false;
        }

        return TryDiagnoseCrisisTargets(target, snapshot!, result!, isDryRun, out response);
    }

    private bool TryRunBioSimulation(int zombieCount, out string response)
    {
        return TryRunSimulation(
            CrisisTag.BIO,
            snapshot => CrisisDiagnosticSnapshotFactory.WithZombieCount(snapshot, zombieCount),
            $"BIO 模拟 049-2={zombieCount}",
            out response);
    }

    private bool TryRunSysSimulation(int tier, out string response)
    {
        return TryRunSimulation(
            CrisisTag.SYS,
            snapshot => CrisisDiagnosticSnapshotFactory.WithScp079Tier(snapshot, tier),
            $"SYS 模拟 SCP-079 等级={tier}",
            out response);
    }

    private bool TryRunSecuritySimulation(int foundation, bool hostile, out string response)
    {
        return TryRunSimulation(
            CrisisTag.SEC,
            snapshot => CrisisDiagnosticSnapshotFactory.WithSecurityFacts(snapshot, foundation, hostile),
            $"SEC 模拟 基金会={foundation}，敌对威胁={FormatBoolean(hostile)}",
            out response);
    }

    private bool TryRunWarSimulation(string state, out string response)
    {
        return TryRunSimulation(
            CrisisTag.WAR,
            snapshot => CrisisDiagnosticSnapshotFactory.WithWarheadState(snapshot, state),
            $"WAR 模拟核弹状态={state}",
            out response);
    }

    private bool TryRunContainmentCheckpoint(bool commit, out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out DlrcEvaluationResult? result, out response))
        {
            return false;
        }

        if (crisisManager is null)
        {
            response = "Crisis System 当前未启用，无法执行 CON 检查。";
            return false;
        }

        if (!crisisManager.TryRunContainmentCheckpoint(snapshot!, result!, commit, out CrisisDetectionResult? detection))
        {
            response = "CON 尚无可用正式基线；请等待第二个 Primary Wave 完成并至少产生一次正式危机评估。";
            return false;
        }

        response = FormatCrisisDetection(
            detection!,
            isDryRun: !commit,
            prefix: commit ? "DEBUG STATE MUTATION：CON 检查点已提交。" : "CON 检查点 Dry Run。" );
        return true;
    }

    private bool TryRunEndSimulation(int seconds, out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out _, out response))
        {
            return false;
        }

        if (crisisManager is null)
        {
            response = "Crisis System 当前未启用，无法执行 END 模拟。";
            return false;
        }

        RoundSnapshot simulated = CrisisDiagnosticSnapshotFactory.WithEndStalemate(snapshot!);
        DlrcEvaluationResult simulatedResult = DlrcEvaluator.Evaluate(
            simulated,
            new EvaluationHistory(),
            EvaluationOptions.Default);
        if (!crisisManager.TryDiagnoseEndSimulation(simulated, simulatedResult, seconds, out CrisisDetectionResult? detection))
        {
            response = "END 模拟无法构造有效的核爆后地表僵持输入。";
            return false;
        }

        response = FormatCrisisDetection(detection!, isDryRun: true, prefix: $"END 模拟连续地表僵持 {seconds} 秒。");
        return true;
    }

    private bool TryFormatCleanup(out string response)
    {
        response = $"【Cleanup 状态】\nCurrentRoundId：{roundCoreManager?.State?.RoundId.ToString() ?? "暂无"}\nCurrentWave：{reinforcementManager?.State?.MajorWaveHistory.CurrentWave?.WaveId ?? "暂无"}\nWaveHistoryCount：{reinforcementManager?.GetMajorWaveRecords().Count ?? 0}\nEvaluationHistoryCount：{dlrcEvaluatorService?.History.Count ?? 0}\nCrisisState：{crisisManager?.CurrentCrisisAssessment?.Code ?? "暂无"}\nFDI：Active={FormatBoolean(facilityDisorderManager?.State.IsActive == true)}；Events={facilityDisorderManager?.Events.Count ?? 0}；History={facilityDisorderManager?.History.Count ?? 0}\nPendingCoroutines：M02={reinforcementManager?.State?.ScheduledHandles.Count ?? 0}；M03={FormatBoolean(dlrcEvaluatorService?.HasScheduledEvaluation == true)}\n此命令只查询，不执行强制清理。";
        return true;
    }

    private bool TryFormatDisorderState(out string response)
    {
        FacilityDisorderRuntimeManager? manager = facilityDisorderManager;
        if (manager is null)
        {
            response = "FDI 运行时尚未初始化。";
            return false;
        }

        FacilityDisorderState state = manager.State;
        CrisisAssessment? assessment = crisisManager?.CurrentCrisisAssessment;
        response = $"【Facility Disorder 当前状态】\n状态：{GetModuleState(Config.FacilityDisorder.Enabled, state.IsActive)}\n本回合暂停：{FormatBoolean(state.IsSuspended)}\n当前 Facility Disorder：{state.CurrentFacilityDisorder:0.##} / 100\n区间：{state.DisorderBand}\n最近处理时间：{FormatNullableTime(state.LastProcessedAt)}\n最近结算：{FormatNullableTime(state.LastSettlementAt)}\n事件数：{manager.Events.Count}\n结算次数：{manager.History.Count}\n当前 D-LRC：{CurrentDlrcResult?.Code ?? "暂无"}\n当前 Crisis：{assessment?.Code ?? "暂无"}\n说明：FDI 只在正常 PERIODIC 完成并经过 Crisis 评估后结算。";
        return true;
    }

    private bool TryFormatDisorderEvents(out string response)
    {
        FacilityDisorderRuntimeManager? manager = facilityDisorderManager;
        if (manager is null)
        {
            response = "FDI 运行时尚未初始化。";
            return false;
        }

        StringBuilder builder = new StringBuilder("【Facility Disorder 事件】\n");
        foreach (DisorderEvent disorderEvent in manager.Events.OrderByDescending(item => item.Timestamp).Take(20))
        {
            builder.AppendLine($"{disorderEvent.Timestamp:O} | {disorderEvent.Category} | Δ={disorderEvent.Delta:0.####} | {disorderEvent.EventId} | {disorderEvent.Description}");
        }

        if (manager.Events.Count == 0)
        {
            builder.Append("暂无事件。 ");
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatDisorderHistory(int? requestedCount, out string response)
    {
        FacilityDisorderRuntimeManager? manager = facilityDisorderManager;
        if (manager is null)
        {
            response = "FDI 运行时尚未初始化。";
            return false;
        }

        int count = Math.Min(Math.Max(requestedCount ?? 5, 1), 20);
        StringBuilder builder = new StringBuilder($"【Facility Disorder 结算历史】最近 {count} 次\n");
        foreach (FacilityDisorderSettlement settlement in manager.History.Reverse().Take(count))
        {
            builder.AppendLine($"{settlement.WindowEnd:O} | Window={settlement.WindowStart:O}→{settlement.WindowEnd:O} | {settlement.PreviousValue:0.##} + {settlement.Delta:0.##} = {settlement.CurrentValue:0.##} | Events={settlement.ProcessedEvents.Count}");
        }

        if (manager.History.Count == 0)
        {
            builder.Append("暂无 PERIODIC 结算。 ");
        }

        response = builder.ToString().TrimEnd();
        return true;
    }

    private bool TryFormatDisorderExplain(out string response)
    {
        FacilityDisorderConfig config = Config.FacilityDisorder;
        response = $"【Facility Disorder 规则】\n范围：{config.LowMinimum:0.##}–{config.HighMaximum:0.##}；LOW < {config.MediumMinimum:0.##}；MEDIUM < {config.HighMinimum:0.##}；HIGH ≥ {config.HighMinimum:0.##}\n首次：InitialBase={config.InitialBase:0.##} + 首次评估前 {config.InitialLookbackSeconds} 秒有效事件\n后续：严格处理 [LastProcessedAt, PeriodicTimestamp] 的新增事实，不使用 Now-30 秒窗口\n结算：只允许 PERIODIC；POST_MAJOR_WAVE、MANUAL、MANUAL_RA 只读\n原则：不改变 ResponseScore、Natural/Effective、Control、FinalLevel 或 Crisis Active/Inactive\n权重状态：{(config.IsProvisionalBalance ? "临时平衡值" : "正式平衡值")}。";
        return true;
    }

    private bool TryRunDisorderEvent(string eventName, int amount, out string response)
    {
        if (facilityDisorderManager is null)
        {
            response = "FDI 运行时尚未初始化。";
            return false;
        }

        return facilityDisorderManager.TryDryRunEvent(eventName, amount, out response);
    }

    private bool TryVerifyCleanup(out string response)
    {
        bool isClean = roundCoreManager?.State is null
            && (reinforcementManager?.GetMajorWaveRecords().Count ?? 0) == 0
            && (dlrcEvaluatorService?.History.Count ?? 0) == 0
            && crisisManager?.CurrentCrisisAssessment is null
            && (facilityDisorderManager?.Events.Count ?? 0) == 0
            && (facilityDisorderManager?.History.Count ?? 0) == 0
            && facilityDisorderManager?.State.IsInitialized != true;
        response = $"【Cleanup Verify】\nDRY RUN：是\n当前回合状态残留：{(isClean ? "未发现" : "存在")}";
        return isClean;
    }

    private bool TryRunSimulation(
        CrisisTag tag,
        Func<RoundSnapshot, RoundSnapshot> simulation,
        string prefix,
        out string response)
    {
        if (!TryGetLatestDlrc(out RoundSnapshot? snapshot, out _, out response))
        {
            return false;
        }

        if (crisisManager is null)
        {
            response = "Crisis System 当前未启用，无法执行 Dry Run。";
            return false;
        }

        RoundSnapshot simulated = simulation(snapshot!);
        DlrcEvaluationResult simulatedResult = DlrcEvaluator.Evaluate(
            simulated,
            new EvaluationHistory(),
            EvaluationOptions.Default);
        if (!crisisManager.TryDiagnose(tag, simulated, simulatedResult, out CrisisDetectionResult? detection))
        {
            response = $"{prefix}\n该危机当前未实现或不可诊断。";
            return false;
        }

        response = FormatCrisisDetection(detection!, isDryRun: true, prefix: prefix);
        return true;
    }

    private bool TryDiagnoseCrisisTargets(
        string target,
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        bool isDryRun,
        out string response)
    {
        List<CrisisTag> tags = target == "all"
            ? Enum.GetValues(typeof(CrisisTag)).Cast<CrisisTag>().ToList()
            : TryParseCrisisTag(target, out CrisisTag parsed) ? new List<CrisisTag> { parsed } : new List<CrisisTag>();
        if (tags.Count == 0)
        {
            response = "未知危机标签。使用 bio、sys、con、sec、goi、war、end 或 all。";
            return false;
        }

        StringBuilder builder = new StringBuilder(isDryRun ? "【危机 Dry Run】\nDRY RUN：是\n" : "【危机正式检测】\n");
        bool succeeded = true;
        foreach (CrisisTag tag in tags)
        {
            if (!crisisManager!.TryDiagnose(tag, snapshot, result, out CrisisDetectionResult? detection))
            {
                builder.AppendLine($"{DlrcStageReportFormatter.FormatCrisisTag(tag)}：未实现或不可诊断。");
                succeeded = false;
                continue;
            }

            builder.AppendLine(FormatCrisisDetection(detection!, isDryRun, prefix: string.Empty));
        }

        response = builder.ToString().TrimEnd();
        return succeeded;
    }

    private bool TryGetLatestDlrc(
        out RoundSnapshot? snapshot,
        out DlrcEvaluationResult? result,
        out string response)
    {
        snapshot = dlrcEvaluatorService?.LastSnapshot;
        result = dlrcEvaluatorService?.LastResult;
        if (snapshot is null || result is null)
        {
            response = "D-LRC 尚未完成首次有效评估，当前没有可查询的战局快照。";
            return false;
        }

        response = string.Empty;
        return true;
    }

    private static string FormatWaveRecord(string title, MajorWaveRecord record, bool includeDetail)
    {
        StringBuilder builder = new StringBuilder($"【{title}】\n");
        builder.AppendLine($"WaveId：{record.WaveId}");
        builder.AppendLine($"阵营：{record.Faction}");
        builder.AppendLine($"人口档位：{record.PopulationTier}");
        builder.AppendLine($"开始时间：{FormatRoundTime(record.StartedAt)}");
        builder.AppendLine($"完成时间：{FormatRoundTime(record.CompletedAt)}");
        builder.AppendLine($"实际刷新人数：{record.ActualSpawnedCount}");
        builder.AppendLine($"120秒是否成熟：{FormatBoolean(record.IsSurvivalObservationComplete)}");
        builder.AppendLine($"120秒存活人数：{(record.IsSurvivalObservationComplete ? record.SurvivingCountAtObservation.ToString() : "待观察")}");
        if (includeDetail)
        {
            builder.AppendLine($"成员记录数量：{record.MemberIds.Count}");
            builder.Append("成员账号标识：为保护隐私不在 RA 输出。" );
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatCrisisDetection(
        CrisisDetectionResult detection,
        bool isDryRun,
        string prefix)
    {
        StringBuilder builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            builder.AppendLine(prefix);
        }

        if (isDryRun)
        {
            builder.AppendLine("DRY RUN：是");
        }

        builder.AppendLine($"{DlrcStageReportFormatter.FormatCrisisTag(detection.Tag)}");
        builder.AppendLine($"状态：{(detection.IsActive ? "激活" : "未激活")}");
            builder.AppendLine($"状态：{(detection.IsActive ? "激活" : "未激活")}");
        builder.AppendLine("输入与阈值：");
        if (detection.Metrics.Count == 0)
        {
            builder.AppendLine("暂无可展示指标。");
        }
        else
        {
            foreach (KeyValuePair<string, double> metric in detection.Metrics)
            {
                builder.AppendLine($"{FormatMetricName(metric.Key)}：{metric.Value:0.##}");
            }
        }

        builder.Append($"理由：{FormatDetectorReason(detection.Reason)}");
        return builder.ToString();
    }

    private string GetDisplayCode(
        DlrcEvaluationResult result,
        CrisisAssessment? assessment,
        out string crisisNote)
    {
        long assessmentId = assessment?.EvaluationId ?? -1L;
        if (DlrcDisplayCodeFormatter.TryFormat(result, assessmentId, assessment, out string code, out crisisNote))
        {
            return code;
        }

        return result.Code;
    }

    private string FormatInactiveCommandResponse(PluginRuntimeState state)
    {
        return state switch
        {
            PluginRuntimeState.LOW_POPULATION_SUSPENDED => $"EmergencyEvents 当前回合已因人数不足暂停。\n当前人数：{runtimeCoordinator?.CurrentPopulation ?? 0}\n最低要求：{runtimeCoordinator?.MinimumPlayers ?? Config.MinimumPlayers}\n本回合不会重新激活。",
            PluginRuntimeState.DISABLED => "EmergencyEvents 当前已禁用；该操作仅会在有效 EE 回合执行。",
            PluginRuntimeState.STANDBY => "当前回合未满足 EmergencyEvents 启动条件；原版 SCP:SL 正常运行。",
            PluginRuntimeState.ROUND_ENDED => "当前回合已结束，无法执行该操作。",
            _ => "EmergencyEvents 当前没有可执行该操作的有效回合。",
        };
    }

    private static bool RejectUnknownRequest(out string response)
    {
        response = "未知 EmergencyEvents 命令。使用 ee help 查看可用命令。";
        return false;
    }

    private static bool TryParseCrisisTag(string value, out CrisisTag tag)
    {
        tag = value switch
        {
            "bio" => CrisisTag.BIO,
            "sys" => CrisisTag.SYS,
            "con" => CrisisTag.CON,
            "sec" => CrisisTag.SEC,
            "goi" => CrisisTag.GOI,
            "war" => CrisisTag.WAR,
            "end" => CrisisTag.END,
            _ => default,
        };
        return value is "bio" or "sys" or "con" or "sec" or "goi" or "war" or "end";
    }

    private void AppendModuleSummary(StringBuilder builder, string name, bool enabled, bool active)
    {
        builder.AppendLine($"{name}：{GetModuleState(enabled, active)}");
    }

    private string GetModuleState(bool enabled, bool active)
    {
        if (!enabled)
        {
            return "INACTIVE";
        }

        if (runtimeCoordinator?.State == PluginRuntimeState.LOW_POPULATION_SUSPENDED)
        {
            return "SUSPENDED";
        }

        return active ? "ACTIVE" : "LOADED";
    }

    private void AppendCurrentDlrcSummary(StringBuilder builder)
    {
        DlrcEvaluationResult? result = dlrcEvaluatorService?.LastResult;
        if (result is null)
        {
            builder.AppendLine("当前 D-LRC：暂无有效评估");
            return;
        }

        builder.AppendLine("当前 D-LRC：");
        builder.AppendLine(GetDisplayCode(result, crisisManager?.CurrentCrisisAssessment, out _));
        builder.AppendLine($"响应分数：{result.EffectiveResponseScore:0.##}");
        builder.AppendLine($"控制状态：{DlrcStageReportFormatter.FormatControlState(result.ControlState)}");
    }

    private void AppendLatestWaveSummary(StringBuilder builder)
    {
        MajorWaveRecord? record = reinforcementManager?.State?.MajorWaveHistory.LastMajorWave;
        if (record is null)
        {
            builder.AppendLine("最近主支援：暂无");
            return;
        }

        builder.AppendLine("最近主支援：");
        builder.AppendLine($"{record.Faction}；人数：{record.ActualSpawnedCount}；距今：{FormatSeconds((DateTime.UtcNow - record.CompletedAt).TotalSeconds)}");
    }

    private static void AppendCrisisSummary(
        StringBuilder builder,
        CrisisAssessment? assessment,
        DlrcEvaluationResult result)
    {
        builder.AppendLine("—— 当前危机 ——");
        if (assessment is null || !ReferenceEquals(assessment.Result, result))
        {
            builder.AppendLine("危机评估不可用或未与本次评估同步。");
            return;
        }

        if (assessment.ActiveTags.Count == 0)
        {
            builder.AppendLine("当前危机：无");
            return;
        }

        foreach (CrisisTag tag in assessment.ActiveTags)
        {
            builder.AppendLine($"{DlrcStageReportFormatter.FormatCrisisTag(tag)}：{(assessment.IsActive(tag) ? "激活" : "未激活")}");
        }
    }

    private static string FormatMetricName(string name)
    {
        return name switch
        {
            "ZombieCount" => "049-2数量",
            "L3Threshold" => "L3阈值",
            "L4Threshold" => "L4阈值",
            "L5Threshold" => "L5阈值",
            "Scp079Present" => "079存在",
            "Scp079Tier" => "079等级",
            "Scp079TierIsValid" => "079等级数据有效",
            "FoundationCombatants" => "基金会战斗人员",
            "HostileThreatPresent" => "敌对威胁存在",
            "CurrentEquivalent" => "当前SCP当量",
            "BaselineEquivalent" => "SCP当量基线",
            "FailureStreak" => "连续失败次数",
            "ContinuousStalemateSeconds" => "连续地表僵持秒数",
            "SurfaceFoundationCombatants" => "地表基金会人员",
            "SurfaceChaosCombatants" => "地表混沌人员",
            "SurfaceMainScp" => "地表主SCP",
            "SurfaceOtherHostiles" => "地表其他敌对人员",
            "HostileThirdPartyActive" => "敌对第三方已登记",
            "HostileThirdPartyCombatants" => "敌对第三方人数",
            "GlobalLevel" => "D-LRC最终等级",
            "FoundationDisadvantaged" => "基金会处于劣势",
            _ => name,
        };
    }

    private static string FormatDetectorReason(string reason)
    {
        return reason switch
        {
            "ZombieCount below L3Threshold" => "049-2数量低于 L3 阈值",
            "SCP079 unavailable or below Tier3" => "SCP-079 不存在或等级低于 3",
            "SCP079 TierUnavailable" => "SCP-079 等级数据不可用",
            "HostileThreatPresent=false" => "当前不存在敌对威胁",
            "SecondMajorWaveUnavailable" => "第二个主支援波尚不可用",
            "Containment checkpoint passed or pending" => "收容检查点通过或尚未到期",
            "WarheadDetonated=false" => "核弹尚未爆炸",
            "SurfaceHostileStalemate=false" => "地表尚未形成敌对僵持",
            "SurfaceHostileStalemate duration below L3Threshold" => "连续地表僵持时长低于 L3 阈值",
            "GOI activation prerequisites not met" => "GOI 危机前置条件未满足",
            _ => reason,
        };
    }

    private static string FormatTrigger(DlrcEvaluationTrigger? trigger)
    {
        return trigger switch
        {
            DlrcEvaluationTrigger.PERIODIC => "周期评估",
            DlrcEvaluationTrigger.POST_MAJOR_WAVE => "主支援波完成后评估",
            DlrcEvaluationTrigger.MANUAL_RA => "RA手动评估",
            DlrcEvaluationTrigger.MANUAL => "手动评估",
            _ => "暂无",
        };
    }

    private static string FormatThreatTrend(ThreatTrend value)
    {
        return value switch
        {
            ThreatTrend.IMPROVING => "正在改善",
            ThreatTrend.WORSENING => "正在恶化",
            ThreatTrend.STALLED_HIGH => "高位僵持",
            ThreatTrend.STABLE => "稳定",
            _ => "数据不足",
        };
    }

    private static string FormatFoundationStrength(FoundationStrength value)
    {
        return value switch
        {
            FoundationStrength.STRONG => "强",
            FoundationStrength.ADEQUATE => "尚可",
            FoundationStrength.WEAK => "弱",
            FoundationStrength.CRITICAL => "极弱",
            _ => "数据不足",
        };
    }

    private static string FormatWavePerformance(WavePerformance value)
    {
        return value switch
        {
            WavePerformance.GOOD => "良好",
            WavePerformance.NEUTRAL => "一般",
            WavePerformance.POOR => "较差",
            WavePerformance.CATASTROPHIC => "灾难性",
            _ => "数据不足",
        };
    }

    private static string FormatMomentum(BattlefieldMomentum value)
    {
        return value switch
        {
            BattlefieldMomentum.FOUNDATION_POSITIVE => "基金会占优",
            BattlefieldMomentum.FOUNDATION_NEGATIVE => "基金会失利",
            _ => "均势",
        };
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "是" : "否";
    }

    private static string FormatSeconds(double? seconds)
    {
        return seconds.HasValue ? FormatSeconds(seconds.Value) : "暂无";
    }

    private static string FormatSeconds(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0d, seconds));
        int hours = (int)time.TotalHours;
        return hours > 0
            ? $"{hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

    private static string FormatRoundTime(DateTime timestamp)
    {
        return timestamp == default ? "暂无" : timestamp.ToLocalTime().ToString("HH:mm:ss");
    }

    private static string FormatNullableTime(DateTime? timestamp)
    {
        return timestamp.HasValue ? FormatRoundTime(timestamp.Value) : "暂无";
    }

    private static string FormatOptionalLine(string title, string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : $"\n{title}：{value}";
    }

    private static string FormatRoundElapsed()
    {
        return FormatSeconds(Round.ElapsedTime.TotalSeconds);
    }
}
