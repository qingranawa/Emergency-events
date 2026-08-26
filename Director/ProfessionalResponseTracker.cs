using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Director;

/// <summary>
/// 追踪每个危机标签在当前回合的 Episode 与已提交 D-LRC 响应等级。
/// </summary>
public sealed class ProfessionalResponseTracker
{
    private readonly Dictionary<CrisisTag, EpisodeState> states = new Dictionary<CrisisTag, EpisodeState>();
    private long nextEpisodeId;

    public long EpisodeId { get; private set; }

    public void Observe(CrisisAssessment? assessment)
    {
        if (assessment is null)
        {
            return;
        }

        foreach (CrisisTag tag in Enum.GetValues(typeof(CrisisTag)))
        {
            bool isActive = assessment.IsActive(tag);
            if (!states.TryGetValue(tag, out EpisodeState? state))
            {
                state = new EpisodeState();
                states[tag] = state;
            }

            if (isActive && !state.IsActive)
            {
                state.IsActive = true;
                state.EpisodeId = ++nextEpisodeId;
                state.RespondedResponseLevels.Clear();
                EpisodeId = state.EpisodeId;
            }
            else if (!isActive && state.IsActive)
            {
                state.IsActive = false;
                state.RespondedResponseLevels.Clear();
            }
        }
    }

    public bool CanConsume(CrisisTag tag, EventResponseLevel responseLevel)
    {
        return states.TryGetValue(tag, out EpisodeState? state)
            && state.IsActive
            && !state.RespondedResponseLevels.Contains(responseLevel);
    }

    public bool Consume(CrisisTag tag, EventResponseLevel responseLevel, string cycleId)
    {
        if (string.IsNullOrWhiteSpace(cycleId) || !CanConsume(tag, responseLevel))
        {
            return false;
        }

        states[tag].RespondedResponseLevels.Add(responseLevel);
        return true;
    }

    public void Reset()
    {
        states.Clear();
        nextEpisodeId = 0L;
        EpisodeId = 0L;
    }

    private sealed class EpisodeState
    {
        public bool IsActive { get; set; }

        public long EpisodeId { get; set; }

        public HashSet<EventResponseLevel> RespondedResponseLevels { get; } = new HashSet<EventResponseLevel>();
    }
}
