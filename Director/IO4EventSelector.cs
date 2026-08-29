using System;
using EmergencyEvents.O4;

namespace EmergencyEvents.Director;

/// <summary>
/// M05 与 M06 之间的最小异步选择边界。
/// </summary>
public interface IO4EventSelector
{
    bool IsAvailable { get; }

    void RequestSelection(O4SelectionRequest request, Action<O4SelectionResult> completed);

    void CancelAll(string reason);
}
