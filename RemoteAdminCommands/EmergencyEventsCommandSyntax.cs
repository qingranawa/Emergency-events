using System;

namespace EmergencyEvents.RemoteAdminCommands;

/// <summary>
/// EmergencyEvents Remote Admin 命令的纯语法解析。
/// </summary>
public static class EmergencyEventsCommandSyntax
{
    public static bool TryParse(string[]? arguments, out EmergencyEventsCommandRequest request)
    {
        string[] values = arguments ?? Array.Empty<string>();
        if (values.Length == 0)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.Help);
            return true;
        }

        string root = Normalize(values[0]);
        if (root == "help")
        {
            return TryParseHelp(values, out request);
        }

        if (root == "round" && values.Length == 2 && Normalize(values[1]) == "state")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.Round);
            return true;
        }

        if (TryParseSimpleRoot(root, values.Length, out request))
        {
            return true;
        }

        return root switch
        {
            "module" => TryParseModule(values, out request),
            "wave" => TryParseWave(values, out request),
            "dlrc" => TryParseDlrc(values, out request),
            "crisis" => TryParseCrisis(values, out request),
            "disorder" or "fdi" => TryParseDisorder(values, out request),
            "o4" => TryParseO4(values, out request),
            "test" => TryParseTest(values, out request),
            _ => Reject(out request),
        };
    }

    public static bool IsDlrcEvaluate(string[] arguments)
    {
        return TryParse(arguments, out EmergencyEventsCommandRequest request)
            && request.Kind == EmergencyEventsCommandKind.DlrcEvaluate;
    }

    public static bool IsDlrcState(string[] arguments)
    {
        return TryParse(arguments, out EmergencyEventsCommandRequest request)
            && request.Kind == EmergencyEventsCommandKind.DlrcState;
    }

    private static bool TryParseSimpleRoot(string root, int length, out EmergencyEventsCommandRequest request)
    {
        if (length == 1 && TryGetSimpleRootKind(root, out EmergencyEventsCommandKind kind))
        {
            request = new EmergencyEventsCommandRequest(kind);
            return true;
        }

        request = default;
        return false;
    }

    private static bool TryGetSimpleRootKind(string value, out EmergencyEventsCommandKind kind)
    {
        kind = value switch
        {
            "status" => EmergencyEventsCommandKind.Status,
            "enable" or "on" => EmergencyEventsCommandKind.Enable,
            "disable" or "off" => EmergencyEventsCommandKind.Disable,
            "version" => EmergencyEventsCommandKind.Version,
            "config" => EmergencyEventsCommandKind.Config,
            "health" => EmergencyEventsCommandKind.Health,
            "modules" => EmergencyEventsCommandKind.Modules,
            "round" => EmergencyEventsCommandKind.Round,
            "cleanup" => EmergencyEventsCommandKind.Cleanup,
            _ => EmergencyEventsCommandKind.Invalid,
        };
        return kind != EmergencyEventsCommandKind.Invalid;
    }

    private static bool TryParseModule(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 2 && IsModuleName(values[1]))
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.ModuleDetail, target: Normalize(values[1]));
            return true;
        }

        return Reject(out request);
    }

    private static bool TryParseHelp(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.Help);
            return true;
        }

        string target = values.Length == 2 ? Normalize(values[1]) : string.Empty;
        if (target is "wave" or "dlrc" or "crisis" or "disorder" or "fdi" or "test")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.Help, target: target);
            return true;
        }

        return Reject(out request);
    }

    private static bool TryParseWave(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.WaveState);
            return true;
        }

        string subcommand = Normalize(values[1]);
        if (values.Length == 2 && TryGetWaveKind(subcommand, out EmergencyEventsCommandKind kind))
        {
            request = new EmergencyEventsCommandRequest(kind);
            return true;
        }

        if (subcommand == "history" && values.Length == 3 && int.TryParse(values[2], out int count) && count > 0)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.WaveHistory, number: count);
            return true;
        }

        if (subcommand == "history"
            && values.Length == 4
            && Normalize(values[3]) == "detail"
            && !string.IsNullOrWhiteSpace(values[2]))
        {
            request = new EmergencyEventsCommandRequest(
                EmergencyEventsCommandKind.WaveHistoryDetail,
                target: Normalize(values[2]));
            return true;
        }

        return Reject(out request);
    }

    private static bool TryGetWaveKind(string value, out EmergencyEventsCommandKind kind)
    {
        kind = value switch
        {
            "state" => EmergencyEventsCommandKind.WaveState,
            "current" => EmergencyEventsCommandKind.WaveCurrent,
            "last" => EmergencyEventsCommandKind.WaveLast,
            "previous" => EmergencyEventsCommandKind.WavePrevious,
            "history" => EmergencyEventsCommandKind.WaveHistory,
            "timers" => EmergencyEventsCommandKind.WaveTimers,
            "cap" => EmergencyEventsCommandKind.WaveCap,
            "survival" => EmergencyEventsCommandKind.WaveSurvival,
            _ => EmergencyEventsCommandKind.Invalid,
        };
        return kind != EmergencyEventsCommandKind.Invalid;
    }

    private static bool TryParseDlrc(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DlrcState);
            return true;
        }

        string subcommand = Normalize(values[1]);
        if (values.Length == 2 && TryGetDlrcKind(subcommand, out EmergencyEventsCommandKind kind))
        {
            request = new EmergencyEventsCommandRequest(kind);
            return true;
        }

        if (subcommand == "stage" && values.Length == 3)
        {
            EmergencyEventsCommandKind stageKind = Normalize(values[2]) switch
            {
                "full" => EmergencyEventsCommandKind.DlrcStageFull,
                "raw" => EmergencyEventsCommandKind.DlrcStageRaw,
                _ => EmergencyEventsCommandKind.Invalid,
            };
            if (stageKind != EmergencyEventsCommandKind.Invalid)
            {
                request = new EmergencyEventsCommandRequest(stageKind);
                return true;
            }
        }

        if (subcommand == "history" && values.Length == 3 && int.TryParse(values[2], out int count) && count > 0)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DlrcHistory, number: count);
            return true;
        }

        return Reject(out request);
    }

    private static bool TryGetDlrcKind(string value, out EmergencyEventsCommandKind kind)
    {
        kind = value switch
        {
            "state" => EmergencyEventsCommandKind.DlrcState,
            "evaluate" => EmergencyEventsCommandKind.DlrcEvaluate,
            "stage" => EmergencyEventsCommandKind.DlrcStage,
            "breakdown" => EmergencyEventsCommandKind.DlrcBreakdown,
            "control" => EmergencyEventsCommandKind.DlrcControl,
            "history" => EmergencyEventsCommandKind.DlrcHistory,
            "snapshot" => EmergencyEventsCommandKind.DlrcSnapshot,
            _ => EmergencyEventsCommandKind.Invalid,
        };
        return kind != EmergencyEventsCommandKind.Invalid;
    }

    private static bool TryParseCrisis(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1 || (values.Length == 2 && Normalize(values[1]) == "state"))
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.CrisisState);
            return true;
        }

        if (values.Length == 2 && Normalize(values[1]) == "list")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.CrisisList);
            return true;
        }

        int targetIndex = Normalize(values[1]) == "check" ? 2 : 1;
        if (values.Length == targetIndex + 1 && IsCrisisTarget(values[targetIndex]))
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.CrisisCheck, target: Normalize(values[targetIndex]));
            return true;
        }

        return Reject(out request);
    }

    private static bool TryParseTest(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 3 && Normalize(values[1]) == "cleanup" && Normalize(values[2]) == "verify")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.TestCleanupVerify);
            return true;
        }

        if (values.Length == 5
            && Normalize(values[1]) == "disorder"
            && Normalize(values[2]) == "event"
            && !string.IsNullOrWhiteSpace(values[3])
            && TryPositiveNumber(values[4], out int eventAmount))
        {
            request = new EmergencyEventsCommandRequest(
                EmergencyEventsCommandKind.TestDisorderEvent,
                target: Normalize(values[3]),
                number: eventAmount);
            return true;
        }

        if (values.Length < 3 || Normalize(values[1]) != "crisis")
        {
            return Reject(out request);
        }

        string target = Normalize(values[2]);
        return target switch
        {
            "all" when values.Length == 3 => Accept(EmergencyEventsCommandKind.TestCrisisAll, out request),
            "check" when values.Length == 4 && IsCrisisTarget(values[3]) => Accept(EmergencyEventsCommandKind.TestCrisisCheck, Normalize(values[3]), out request),
            "bio" when values.Length == 5 && Normalize(values[3]) == "zombies" && TryPositiveNumber(values[4], out int zombies) => Accept(EmergencyEventsCommandKind.TestCrisisBioZombies, zombies, out request),
            "sys" when values.Length == 5 && Normalize(values[3]) == "tier" && TryRange(values[4], 0, 5, out int tier) => Accept(EmergencyEventsCommandKind.TestCrisisSysTier, tier, out request),
            "sec" when values.Length == 7 && Normalize(values[3]) == "foundation" && TryNonNegativeNumber(values[4], out int foundation) && Normalize(values[5]) == "hostile" && bool.TryParse(values[6], out bool hostile) => Accept(EmergencyEventsCommandKind.TestCrisisSec, foundation, hostile, out request),
            "war" when values.Length == 4 && IsWarState(values[3]) => Accept(EmergencyEventsCommandKind.TestCrisisWar, Normalize(values[3]), out request),
            "con" when values.Length is 4 or 5 && Normalize(values[3]) == "checkpoint" && (values.Length == 4 || Normalize(values[4]) == "commit") => Accept(values.Length == 5 ? EmergencyEventsCommandKind.TestCrisisConCheckpointCommit : EmergencyEventsCommandKind.TestCrisisConCheckpoint, out request),
            "end" when values.Length == 4 && Normalize(values[3]) == "check" => Accept(EmergencyEventsCommandKind.TestCrisisEndCheck, out request),
            "end" when values.Length == 5 && Normalize(values[3]) == "simulate" && TryNonNegativeNumber(values[4], out int seconds) => Accept(EmergencyEventsCommandKind.TestCrisisEndSimulate, seconds, out request),
            _ => Reject(out request),
        };
    }

    private static bool TryParseDisorder(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1 || (values.Length == 2 && Normalize(values[1]) == "state"))
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DisorderState);
            return true;
        }

        string subcommand = Normalize(values[1]);
        if (values.Length == 2 && subcommand == "events")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DisorderEvents);
            return true;
        }

        if (values.Length == 2 && subcommand == "explain")
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DisorderExplain);
            return true;
        }

        if (subcommand == "history" && values.Length == 2)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DisorderHistory);
            return true;
        }

        if (subcommand == "history" && values.Length == 3 && int.TryParse(values[2], out int count) && count > 0)
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.DisorderHistory, number: count);
            return true;
        }

        return Reject(out request);
    }

    private static bool TryParseO4(string[] values, out EmergencyEventsCommandRequest request)
    {
        if (values.Length == 1 || (values.Length == 2 && Normalize(values[1]) == "status"))
        {
            request = new EmergencyEventsCommandRequest(EmergencyEventsCommandKind.O4Status);
            return true;
        }

        return Reject(out request);
    }

    private static bool Accept(EmergencyEventsCommandKind kind, out EmergencyEventsCommandRequest request)
    {
        request = new EmergencyEventsCommandRequest(kind);
        return true;
    }

    private static bool Accept(EmergencyEventsCommandKind kind, string target, out EmergencyEventsCommandRequest request)
    {
        request = new EmergencyEventsCommandRequest(kind, target: target);
        return true;
    }

    private static bool Accept(EmergencyEventsCommandKind kind, int number, out EmergencyEventsCommandRequest request)
    {
        request = new EmergencyEventsCommandRequest(kind, number: number);
        return true;
    }

    private static bool Accept(EmergencyEventsCommandKind kind, int number, bool flag, out EmergencyEventsCommandRequest request)
    {
        request = new EmergencyEventsCommandRequest(kind, number: number, flag: flag);
        return true;
    }

    private static bool Reject(out EmergencyEventsCommandRequest request)
    {
        request = default;
        return false;
    }

    private static bool IsModuleName(string value)
    {
        string normalized = Normalize(value);
        return normalized is "round" or "roundcore" or "m01" or "reinforcement" or "wave" or "m02" or "dlrc" or "m03" or "crisis" or "m04" or "disorder" or "fdi" or "m045" or "o4" or "m06";
    }

    private static bool IsCrisisTarget(string value)
    {
        string normalized = Normalize(value);
        return normalized is "all" or "bio" or "sys" or "con" or "sec" or "goi" or "war" or "end";
    }

    private static bool IsWarState(string value)
    {
        string normalized = Normalize(value);
        return normalized is "locked" or "unlocked" or "active" or "detonated";
    }

    private static bool TryPositiveNumber(string value, out int number)
    {
        return int.TryParse(value, out number) && number > 0;
    }

    private static bool TryNonNegativeNumber(string value, out int number)
    {
        return int.TryParse(value, out number) && number >= 0;
    }

    private static bool TryRange(string value, int minimum, int maximum, out int number)
    {
        return int.TryParse(value, out number) && number >= minimum && number <= maximum;
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
