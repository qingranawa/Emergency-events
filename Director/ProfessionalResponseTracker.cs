using System;
using System.Collections.Generic;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Director;

/// <summary>
/// 追踪每个危机标签在当前回合的 Episode 与已提交 Severity。
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
            bool isActive = assessment.IsActive(tag) && assessment.GetSeverity(tag) > CrisisSeverity.Inactive;
            if (!states.TryGetValue(tag, out EpisodeState? state))
            {
                state = new EpisodeState();
                states[tag] = state;
            }

            if (isActive && !state.IsActive)
            {
                state.IsActive = true;
                state.EpisodeId = ++nextEpisodeId;
                state.ConsumedSeverities.Clear();
                EpisodeId = state.EpisodeId;
            }
            if (isActive)
            {
                state.CurrentSeverity = assessment.GetSeverity(tag);
            }
            else if (!isActive && state.IsActive)
            {
                state.IsActive = false;
                state.ConsumedSeverities.Clear();
            }
        }
    }

    public bool CanConsume(CrisisTag tag, CrisisSeverity severity)
    {
        return severity >= CrisisSeverity.Level3
            && states.TryGetValue(tag, out EpisodeState? state)
            && state.IsActive
            && state.CurrentSeverity >= severity
            && !state.ConsumedSeverities.Contains(severity);
    }

    public bool Consume(CrisisTag tag, CrisisSeverity severity, string cycleId)
    {
        if (string.IsNullOrWhiteSpace(cycleId) || !CanConsume(tag, severity))
        {
            return false;
        }

        states[tag].ConsumedSeverities.Add(severity);
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

        public CrisisSeverity CurrentSeverity { get; set; }

        public HashSet<CrisisSeverity> ConsumedSeverities { get; } = new HashSet<CrisisSeverity>();
    }
}
