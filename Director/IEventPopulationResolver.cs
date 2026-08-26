using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

public interface IEventPopulationResolver
{
    ResolvedEventPopulation Resolve(EventDefinition definition, PopulationTier tier, int availablePersonnel);
}
