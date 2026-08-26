using System;
using EmergencyEvents.Director;
using EmergencyEvents.Evaluation;

namespace EmergencyEvents.Runtime;

/// <summary>
/// Director 使用的设施事实来源边界。
/// </summary>
public interface IFacilityStateProvider
{
    FacilityState GetState(RoundSnapshot snapshot);

    bool IsProvisional { get; }
}

/// <summary>
/// 当前版本只把可靠的核爆事实映射为 DESTROYED，其余状态保持安全的临时 NORMAL。
/// </summary>
public sealed class SnapshotFacilityStateProvider : IFacilityStateProvider
{
    public bool IsProvisional => true;

    public FacilityState GetState(RoundSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return snapshot.WarheadDetonated ? FacilityState.Destroyed : FacilityState.Normal;
    }
}
