using System;

namespace EmergencyEvents.Runtime;

/// <summary>
/// 锁定每局 EmergencyEvents 是否介入，防止低人口后的状态重新初始化。
/// </summary>
public sealed class PluginRuntimeCoordinator
{
    public PluginRuntimeCoordinator(bool isEnabledForNextRound, int minimumPlayers)
    {
        IsEnabledForNextRound = isEnabledForNextRound;
        MinimumPlayers = Math.Max(1, minimumPlayers);
        State = isEnabledForNextRound ? PluginRuntimeState.STANDBY : PluginRuntimeState.DISABLED;
    }

    public PluginRuntimeState State { get; private set; }

    public bool IsEnabledForNextRound { get; private set; }

    public int MinimumPlayers { get; }

    public int RoundStartPopulation { get; private set; }

    public int CurrentPopulation { get; private set; }

    public bool IsRoundInProgress { get; private set; }

    public bool WasLowPopulationSuspended { get; private set; }

    public bool IsEmergencyEventsActiveForRound => State == PluginRuntimeState.ACTIVE;

    public void BeginRound(int currentPopulation)
    {
        RoundStartPopulation = NormalizePopulation(currentPopulation);
        CurrentPopulation = RoundStartPopulation;
        IsRoundInProgress = true;
        WasLowPopulationSuspended = false;
        State = ResolveOpeningState();
    }

    public bool ObservePopulation(int currentPopulation)
    {
        CurrentPopulation = NormalizePopulation(currentPopulation);
        if (State != PluginRuntimeState.ACTIVE || CurrentPopulation >= MinimumPlayers)
        {
            return false;
        }

        WasLowPopulationSuspended = true;
        State = PluginRuntimeState.LOW_POPULATION_SUSPENDED;
        return true;
    }

    public void Disable()
    {
        IsEnabledForNextRound = false;
        State = PluginRuntimeState.DISABLED;
    }

    public bool Enable(bool isRoundInProgress)
    {
        IsEnabledForNextRound = true;
        if (isRoundInProgress || IsRoundInProgress)
        {
            return false;
        }

        State = PluginRuntimeState.STANDBY;
        return true;
    }

    public void EndRound()
    {
        IsRoundInProgress = false;
        State = PluginRuntimeState.ROUND_ENDED;
    }

    public void EnterWaitingForPlayers()
    {
        IsRoundInProgress = false;
        RoundStartPopulation = 0;
        CurrentPopulation = 0;
        WasLowPopulationSuspended = false;
        State = IsEnabledForNextRound ? PluginRuntimeState.STANDBY : PluginRuntimeState.DISABLED;
    }

    private PluginRuntimeState ResolveOpeningState()
    {
        if (!IsEnabledForNextRound)
        {
            return PluginRuntimeState.DISABLED;
        }

        return CurrentPopulation >= MinimumPlayers
            ? PluginRuntimeState.ACTIVE
            : PluginRuntimeState.STANDBY;
    }

    private static int NormalizePopulation(int value)
    {
        return Math.Max(0, value);
    }
}
