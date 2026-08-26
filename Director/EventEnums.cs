namespace EmergencyEvents.Director;

/// <summary>
/// Event Director 可以要求的 D-LRC 响应等级。
/// </summary>
public enum EventResponseLevel
{
    L0 = 0,
    L1 = 1,
    L2 = 2,
    L3 = 3,
    L4 = 4,
    L5 = 5,
}

/// <summary>
/// 事件槽位类别。
/// </summary>
public enum EventCategory
{
    Support,
    NonSupport,
}

/// <summary>
/// 事件候选来源。
/// </summary>
public enum EventSource
{
    Foundation,
    Chaos,
    Goi,
    ProfessionalCrisisResponse,
    Internal,
}

/// <summary>
/// 设施状态，供 Director 做事件合法性过滤。
/// </summary>
public enum FacilityState
{
    Normal,
    Lockdown,
    Evacuation,
    Destroyed,
}

/// <summary>
/// 事件生命周期状态。
/// </summary>
public enum EventLifecycleState
{
    Scheduled,
    Evaluating,
    Selected,
    Prepared,
    Started,
    Committed,
    Failed,
    RolledBack,
    Completed,
}

/// <summary>
/// Director 的两个事件槽位。
/// </summary>
public enum DirectorSlot
{
    Support,
    NonSupport,
}

/// <summary>
/// 第一槽位未成功时第二槽位的临时策略。
/// </summary>
public enum SecondSlotWithoutSuccessfulFirstEventPolicy
{
    Skip,
    Independent,
}

/// <summary>
/// 候选被拒绝时的结构化原因。
/// </summary>
public enum CandidateRejectReason
{
    None,
    Disabled,
    MissingEvaluation,
    InvalidEvaluation,
    RoundMismatch,
    ResponseLevelTooLow,
    CrisisRequirementMissing,
    ProfessionalResponseAlreadyConsumed,
    PersonnelBelowMinimum,
    TargetPersonnelUnavailable,
    FacilityDestroyed,
    NoEligiblePersonnel,
    InvalidDefinition,
}
