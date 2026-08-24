using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmergencyEvents.Crisis;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 将 D-LRC 快照转换为管理员可读的中文战局报告。
/// </summary>
public static class DlrcStageReportFormatter
{
    public static string FormatStandard(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisAssessment? assessment)
    {
        return Format(snapshot, result, assessment, includeDetails: false);
    }

    public static string FormatFull(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisAssessment? assessment)
    {
        return Format(snapshot, result, assessment, includeDetails: true);
    }

    private static string Format(
        RoundSnapshot snapshot,
        DlrcEvaluationResult result,
        CrisisAssessment? assessment,
        bool includeDetails)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("【D-LRC 当前战局快照】");
        AppendRound(builder, snapshot);
        AppendDlrc(builder, result, assessment);
        AppendPersonnel(builder, snapshot);
        AppendScp(builder, snapshot);
        AppendWaves(builder, snapshot);
        AppendWarhead(builder, snapshot);
        AppendCrisis(builder, assessment);
        if (includeDetails)
        {
            AppendDetails(builder, snapshot, result);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendRound(StringBuilder builder, RoundSnapshot snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("—— 回合信息 ——");
        builder.AppendLine($"回合编号：{snapshot.RoundId}");
        builder.AppendLine($"人口编制：{snapshot.PopulationTier}");
        builder.AppendLine($"开局人数：{snapshot.RoundStartPopulation}");
        builder.AppendLine($"当前在线：{snapshot.CurrentOnlinePlayers}");
        builder.AppendLine($"回合时间：{FormatTime(snapshot.RoundElapsedTime)}");
    }

    private static void AppendDlrc(
        StringBuilder builder,
        DlrcEvaluationResult result,
        CrisisAssessment? assessment)
    {
        builder.AppendLine();
        builder.AppendLine("—— D-LRC 状态 ——");
        string code = DlrcDisplayCodeFormatter.TryFormat(
            result,
            assessment?.EvaluationId ?? -1L,
            assessment,
            out string formattedCode,
            out string reason)
            ? formattedCode
            : result.Code;
        builder.AppendLine($"完整代码：{code}");
        builder.AppendLine($"响应分数：{result.EffectiveResponseScore:0.##} / 100");
        builder.AppendLine($"理论响应级别：{result.TheoreticalLevel}");
        builder.AppendLine($"最终响应级别：{result.FinalLevel}");
        builder.AppendLine($"控制状态：{FormatControlState(result.ControlState)}");
        if (!string.IsNullOrEmpty(reason))
        {
            builder.AppendLine($"危机关联：{reason}");
        }
    }

    private static void AppendPersonnel(StringBuilder builder, RoundSnapshot snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("—— 人类战场 ——");
        builder.AppendLine($"基金会战斗人员：{snapshot.FoundationCombatants}");
        builder.AppendLine($"混沌战斗人员：{snapshot.ChaosCombatants}");
        builder.AppendLine($"其他敌对人员：{snapshot.OtherHostileCombatants}");
        builder.AppendLine($"D级人员：{snapshot.ClassDAlive}");
        builder.AppendLine($"科学家：{snapshot.ScientistsAlive}");
        builder.AppendLine($"可参与支援观察者：{snapshot.EligibleSpectators}");
        builder.AppendLine($"监督模式玩家：{snapshot.OverwatchCount}");
    }

    private static void AppendScp(StringBuilder builder, RoundSnapshot snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("—— SCP 状态 ——");
        builder.AppendLine($"开局主SCP：{snapshot.StartingScpCount}");
        builder.AppendLine($"当前主SCP：{snapshot.MainScpAlive}");
        builder.AppendLine($"SCP-049-2：{snapshot.Scp0492Count}");
        builder.AppendLine($"SCP-079：{FormatBoolean(snapshot.Scp079Present)}");
        builder.AppendLine($"SCP-079等级：{(snapshot.Scp079Present && snapshot.Scp079TierIsValid ? snapshot.Scp079Tier.ToString() : "暂无")}");
    }

    private static void AppendWaves(StringBuilder builder, RoundSnapshot snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("—— 支援状态 ——");
        MajorWaveSnapshot? last = snapshot.MajorWaveHistory
            .OrderByDescending(wave => wave.CompletedAt)
            .FirstOrDefault();
        MajorWaveSnapshot? previous = snapshot.MajorWaveHistory
            .OrderByDescending(wave => wave.CompletedAt)
            .Skip(1)
            .FirstOrDefault();
        AppendWave(builder, "最近主支援", last, snapshot.Timestamp);
        if (previous is not null)
        {
            AppendWave(builder, "上一主支援", previous, snapshot.Timestamp);
        }
    }

    private static void AppendWave(StringBuilder builder, string title, MajorWaveSnapshot? wave, DateTime now)
    {
        if (wave is null)
        {
            builder.AppendLine($"{title}：暂无");
            return;
        }

        builder.AppendLine($"{title}：{wave.Name}");
        builder.AppendLine($"实际刷新人数：{wave.StartingCount}");
        builder.AppendLine($"距今：{FormatTime(now - wave.CompletedAt)}");
    }

    private static void AppendWarhead(StringBuilder builder, RoundSnapshot snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("—— 核设施状态 ——");
        builder.AppendLine($"核弹解锁：{FormatBoolean(snapshot.WarheadUnlocked)}");
        builder.AppendLine($"核弹启动：{FormatBoolean(snapshot.WarheadActive)}");
        builder.AppendLine($"核弹已爆炸：{FormatBoolean(snapshot.WarheadDetonated)}");
        builder.AppendLine($"有效取消次数：{snapshot.WarheadCancellationCount}");
    }

    private static void AppendCrisis(StringBuilder builder, CrisisAssessment? assessment)
    {
        builder.AppendLine();
        builder.AppendLine("—— 当前危机 ——");
        if (assessment is null)
        {
            builder.AppendLine("危机评估：不可用");
            return;
        }

        List<CrisisDetectionResult> activeDetections = assessment.Detections.Values
            .Where(detection => detection.IsActive)
            .OrderBy(detection => detection.Tag)
            .ToList();
        if (activeDetections.Count == 0)
        {
            builder.AppendLine("当前危机：无");
            return;
        }

        foreach (CrisisDetectionResult detection in activeDetections)
        {
            builder.AppendLine($"{FormatCrisisTag(detection.Tag)}：{(int)detection.Severity}级");
        }
    }

    private static void AppendDetails(StringBuilder builder, RoundSnapshot snapshot, DlrcEvaluationResult result)
    {
        builder.AppendLine();
        builder.AppendLine("—— SCP详细状态 ——");
        foreach (ScpSnapshot scp in snapshot.ScpStates.Where(scp => scp.IsAlive && !scp.IsScp079))
        {
            builder.AppendLine($"{scp.RoleName}");
            builder.AppendLine($"生命值：{scp.CurrentHealth:0.##} / {scp.MaxHealth:0.##}");
            builder.AppendLine($"休谟护盾：{scp.CurrentHume:0.##} / {scp.MaxHume:0.##}");
        }

        builder.AppendLine("—— Control Signals ——");
        ControlAssessment control = result.ControlAssessment;
        builder.AppendLine($"威胁趋势：{FormatThreatTrend(control.ThreatTrend)}");
        builder.AppendLine($"基金会强度：{FormatFoundationStrength(control.FoundationStrength)}");
        builder.AppendLine($"支援表现：{FormatWavePerformance(control.WavePerformance)}");
        builder.AppendLine($"战场动量：{FormatMomentum(control.BattlefieldMomentum)}");
        builder.AppendLine("—— 响应分数子项 ——");
        ResponseBreakdown score = result.ResponseBreakdown;
        builder.AppendLine($"SCP威胁度：{score.ScpThreatTotal:0.##} / 40");
        builder.AppendLine($"基金会压力：{score.FoundationPressureTotal:0.##} / 20");
        builder.AppendLine($"支援失效度：{score.ReinforcementFailure:0.##} / 20");
        builder.AppendLine($"时间压力：{score.TimePressure:0.##} / 10");
        builder.AppendLine($"战略危险度：{score.StrategicHazard:0.##} / 10");
    }

    public static string FormatCrisisTag(CrisisTag tag)
    {
        return tag switch
        {
            CrisisTag.BIO => "生化危机（BIO）",
            CrisisTag.SYS => "系统危机（SYS）",
            CrisisTag.CON => "收容危机（CON）",
            CrisisTag.SEC => "安全危机（SEC）",
            CrisisTag.GOI => "GOI危机（GOI）",
            CrisisTag.WAR => "核危机（WAR）",
            CrisisTag.END => "终局危机（END）",
            _ => tag.ToString(),
        };
    }

    public static string FormatControlState(ControlState state)
    {
        return state switch
        {
            ControlState.ADVANTAGE => "优势（ADVANTAGE）",
            ControlState.CONTROLLED => "受控（CONTROLLED）",
            ControlState.UNCONTROLLED => "失控（UNCONTROLLED）",
            ControlState.COLLAPSE => "崩溃（COLLAPSE）",
            _ => "数据不足",
        };
    }

    private static string FormatThreatTrend(ThreatTrend trend)
    {
        return trend switch
        {
            ThreatTrend.IMPROVING => "正在改善",
            ThreatTrend.WORSENING => "正在恶化",
            ThreatTrend.STALLED_HIGH => "高位僵持",
            ThreatTrend.STABLE => "稳定",
            _ => "数据不足",
        };
    }

    private static string FormatFoundationStrength(FoundationStrength strength)
    {
        return strength switch
        {
            FoundationStrength.STRONG => "强",
            FoundationStrength.ADEQUATE => "尚可",
            FoundationStrength.WEAK => "弱",
            FoundationStrength.CRITICAL => "极弱",
            _ => "数据不足",
        };
    }

    private static string FormatWavePerformance(WavePerformance performance)
    {
        return performance switch
        {
            WavePerformance.GOOD => "良好",
            WavePerformance.NEUTRAL => "一般",
            WavePerformance.POOR => "较差",
            WavePerformance.CATASTROPHIC => "灾难性",
            _ => "数据不足",
        };
    }

    private static string FormatMomentum(BattlefieldMomentum momentum)
    {
        return momentum switch
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

    private static string FormatTime(TimeSpan value)
    {
        TimeSpan nonNegative = value < TimeSpan.Zero ? TimeSpan.Zero : value;
        int totalHours = (int)nonNegative.TotalHours;
        return totalHours > 0
            ? $"{totalHours:D2}:{nonNegative.Minutes:D2}:{nonNegative.Seconds:D2}"
            : $"{nonNegative.Minutes:D2}:{nonNegative.Seconds:D2}";
    }
}
