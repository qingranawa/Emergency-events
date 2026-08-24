using System.Collections.Generic;
using System.ComponentModel;
using EmergencyEvents.Crisis;
using EmergencyEvents.Reinforcement;
using Exiled.API.Interfaces;

namespace EmergencyEvents;

/// <summary>
/// emergency-events 的 EXILED 配置。
/// </summary>
public sealed class Config : IConfig
{
    [Description("是否启用插件。")]
    public bool IsEnabled { get; set; } = true;

    [Description("是否允许 EmergencyEvents 在下一局介入。RA enable/disable 只切换此运行时开关，不卸载插件。")]
    public bool EmergencyEventsEnabled { get; set; } = true;

    [Description("EmergencyEvents 介入回合所需的最低开局与运行中人数。")]
    public int MinimumPlayers { get; set; } = 16;

    [Description("是否允许 emergencyevents.ra.debug 权限使用 ee test 诊断命令。")]
    public bool DebugCommandsEnabled { get; set; }

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

    [Description("是否启用 Primary Wave 截断、Mini-Wave 禁用与波次事实记录。")]
    public bool ReinforcementEnabled { get; set; } = true;

    [Description("是否取消原版 Mini-Wave。Primary Wave 与 RA 正常手动刷新保留原版流程。")]
    public bool DisableMiniWaves { get; set; } = true;

    [Description("每次成功 Primary Wave 完成后，刷新方原版计时器额外增加的秒数。0=禁用，合法范围 0–300。")]
    public int SpawningFactionTimerExtensionSeconds { get; set; } = PrimaryWaveTimerExtensionPolicy.DefaultSpawningFactionSeconds;

    [Description("每次成功 Primary Wave 完成后，对方原版计时器额外增加的秒数。0=禁用，合法范围 0–300。")]
    public int OpposingFactionTimerExtensionSeconds { get; set; } = PrimaryWaveTimerExtensionPolicy.DefaultOpposingFactionSeconds;

    [Description("按开局锁定人口档位截断 Primary Wave 人数。E=6、D=6、C=8、B=14、A=18。")]
    public PrimaryWaveCaps PrimaryWaveCaps { get; set; } = new PrimaryWaveCaps();

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

    [Description("是否启用 Module 04 Crisis System。关闭后不创建危机评估。")]
    public bool CrisisSystemEnabled { get; set; } = true;

    [Description("BIO 危机 E 档 L3/L4/L5 的 049-2 数量阈值。")]
    public CrisisTierThresholds CrisisBioThresholdsE { get; set; } = new CrisisTierThresholds(3, 5, 7);

    [Description("BIO 危机 D 档 L3/L4/L5 的 049-2 数量阈值。")]
    public CrisisTierThresholds CrisisBioThresholdsD { get; set; } = new CrisisTierThresholds(3, 6, 8);

    [Description("BIO 危机 C 档 L3/L4/L5 的 049-2 数量阈值。")]
    public CrisisTierThresholds CrisisBioThresholdsC { get; set; } = new CrisisTierThresholds(4, 7, 10);

    [Description("BIO 危机 B 档 L3/L4/L5 的 049-2 数量阈值。")]
    public CrisisTierThresholds CrisisBioThresholdsB { get; set; } = new CrisisTierThresholds(4, 8, 12);

    [Description("BIO 危机 A 档 L3/L4/L5 的 049-2 数量阈值。")]
    public CrisisTierThresholds CrisisBioThresholdsA { get; set; } = new CrisisTierThresholds(5, 9, 14);

    [Description("SEC 危机 E 档 Foundation L3/L4/L5 的人数阈值。")]
    public CrisisTierThresholds CrisisSecurityThresholdsE { get; set; } = new CrisisTierThresholds(1, 1, 0);

    [Description("SEC 危机 D 档 Foundation L3/L4/L5 的人数阈值。")]
    public CrisisTierThresholds CrisisSecurityThresholdsD { get; set; } = new CrisisTierThresholds(2, 1, 0);

    [Description("SEC 危机 C 档 Foundation L3/L4/L5 的人数阈值。")]
    public CrisisTierThresholds CrisisSecurityThresholdsC { get; set; } = new CrisisTierThresholds(2, 1, 0);

    [Description("SEC 危机 B 档 Foundation L3/L4/L5 的人数阈值。")]
    public CrisisTierThresholds CrisisSecurityThresholdsB { get; set; } = new CrisisTierThresholds(4, 2, 0);

    [Description("SEC 危机 A 档 Foundation L3/L4/L5 的人数阈值。")]
    public CrisisTierThresholds CrisisSecurityThresholdsA { get; set; } = new CrisisTierThresholds(5, 2, 0);

    [Description("CON 收容检查间隔，单位为秒。非法值回退为 300。")]
    public int CrisisContainmentCheckpointSeconds { get; set; } = 300;

    [Description("CON 每个检查点判定成功所需的最低 SCP Combat Equivalent 下降值。非法值回退为 1.0。")]
    public double CrisisContainmentEquivalentReduction { get; set; } = 1d;

    [Description("END 连续地表敌对僵持达到 L3 所需秒数。")]
    public int CrisisEndLevel3Seconds { get; set; } = 300;

    [Description("END 连续地表敌对僵持达到 L4 所需秒数。")]
    public int CrisisEndLevel4Seconds { get; set; } = 480;

    [Description("END 连续地表敌对僵持达到 L5 所需秒数。")]
    public int CrisisEndLevel5Seconds { get; set; } = 720;

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

}
