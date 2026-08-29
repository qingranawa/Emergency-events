using System;
using System.Collections.Generic;
using CommandSystem;
using EmergencyEvents.Director;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.O4;

namespace EmergencyEvents.RuntimeHarness;

/// <summary>
/// 隔离服 M06 单 Hint 展示探针，不创建正式事件定义。
/// </summary>
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class O4PanelRuntimeHarnessCommand : ICommand
{
    public string Command => "o4_panel_runtime_probe";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "隔离服 M06 O4 Panel dry-run 探针。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 0)
        {
            response = "用法：o4_panel_runtime_probe";
            return false;
        }

        DateTime now = DateTime.UtcNow;
        O4PanelViewModel model = new O4PanelViewModel(
            "DLRC-C4-BIO+SYS",
            4,
            ControlState.UNCONTROLLED,
            new[] { Crisis.CrisisTag.BIO, Crisis.CrisisTag.SYS },
            FacilityDisorderBand.HIGH,
            now,
            now.AddSeconds(27));
        O4PanelRenderer renderer = new O4PanelRenderer();
        string normal = renderer.RenderNormal(model, now);
        string selection = renderer.RenderSelection(
            model,
            new[]
            {
                new O4CandidateView("harness-alpha", "Harness Alpha", EventCategory.Support, EventSource.Foundation, false),
                new O4CandidateView("harness-beta", "Harness Beta", EventCategory.Support, EventSource.Foundation, true),
            },
            14,
            2,
            5);
        bool panelLoaded = EmergencyEvents.Plugin.Instance?.O4Panel is not null;
        bool normalValid = Contains(normal, "DLRC-C4-BIO+SYS", StringComparison.Ordinal)
            && Contains(normal, "FDI 高", StringComparison.Ordinal)
            && Contains(normal, "BIO · SYS", StringComparison.Ordinal)
            && Contains(normal, "下次评估 00:27", StringComparison.Ordinal)
            && !Contains(normal, "Response Score", StringComparison.OrdinalIgnoreCase);
        bool selectionValid = Contains(selection, "Harness Alpha", StringComparison.Ordinal)
            && Contains(selection, "harness-beta", StringComparison.Ordinal)
            && Contains(selection, "已投 2 / 5", StringComparison.Ordinal)
            && selection.Split('\n').Length <= 7;
        bool compactStates = renderer.RenderSuspended().Split('\n').Length <= 2
            && renderer.RenderUnavailable().Split('\n').Length <= 2;
        bool passed = panelLoaded && normalValid && selectionValid && compactStates;
        response = $"{(passed ? "PASS" : "FAIL")} O4_PANEL_RUNTIME_PROBE\nPLUGIN_ADAPTER Loaded={panelLoaded};HintApi=Player.ShowHint(string,float);Anchor=NOT_SUPPORTED\nNORMAL Valid={normalValid};ResponseScoreHidden={!Contains(normal, "Response Score", StringComparison.OrdinalIgnoreCase)};CrisisSeverityShown=false\nSELECTION Valid={selectionValid};Candidates=2;Lines={selection.Split('\n').Length}\nLIFECYCLE CompactSuspendedUnavailable={compactStates};ProductionDefinitions=0";
        return passed;
    }

    private static bool Contains(string value, string expected, StringComparison comparison)
    {
        return value.IndexOf(expected, comparison) >= 0;
    }
}

