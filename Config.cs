using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace EmergencyEvents;

/// <summary>
/// emergency-events 的 EXILED 配置。
/// </summary>
public sealed class Config : IConfig
{
    [Description("是否启用插件。")]
    public bool IsEnabled { get; set; } = true;

    [Description("是否输出 Round Core 的 DEBUG 日志。")]
    public bool Debug { get; set; } = true;

    [Description("是否启用 Round Core 开局编制接管。")]
    public bool RoundCoreEnabled { get; set; } = true;

    [Description("是否把 Foundation Security 与 Chaos Infiltrator 传送到 HCZ Elevator A/B。")]
    public bool TeleportOpeningTeamsToHczElevators { get; set; } = true;

    [Description("Foundation Security 与 Chaos Infiltrator 使用的共用开局物品。填写 ItemType 名称。")]
    public List<string> MirroredStartingItems { get; set; } = new List<string>
    {
        "KeycardGuard",
        "SurfaceAccessPass",
        "GunCOM18",
        "Medkit",
    };

    [Description("Foundation Security 额外获得的开局物品。填写 ItemType 名称。")]
    public List<string> SecurityExtraStartingItems { get; set; } = new List<string>
    {
        "Radio",
    };

    [Description("Foundation Security 的显示标题。")]
    public string SecurityTitle { get; set; } = "安保人员";

    [Description("Chaos Infiltrator 的显示标题。")]
    public string ChaosTitle { get; set; } = "混沌渗透者";

    [Description("是否启用普通支援调度与 Support Score。")]
    public bool ReinforcementEnabled { get; set; } = true;

    [Description("是否启用 D-LRC Evaluator。")]
    public bool DlrcEvaluatorEnabled { get; set; } = true;

    [Description("D-LRC Evaluator 首次评估时间，单位为秒。")]
    public int DlrcEvaluatorStartTimeSeconds { get; set; } = 391;

    [Description("D-LRC Evaluator 评估间隔，单位为秒。")]
    public int DlrcEvaluatorIntervalSeconds { get; set; } = 30;

    [Description("SCP-049-2 达到满压所需的数量。")]
    public int DlrcZombieFullPressureCount { get; set; } = 6;

    [Description("D-LRC Threat Trend 使用的历史窗口，单位为秒。")]
    public int DlrcThreatTrendWindowSeconds { get; set; } = 300;

    [Description("D-LRC Battlefield Momentum 使用的历史窗口，单位为秒。")]
    public int DlrcMomentumWindowSeconds { get; set; } = 120;

    [Description("每次有效核弹取消对 D-LRC Strategic Hazard 增加的分数。")]
    public double DlrcWarheadCancelScore { get; set; } = 5d;

    [Description("D-LRC Strategic Hazard 的核弹取消分数上限。")]
    public double DlrcWarheadCancelMaxScore { get; set; } = 10d;

    [Description("D-LRC Evaluator 保留的评估历史容量。")]
    public int DlrcEvaluationHistoryCapacity { get; set; } = 20;

    [Description("D-LRC E 档响应等级阈值，从 L0 到 L5。")]
    public List<double> DlrcResponseThresholdsE { get; set; } = new List<double>
    {
        0d,
        18d,
        32d,
        48d,
        65d,
        82d,
    };

    [Description("D-LRC D 档响应等级阈值，从 L0 到 L5。")]
    public List<double> DlrcResponseThresholdsD { get; set; } = new List<double>
    {
        0d,
        20d,
        34d,
        50d,
        67d,
        84d,
    };

    [Description("D-LRC C 档响应等级阈值，从 L0 到 L5。")]
    public List<double> DlrcResponseThresholdsC { get; set; } = new List<double>
    {
        0d,
        22d,
        36d,
        52d,
        69d,
        86d,
    };

    [Description("D-LRC B 档响应等级阈值，从 L0 到 L5。")]
    public List<double> DlrcResponseThresholdsB { get; set; } = new List<double>
    {
        0d,
        24d,
        38d,
        54d,
        71d,
        88d,
    };

    [Description("D-LRC A 档响应等级阈值，从 L0 到 L5。")]
    public List<double> DlrcResponseThresholdsA { get; set; } = new List<double>
    {
        0d,
        26d,
        40d,
        56d,
        73d,
        90d,
    };

    [Description("第一正常大波开启时间，单位为秒。默认 300 秒，即 05:00。")]
    public float FirstReinforcementTimeSeconds { get; set; } = 300f;

    [Description("第一正常大波等待观察者的最后期限，单位为秒。默认 390 秒，即 06:30。")]
    public float FirstReinforcementDeadlineSeconds { get; set; } = 390f;

    [Description("正常大波固定窗口之间的间隔，单位为秒。默认 300 秒，即每 05:00 一个窗口。")]
    public float NormalReinforcementIntervalSeconds { get; set; } = 300f;

    [Description("每次普通支援周期结束后保留的 Support Score 比例。")]
    public double SupportScoreCarryoverRatio { get; set; } = 0.25d;

    [Description("一名 Class-D 正常撤离或被拘留撤离产生的 Support Score。")]
    public int ClassDSupportScore { get; set; } = 1;

    [Description("一名 Scientist 正常撤离或被拘留撤离产生的 Support Score。")]
    public int ScientistSupportScore { get; set; } = 2;
}
