using System;
using Exiled.API.Interfaces;
using Exiled.API.Features;

namespace EmergencyEvents.RuntimeHarness;

/// <summary>
/// 仅让隔离服加载测试命令，不参与正式 EmergencyEvents 行为。
/// </summary>
public sealed class RuntimeHarnessPlugin : Plugin<RuntimeHarnessConfig>
{
    public override string Name => "EmergencyEvents.RuntimeHarness";

    public override string Author => "Codex Test Harness";

    public override Version Version => new Version(0, 0, 1);

    public override Version RequiredExiledVersion => new Version(9, 14, 2);
}

public sealed class RuntimeHarnessConfig : IConfig
{
    public bool IsEnabled { get; set; } = true;

    public bool Debug { get; set; }
}
