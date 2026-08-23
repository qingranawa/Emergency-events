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

    [Description("第一正常大波开启时间，单位为秒。默认 300 秒，即 05:00。")]
    public float FirstReinforcementTimeSeconds { get; set; } = 300f;

    [Description("第一正常大波等待观察者的最后期限，单位为秒。默认 390 秒，即 06:30。")]
    public float FirstReinforcementDeadlineSeconds { get; set; } = 390f;

    [Description("两次正常大波之间的最短间隔，单位为秒。默认 300 秒，即 05:00。插件不会主动加速原版波次。")]
    public float NormalReinforcementIntervalSeconds { get; set; } = 300f;

    [Description("每次普通支援周期结束后保留的 Support Score 比例。")]
    public double SupportScoreCarryoverRatio { get; set; } = 0.25d;

    [Description("一名 Class-D 正常撤离或被拘留撤离产生的 Support Score。")]
    public int ClassDSupportScore { get; set; } = 1;

    [Description("一名 Scientist 正常撤离或被拘留撤离产生的 Support Score。")]
    public int ScientistSupportScore { get; set; } = 2;
}
