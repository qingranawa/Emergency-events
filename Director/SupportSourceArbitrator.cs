using System;
using System.Collections.Generic;
using System.Linq;
using EmergencyEvents.Disorder;

namespace EmergencyEvents.Director;

/// <summary>
/// 普通 SUPPORT 来源仲裁器。FDI 只在这里作为临时权重输入。
/// </summary>
public sealed class SupportSourceArbitrator
{
    private readonly EventDirectorConfig config;
    private readonly IRandomSource randomSource;

    public SupportSourceArbitrator(EventDirectorConfig config)
        : this(config, ProductionRandomSource.Shared)
    {
    }

    public SupportSourceArbitrator(EventDirectorConfig config, IRandomSource randomSource)
    {
        this.config = (config ?? new EventDirectorConfig()).Normalize();
        this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
    }

    public IReadOnlyList<EventSource> GetEligibleSources(
        DirectorContext context,
        IReadOnlyList<EventCandidate> candidates)
    {
        if (context is null || candidates is null)
        {
            return Array.Empty<EventSource>();
        }

        return candidates
            .Where(candidate => candidate.IsLegal
                && candidate.Category == EventCategory.Support
                && candidate.Source != EventSource.ProfessionalCrisisResponse)
            .Select(candidate => candidate.Source)
            .Distinct()
            .OrderBy(source => source)
            .ToArray();
    }

    public EventSource? SelectOrdinarySource(
        DirectorContext context,
        IReadOnlyList<EventCandidate> candidates)
    {
        if (context is null)
        {
            return null;
        }

        IReadOnlyList<EventCandidate> legal = candidates?
            .Where(candidate => candidate.IsLegal
                && candidate.Category == EventCategory.Support
                && candidate.Source != EventSource.ProfessionalCrisisResponse)
            .ToArray() ?? Array.Empty<EventCandidate>();
        if (legal.Count == 0)
        {
            return null;
        }

        var sourceWeights = legal
            .GroupBy(candidate => candidate.Source)
            .Select(group => new
            {
                Source = group.Key,
                Weight = GetEffectiveWeight(group, context),
            })
            .OrderBy(item => GetSourceOrder(item.Source))
            .ThenBy(item => item.Source)
            .ToArray();
        if (sourceWeights.Length == 0)
        {
            return null;
        }

        double total = sourceWeights
            .Where(item => IsPositiveFinite(item.Weight))
            .Sum(item => item.Weight);
        if (!IsPositiveFinite(total))
        {
            return sourceWeights[0].Source;
        }

        double unit = randomSource.NextUnit();
        if (double.IsNaN(unit) || double.IsInfinity(unit) || unit < 0d)
        {
            return sourceWeights[0].Source;
        }

        unit = Math.Min(unit, 0.9999999999999999d);
        double target = unit * total;
        foreach (var sourceWeight in sourceWeights)
        {
            if (!IsPositiveFinite(sourceWeight.Weight))
            {
                continue;
            }

            target -= sourceWeight.Weight;
            if (target < 0d)
            {
                return sourceWeight.Source;
            }
        }

        return sourceWeights.First(item => IsPositiveFinite(item.Weight)).Source;
    }

    private double GetEffectiveWeight(
        IEnumerable<EventCandidate> candidates,
        DirectorContext context)
    {
        double candidateWeight = candidates
            .Select(candidate => candidate.Definition.Weight * Math.Max(1, candidate.Definition.Priority))
            .Where(IsPositiveFinite)
            .DefaultIfEmpty(0d)
            .Max();
        double sourceWeight = candidateWeight * GetSourceWeight(context, candidates.First().Source);
        return IsPositiveFinite(sourceWeight) ? sourceWeight : 0d;
    }

    private static bool IsPositiveFinite(double value)
    {
        return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static int GetSourceOrder(EventSource source)
    {
        return source switch
        {
            EventSource.Foundation => 0,
            EventSource.Chaos => 1,
            EventSource.Goi => 2,
            _ => 3,
        };
    }

    private double GetSourceWeight(DirectorContext context, EventSource source)
    {
        double baseWeight = source switch
        {
            EventSource.Foundation => config.FoundationWeight,
            EventSource.Chaos => config.ChaosWeight,
            EventSource.Goi => config.GoiWeight,
            _ => 1d,
        };

        return context.FacilityDisorderBand switch
        {
            FacilityDisorderBand.LOW when source == EventSource.Foundation => baseWeight * 0.8d,
            FacilityDisorderBand.LOW => baseWeight * 1.2d,
            FacilityDisorderBand.HIGH when source == EventSource.Foundation => baseWeight * 1.2d,
            FacilityDisorderBand.HIGH => baseWeight * 0.8d,
            _ => baseWeight,
        };
    }
}