/// <summary>
/// 隔离服 M06 投票和 stale 保护探针，不向 Production EventRegistry 注册候选。
/// </summary>
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class O4SelectionRuntimeHarnessCommand : ICommand
{
    public string Command => "o4_selection_runtime_probe";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "隔离服 M06 O4 Selection dry-run 探针。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 0)
        {
            response = "用法：o4_selection_runtime_probe";
            return false;
        }

        DateTime now = DateTime.UtcNow;
        O4SelectionRequest request = CreateRequest(96006, 1, "harness-o4-1");
        O4PlayerSnapshot[] voters =
        {
            new O4PlayerSnapshot("O4-01", true, O4RoleState.Spectator),
            new O4PlayerSnapshot("O4-02", true, O4RoleState.Spectator),
            new O4PlayerSnapshot("O4-03", true, O4RoleState.Overwatch),
            new O4PlayerSnapshot("O4-04", true, O4RoleState.Spectator),
        };
        O4VoteService majority = new O4VoteService(new O4PanelConfig());
        bool opened = majority.TryOpenSession(request, voters, now, out _);
        bool voteOne = majority.TryCastVote(request.SessionId, "O4-01", 1, now.AddSeconds(1), true, out _, out _);
        bool voteTwo = majority.TryCastVote(request.SessionId, "O4-02", 1, now.AddSeconds(1), true, out _, out _);
        bool voteThree = majority.TryCastVote(request.SessionId, "O4-03", 2, now.AddSeconds(1), true, out _, out _);
        bool changedVote = majority.TryCastVote(request.SessionId, "O4-04", 1, now.AddSeconds(1), true, out _, out _);
        O4SelectionResult? winner = majority.ResolveActive(now.AddSeconds(20), voters);

        O4VoteService tie = new O4VoteService(new O4PanelConfig());
        O4SelectionRequest tieRequest = CreateRequest(96006, 2, "harness-o4-2");
        tie.TryOpenSession(tieRequest, voters, now, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-01", 1, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-02", 1, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-03", 2, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-04", 2, now.AddSeconds(1), true, out _, out _);
        O4SelectionResult? tieResult = tie.ResolveActive(now.AddSeconds(20), voters);

        O4VoteService noO4 = new O4VoteService(new O4PanelConfig());
        bool noO4Opened = noO4.TryOpenSession(CreateRequest(96006, 3, "harness-o4-3"), Array.Empty<O4PlayerSnapshot>(), now, out O4SelectionResult noO4Result);
        O4SelectionResult stale = O4SelectionResult.ExplicitWinner(96006, 99, "harness-o4-stale", "harness-alpha", now);
        O4VoteService cancelled = new O4VoteService(new O4PanelConfig());
        O4SelectionRequest cancelledRequest = CreateRequest(96006, 4, "harness-o4-4");
        cancelled.TryOpenSession(cancelledRequest, voters, now, out _);
        O4SelectionResult? cancelledResult = cancelled.CancelActive("RoundEnd", now.AddSeconds(2));

        bool majorityPass = opened && voteOne && voteTwo && voteThree && changedVote
            && winner?.Outcome == O4SelectionOutcome.EXPLICIT_WINNER
            && winner.SelectedEventId == "harness-alpha"
            && winner.EligibleVotes == 4;
        bool tiePass = tieResult?.Outcome == O4SelectionOutcome.FALLBACK && tieResult.Reason == "TIE";
        bool noO4Pass = !noO4Opened && noO4Result.Outcome == O4SelectionOutcome.FALLBACK && noO4Result.Reason == "NO_O4_AVAILABLE";
        bool stalePass = !stale.MatchesBinding(96006, 4, "harness-o4-4");
        bool cleanupPass = cancelledResult?.Outcome == O4SelectionOutcome.CANCELLED && cancelled.ActiveSession is null;
        bool passed = majorityPass && tiePass && noO4Pass && stalePass && cleanupPass;
        response = $"{(passed ? "PASS" : "FAIL")} O4_SELECTION_RUNTIME_PROBE\nSYNTHETIC_ONLY ProductionDefinitions=0;Candidates=2\nMAJORITY Opened={opened};Winner={winner?.SelectedEventId};EligibleVotes={winner?.EligibleVotes}\nTIE Outcome={tieResult?.Outcome};Reason={tieResult?.Reason}\nNO_O4 Opened={noO4Opened};Reason={noO4Result.Reason}\nSTALE Rejected={stalePass}\nCLEANUP Cancelled={cleanupPass};ActiveSession={cancelled.ActiveSession is not null}";
        return passed;
    }

    private static O4SelectionRequest CreateRequest(long roundId, long cycleId, string sessionId)
    {
        return new O4SelectionRequest(
            roundId,
            cycleId,
            sessionId,
            DateTime.UtcNow,
            new[]
            {
                new O4CandidateView("harness-alpha", "Harness Alpha", EventCategory.Support, EventSource.Foundation, false),
                new O4CandidateView("harness-beta", "Harness Beta", EventCategory.Support, EventSource.Foundation, false),
            },
            "harness-alpha");
    }
}
