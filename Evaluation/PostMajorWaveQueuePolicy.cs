namespace EmergencyEvents.Evaluation;

/// <summary>
/// D-LRC 忙碌期间的 POST_MAJOR_WAVE 补算队列策略。
/// </summary>
public static class PostMajorWaveQueuePolicy
{
    public static bool ShouldQueue(int pendingCount)
    {
        return pendingCount <= 0;
    }
}
