using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Director.TestEvents;

/// <summary>
/// 只用于自动化和隔离运行时验证的 Fake Event Definitions，不是生产事件包。
/// </summary>
public static class TestEventDefinitions
{
    public static IReadOnlyList<EventDefinition> CreateDefaults()
    {
        List<EventDefinition> definitions = new List<EventDefinition>
        {
            CreateProfessional("fake-bio-l3", EventResponseLevel.L3, CrisisSeverity.Level3),
            CreateProfessional("fake-bio-l4", EventResponseLevel.L4, CrisisSeverity.Level4),
            CreateSource("fake-foundation-l2", EventSource.Foundation, EventResponseLevel.L2, EventCategory.Support, 20),
            CreateSource("fake-foundation-l3", EventSource.Foundation, EventResponseLevel.L3, EventCategory.Support, 10),
            CreateSource("fake-chaos-l3", EventSource.Chaos, EventResponseLevel.L3, EventCategory.Support, 10),
            CreateSource("fake-goi-l3", EventSource.Goi, EventResponseLevel.L3, EventCategory.Support, 5),
            CreateSource("fake-nonsupport-l2", EventSource.Internal, EventResponseLevel.L2, EventCategory.NonSupport, 10),
            CreateSource("fake-nonsupport-l3", EventSource.Internal, EventResponseLevel.L3, EventCategory.NonSupport, 5),
        };

        return new ReadOnlyCollection<EventDefinition>(definitions);
    }

    private static EventDefinition CreateProfessional(string eventId, EventResponseLevel level, CrisisSeverity severity)
    {
        return new EventDefinition(
            eventId,
            eventId,
            EventCategory.Support,
            EventSource.ProfessionalCrisisResponse,
            level,
            new[] { CrisisTag.BIO },
            severity,
            TierPersonnelPlan.Uniform(6),
            TierPersonnelPlan.Uniform(2),
            isEnabled: false,
            priority: 100,
            weight: 1d,
            requiresUndergroundFacility: false);
    }

    private static EventDefinition CreateSource(
        string eventId,
        EventSource source,
        EventResponseLevel level,
        EventCategory category,
        int priority)
    {
        return new EventDefinition(
            eventId,
            eventId,
            category,
            source,
            level,
            Array.Empty<CrisisTag>(),
            CrisisSeverity.Inactive,
            TierPersonnelPlan.Uniform(6),
            TierPersonnelPlan.Uniform(2),
            isEnabled: false,
            priority: priority,
            weight: 1d,
            requiresUndergroundFacility: false);
    }
}
