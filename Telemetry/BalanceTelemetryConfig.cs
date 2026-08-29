namespace EmergencyEvents.Telemetry;

public sealed class BalanceTelemetryConfig
{
    public bool Enabled { get; set; } = true;

    public bool WriteEvaluationRecords { get; set; } = true;

    public bool WriteWaveRecords { get; set; } = true;

    public bool WriteRoundSummary { get; set; } = true;

    public bool TrackSpectatorWait { get; set; } = true;

    public bool FlushOnRoundEnd { get; set; } = true;

    public int RecentRecordCapacity { get; set; } = 2048;

    public void Validate()
    {
        if (RecentRecordCapacity < 64)
        {
            RecentRecordCapacity = 2048;
        }
    }
}
