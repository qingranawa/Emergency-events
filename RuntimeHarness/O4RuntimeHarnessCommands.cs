using System;
using System.Collections.Generic;
using System.Linq;
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
            && Contains(selection, "已投 2", StringComparison.Ordinal)
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
        bool opened = majority.TryOpenSession(request, voters.Take(2).ToArray(), now, out _);
        bool voteOne = majority.TryCastVote(request.SessionId, "O4-01", 1, now.AddSeconds(1), true, out _, out _);
        bool voteTwo = majority.TryCastVote(request.SessionId, "O4-02", 1, now.AddSeconds(1), true, out _, out _);
        bool joinVote = majority.TryCastVote(request.SessionId, "O4-03", 2, now.AddSeconds(5), true, out _, out _);
        bool changedVote = majority.TryCastVote(request.SessionId, "O4-01", 2, now.AddSeconds(6), true, out _, out string changedReason);
        O4SelectionResult? winner = majority.ResolveActive(now.AddSeconds(20), voters);

        O4VoteService leaving = new O4VoteService(new O4PanelConfig());
        O4SelectionRequest leavingRequest = CreateRequest(96006, 2, "harness-o4-leave");
        leaving.TryOpenSession(leavingRequest, voters.Take(2).ToArray(), now, out _);
        leaving.TryCastVote(leavingRequest.SessionId, "O4-01", 1, now.AddSeconds(1), true, out _, out _);
        leaving.TryCastVote(leavingRequest.SessionId, "O4-02", 2, now.AddSeconds(1), true, out _, out _);
        O4PlayerSnapshot[] afterLeave =
        {
            voters[0],
            new O4PlayerSnapshot("O4-02", true, O4RoleState.Alive),
        };
        O4SelectionResult? leaveResult = leaving.ResolveActive(now.AddSeconds(20), afterLeave);

        O4VoteService tie = new O4VoteService(new O4PanelConfig());
        O4SelectionRequest tieRequest = CreateRequest(96006, 3, "harness-o4-tie");
        tie.TryOpenSession(tieRequest, voters, now, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-01", 1, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-02", 1, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-03", 2, now.AddSeconds(1), true, out _, out _);
        tie.TryCastVote(tieRequest.SessionId, "O4-04", 2, now.AddSeconds(1), true, out _, out _);
        O4SelectionResult? tieResult = tie.ResolveActive(now.AddSeconds(20), voters);
        EventCandidate[] tiedCandidates =
        {
            CreateHarnessCandidate("harness-alpha", 2),
            CreateHarnessCandidate("harness-beta", 1),
        };
        EventCandidate? tieWinner = new EventSelectionService(
            new SupportSourceArbitrator(new EventDirectorConfig()))
            .ResolveTiedCandidates(tiedCandidates, tieResult?.TiedCandidateIds ?? Array.Empty<string>());

        O4VoteService noO4 = new O4VoteService(new O4PanelConfig());
        bool noO4Opened = noO4.TryOpenSession(CreateRequest(96006, 4, "harness-o4-none"), Array.Empty<O4PlayerSnapshot>(), now, out O4SelectionResult noO4Result);
        O4VoteService single = new O4VoteService(new O4PanelConfig());
        bool singleOpened = single.TryOpenSession(CreateRequest(96006, 5, "harness-o4-single", 1), Array.Empty<O4PlayerSnapshot>(), now, out O4SelectionResult singleResult);
        O4SelectionResult stale = O4SelectionResult.ExplicitWinner(96006, 99, "harness-o4-stale", "harness-alpha", now);
        O4VoteService cancelled = new O4VoteService(new O4PanelConfig());
        O4SelectionRequest cancelledRequest = CreateRequest(96006, 6, "harness-o4-cancel");
        cancelled.TryOpenSession(cancelledRequest, voters, now, out _);
        O4SelectionResult? cancelledResult = cancelled.CancelActive("RoundEnd", now.AddSeconds(2));

        bool majorityPass = opened && voteOne && voteTwo && joinVote && !changedVote
            && changedReason == "ALREADY_VOTED"
            && winner?.Outcome == O4SelectionOutcome.EXPLICIT_WINNER
            && winner.SelectedEventId == "harness-alpha"
            && winner.EligibleVotes == 3;
        bool leavePass = leaveResult?.SelectedEventId == "harness-alpha" && leaveResult.EligibleVotes == 1;
        bool tiePass = tieResult?.Outcome == O4SelectionOutcome.TIE
            && tieResult.Reason == "TIE"
            && tieWinner?.Definition.EventId == "harness-alpha";
        bool noO4Pass = !noO4Opened && noO4Result.Outcome == O4SelectionOutcome.SKIPPED && noO4Result.Reason == "NO_O4_AVAILABLE";
        bool singlePass = !singleOpened
            && singleResult.Outcome == O4SelectionOutcome.EXPLICIT_WINNER
            && singleResult.SelectedEventId == "harness-alpha";
        bool stalePass = !stale.MatchesBinding(96006, 4, "harness-o4-4");
        bool cleanupPass = cancelledResult?.Outcome == O4SelectionOutcome.CANCELLED && cancelled.ActiveSession is null;
        bool passed = majorityPass && leavePass && tiePass && noO4Pass && singlePass && stalePass && cleanupPass;
        response = $"{(passed ? "PASS" : "FAIL")} O4_SELECTION_RUNTIME_PROBE\nSYNTHETIC_ONLY ProductionDefinitions=0;Candidates=2\nDYNAMIC_JOIN Accepted={joinVote};LateVoteCount={winner?.EligibleVotes};SecondVoteRejected={!changedVote};Reason={changedReason}\nLEAVE_INVALIDATED Winner={leaveResult?.SelectedEventId};EligibleVotes={leaveResult?.EligibleVotes}\nTIE Outcome={tieResult?.Outcome};Reason={tieResult?.Reason};M05Winner={tieWinner?.Definition.EventId}\nNO_O4 Outcome={noO4Result.Outcome};Reason={noO4Result.Reason}\nSINGLE Opened={singleOpened};Selected={singleResult.SelectedEventId}\nSTALE Rejected={stalePass}\nCLEANUP Cancelled={cleanupPass};ActiveSession={cancelled.ActiveSession is not null}";
        return passed;
    }

    private static EventCandidate CreateHarnessCandidate(string eventId, int priority)
    {
        EventDefinition definition = new EventDefinition(
            eventId,
            eventId,
            EventCategory.Support,
            EventSource.Foundation,
            EventResponseLevel.L0,
            Array.Empty<Crisis.CrisisTag>(),
            TierPersonnelPlan.Uniform(1),
            TierPersonnelPlan.Uniform(1),
            isEnabled: true,
            priority: priority,
            weight: 1d,
            requiresUndergroundFacility: false);
        return new EventCandidate(definition, true, "HarnessEligible", 1, 1, 1, 1);
    }

    private static O4SelectionRequest CreateRequest(long roundId, long cycleId, string sessionId, int candidateCount = 2)
    {
        O4CandidateView[] candidates =
        {
            new O4CandidateView("harness-alpha", "Harness Alpha", EventCategory.Support, EventSource.Foundation, false),
            new O4CandidateView("harness-beta", "Harness Beta", EventCategory.Support, EventSource.Foundation, false),
        };
        return new O4SelectionRequest(
            roundId,
            cycleId,
            sessionId,
            DateTime.UtcNow,
            candidates.Take(candidateCount).ToArray());
    }
}
