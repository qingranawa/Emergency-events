using System;
using EmergencyEvents.RoundCore;

namespace EmergencyEvents.Director;

/// <summary>
/// 一个事件在指定人口档位下的规模与资源配置。
/// </summary>
public sealed class EventPopulationProfile
{
    public EventPopulationProfile(
        int targetPersonnel,
        int minimumPersonnel,
        bool allowDownscale = true,
        string? compositionProfileId = null,
        string? loadoutProfileId = null)
    {
        TargetPersonnel = Math.Max(0, targetPersonnel);
        MinimumPersonnel = Math.Max(0, minimumPersonnel);
        if (MinimumPersonnel > TargetPersonnel)
        {
            throw new ArgumentException("MinimumPersonnel 不得高于 TargetPersonnel。", nameof(minimumPersonnel));
        }

        AllowDownscale = allowDownscale;
        CompositionProfileId = compositionProfileId?.Trim() ?? string.Empty;
        LoadoutProfileId = loadoutProfileId?.Trim() ?? string.Empty;
    }

    public int TargetPersonnel { get; }

    public int MinimumPersonnel { get; }

    public bool AllowDownscale { get; }

    public string CompositionProfileId { get; }

    public string LoadoutProfileId { get; }
}

public sealed class ResolvedEventPopulation
{
    public ResolvedEventPopulation(
        PopulationTier tier,
        EventPopulationProfile profile,
        int available,
        int planned,
        bool isViable,
        string rejectReason = "")
    {
        Tier = tier;
        Target = profile.TargetPersonnel;
        Minimum = profile.MinimumPersonnel;
        Available = Math.Max(0, available);
        Planned = Math.Max(0, planned);
        AllowDownscale = profile.AllowDownscale;
        CompositionProfileId = profile.CompositionProfileId;
        LoadoutProfileId = profile.LoadoutProfileId;
        IsViable = isViable;
        RejectReason = rejectReason?.Trim() ?? string.Empty;
    }

    public PopulationTier Tier { get; }
    public int Target { get; }
    public int Minimum { get; }
    public int Available { get; }
    public int Planned { get; }
    public bool AllowDownscale { get; }
    public string CompositionProfileId { get; }
    public string LoadoutProfileId { get; }
    public bool IsViable { get; }
    public string RejectReason { get; }
}
