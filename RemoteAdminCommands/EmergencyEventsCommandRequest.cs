namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// 已完成语法校验的命令请求，不包含任何游戏业务计算。
/// </summary>
public readonly struct EmergencyEventsCommandRequest
{
    public EmergencyEventsCommandRequest(
        EmergencyEventsCommandKind kind,
        string? target = null,
        int? number = null,
        bool? flag = null)
    {
        Kind = kind;
        Target = target ?? string.Empty;
        Number = number;
        Flag = flag;
    }

    public EmergencyEventsCommandKind Kind { get; }

    public string Target { get; }

    public int? Number { get; }

    public bool? Flag { get; }
}
