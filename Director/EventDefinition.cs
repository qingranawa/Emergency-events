using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EmergencyEvents.Crisis;
using EmergencyEvents.Evaluation;
using EmergencyEvents.Reinforcement;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// Module 05 的声明式事件定义，不包含具体生产事件的生成逻辑。
/// </summary>
public sealed class EventDefinition
{
    public EventDefinition(
        string eventId,
        string displayName,
        EventCategory category,
        EventSource source,
        EventResponseLevel requiredResponseLevel,
        IEnumerable<CrisisTag>? requiredCrisisTags,
        TierPersonnelPlan targetPersonnel,
        TierPersonnelPlan minimumPersonnel,
        bool isEnabled,
        int priority,
        double weight,
        bool requiresUndergroundFacility)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            throw new ArgumentException("EventId 不能为空。", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName 不能为空。", nameof(displayName));
        }

        EventId = eventId.Trim();
        DisplayName = displayName.Trim();
        Category = category;
        Source = source;
        RequiredResponseLevel = requiredResponseLevel;
        RequiredCrisisTags = new ReadOnlyCollection<CrisisTag>(
            (requiredCrisisTags ?? Array.Empty<CrisisTag>()).Distinct().ToArray());
        TargetPersonnel = targetPersonnel ?? throw new ArgumentNullException(nameof(targetPersonnel));
        MinimumPersonnel = minimumPersonnel ?? throw new ArgumentNullException(nameof(minimumPersonnel));
        IsEnabled = isEnabled;
        Priority = priority;
        Weight = !double.IsNaN(weight) && !double.IsInfinity(weight) && weight > 0d ? weight : 0d;
        RequiresUndergroundFacility = requiresUndergroundFacility;
        Dictionary<PopulationTier, EventPopulationProfile> profiles = new Dictionary<PopulationTier, EventPopulationProfile>();

        foreach (PopulationTier tier in Enum.GetValues(typeof(PopulationTier)))
        {
            if (GetMinimumPersonnel(tier) > GetTargetPersonnel(tier))
            {
                throw new ArgumentException("MinimumPersonnel 不得高于 TargetPersonnel。", nameof(minimumPersonnel));
            }

            profiles[tier] = new EventPopulationProfile(GetTargetPersonnel(tier), GetMinimumPersonnel(tier));
        }

        PopulationProfiles = new ReadOnlyDictionary<PopulationTier, EventPopulationProfile>(profiles);
    }

    public string EventId { get; }

    public string DisplayName { get; }

    public EventCategory Category { get; }

    public EventSource Source { get; }

    public EventResponseLevel RequiredResponseLevel { get; }

    public IReadOnlyList<CrisisTag> RequiredCrisisTags { get; }

    public TierPersonnelPlan TargetPersonnel { get; }

    public TierPersonnelPlan MinimumPersonnel { get; }

    public bool IsEnabled { get; }

    public int Priority { get; }

    public double Weight { get; }

    public bool RequiresUndergroundFacility { get; }

    public IReadOnlyDictionary<PopulationTier, EventPopulationProfile> PopulationProfiles { get; }

    public bool IsProfessionalResponse => Source == EventSource.ProfessionalCrisisResponse;

    public int GetTargetPersonnel(PopulationTier tier)
    {
        return TargetPersonnel.Get(tier);
    }

    public int GetMinimumPersonnel(PopulationTier tier)
    {
        return MinimumPersonnel.Get(tier);
    }

    public EventPopulationProfile GetPopulationProfile(PopulationTier tier)
    {
        return PopulationProfiles[tier];
    }
}
