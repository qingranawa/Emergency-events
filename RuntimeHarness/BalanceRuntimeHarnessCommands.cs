using System;
using System.Collections.Generic;
using System.IO;
using CommandSystem;
using EmergencyEvents.Crisis;
using EmergencyEvents.Disorder;
using EmergencyEvents.Evaluation;
using EmergencyEvents.RoundCore;
using EmergencyEvents.Telemetry;

namespace EmergencyEvents.RuntimeHarness;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class FdiOrderRecoveryHarnessCommand : ICommand
{
    public string Command => "fdi_order_recovery_probe";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "隔离服 FDI Order Recovery 逻辑探针。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        FacilityDisorderConfig config = new FacilityDisorderConfig { InitialBase = 80d, OrderRecoveryQuietWindowSeconds = 90 };
        FacilityDisorderService service = new FacilityDisorderService(config);
        DateTime initial = DateTime.UtcNow;
        service.StartRound(initial.AddMinutes(-5), 16, 99001);
        Settle(service, initial, 1);
        FacilityDisorderSettlement? recovery = Settle(service, initial.AddSeconds(90), 2);
        FacilityDisorderSettlement? noDuplicate = Settle(service, initial.AddSeconds(120), 3);
        bool passed = recovery?.OrderRecoveryDelta == -2d && noDuplicate?.OrderRecoveryDelta == 0d;
        response = $"{(passed ? "PASS" : "FAIL")} FDI_ORDER_RECOVERY_PROBE\nRecoveryDelta={recovery?.OrderRecoveryDelta:0.####}\nSecondDelta={noDuplicate?.OrderRecoveryDelta:0.####}\nHistory={service.History.Count}";
        return passed;
    }

    private static FacilityDisorderSettlement? Settle(FacilityDisorderService service, DateTime timestamp, long evaluationId)
    {
        RoundSnapshot snapshot = new RoundSnapshot(99001, timestamp, timestamp - timestamp.Date, PopulationTier.E, 16, 1);
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(snapshot, new EvaluationHistory(), new EvaluationOptions(), 0d);
        CrisisAssessment assessment = new CrisisAssessment(evaluationId, DlrcEvaluationTrigger.PERIODIC, snapshot, result, Array.Empty<CrisisDetectionResult>());
        return service.SettlePeriodic(
            new FacilityDisorderEvaluationContext(new DlrcEvaluationCompletedEvent(evaluationId, DlrcEvaluationTrigger.PERIODIC, snapshot, result), assessment),
            new FacilityDisorderStockSnapshot(0, 0, 0, 0, false, 0, assessment, false, false, false));
    }
}

[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class BalanceTelemetryHarnessCommand : ICommand
{
    public string Command => "balance_telemetry_runtime_probe";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "隔离服 Balance Telemetry JSONL 探针。";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string output = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "telemetry-harness");
        BalanceTelemetryService telemetry = new BalanceTelemetryService(new BalanceTelemetryConfig(), output);
        DateTime timestamp = DateTime.UtcNow;
        RoundSnapshot snapshot = new RoundSnapshot(99002, timestamp, TimeSpan.FromSeconds(391), PopulationTier.E, 16, 1, currentOnlinePlayers: 16);
        DlrcEvaluationResult result = DlrcEvaluator.Evaluate(snapshot, new EvaluationHistory(), new EvaluationOptions(), 0d);
        CrisisAssessment assessment = new CrisisAssessment(1, DlrcEvaluationTrigger.PERIODIC, snapshot, result, Array.Empty<CrisisDetectionResult>());
        telemetry.StartRound(99002, timestamp, 16);
        telemetry.RecordEvaluation(new DlrcEvaluationCompletedEvent(1, DlrcEvaluationTrigger.PERIODIC, snapshot, result), assessment, 0d, FacilityDisorderBand.LOW, "PROVISIONAL_NORMAL");
        telemetry.CompleteRound(99002, timestamp.AddSeconds(30), "E", 16);
        string[] files = Directory.GetFiles(output, "*.jsonl");
        bool passed = files.Length > 0 && telemetry.LastWriteError is null;
        response = $"{(passed ? "PASS" : "FAIL")} BALANCE_TELEMETRY_RUNTIME_PROBE\nOutput={output}\nFiles={files.Length}\nLastWriteError={telemetry.LastWriteError ?? "None"}";
        return passed;
    }
}
