using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

public sealed class EventPopulationResolver : IEventPopulationResolver
{
    public ResolvedEventPopulation Resolve(EventDefinition definition, PopulationTier tier, int availablePersonnel)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        EventPopulationProfile profile = definition.GetPopulationProfile(tier);
        int available = Math.Max(0, availablePersonnel);
        if (available < profile.MinimumPersonnel)
        {
            return new ResolvedEventPopulation(tier, profile, available, 0, false, "PersonnelBelowMinimum");
        }

        if (!profile.AllowDownscale && available < profile.TargetPersonnel)
        {
            return new ResolvedEventPopulation(tier, profile, available, 0, false, "TargetPersonnelUnavailable");
        }

        return new ResolvedEventPopulation(
            tier,
            profile,
            available,
            Math.Min(profile.TargetPersonnel, available),
            true);
    }
}
