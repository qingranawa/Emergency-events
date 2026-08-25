using System;
using EmergencyEvents.Crisis;

namespace EmergencyEvents.Director;

/// <summary>
/// 只根据已有官方事实判断事件候选是否合法。
/// </summary>
public sealed class EventEligibilityService
{
    public EventCandidate Evaluate(
        DirectorContext context,
        EventDefinition definition,
        ProfessionalResponseTracker tracker)
    {
        if (context is null || definition is null || tracker is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        int requested = definition.GetTargetPersonnel(context.PopulationTier);
        int minimum = definition.GetMinimumPersonnel(context.PopulationTier);
        int available = GetAvailablePersonnel(context, definition.Source);

        if (!definition.IsEnabled)
        {
            return Reject(definition, CandidateRejectReason.Disabled, "Disabled", available, requested, minimum);
        }

        if (context.DlrcResult is null)
        {
            return Reject(definition, CandidateRejectReason.MissingEvaluation, "MissingEvaluation", available, requested, minimum);
        }

        if (!context.DlrcResult.IsValid)
        {
            return Reject(definition, CandidateRejectReason.InvalidEvaluation, "InvalidEvaluation", available, requested, minimum);
        }

        if (context.DlrcResult.RoundId != context.RoundId)
        {
            return Reject(definition, CandidateRejectReason.RoundMismatch, "RoundMismatch", available, requested, minimum);
        }

        if ((int)context.DlrcResult.FinalLevel < (int)definition.RequiredResponseLevel)
        {
            return Reject(definition, CandidateRejectReason.ResponseLevelTooLow, "ResponseLevelTooLow", available, requested, minimum);
        }

        if (definition.IsProfessionalResponse && definition.RequiredCrisisTags.Count == 0)
        {
            return Reject(definition, CandidateRejectReason.InvalidDefinition, "ProfessionalResponseRequiresCrisisTag", available, requested, minimum);
        }

        CandidateRejectReason crisisFailure = CheckCrisisRequirements(context, definition);
        if (crisisFailure != CandidateRejectReason.None)
        {
            return Reject(definition, crisisFailure, crisisFailure.ToString(), available, requested, minimum);
        }

        if (definition.IsProfessionalResponse)
        {
            foreach (CrisisTag tag in definition.RequiredCrisisTags)
            {
                if (!tracker.CanConsume(tag, definition.RequiredCrisisSeverity))
                {
                    return Reject(definition, CandidateRejectReason.ProfessionalResponseAlreadyConsumed, "ProfessionalResponseAlreadyConsumed", available, requested, minimum);
                }
            }
        }

        if (definition.RequiresUndergroundFacility && context.FacilityState == FacilityState.Destroyed)
        {
            return Reject(definition, CandidateRejectReason.FacilityDestroyed, "FacilityDestroyed", available, requested, minimum);
        }

        if (available < minimum)
        {
            return Reject(definition, CandidateRejectReason.PersonnelBelowMinimum, "PersonnelBelowMinimum", available, requested, minimum);
        }

        int planned = Math.Min(requested, available);
        return new EventCandidate(definition, true, "Eligible", available, requested, minimum, planned);
    }

    private static CandidateRejectReason CheckCrisisRequirements(DirectorContext context, EventDefinition definition)
    {
        if (definition.RequiredCrisisTags.Count == 0)
        {
            return CandidateRejectReason.None;
        }

        if (context.CrisisAssessment is null)
        {
            return CandidateRejectReason.CrisisRequirementMissing;
        }

        foreach (CrisisTag tag in definition.RequiredCrisisTags)
        {
            if (!context.CrisisAssessment.IsActive(tag))
            {
                return CandidateRejectReason.CrisisRequirementMissing;
            }

            if (context.CrisisAssessment.GetSeverity(tag) < definition.RequiredCrisisSeverity)
            {
                return CandidateRejectReason.CrisisSeverityTooLow;
            }
        }

        return CandidateRejectReason.None;
    }

    private static int GetAvailablePersonnel(DirectorContext context, EventSource source)
    {
        return source switch
        {
            EventSource.Foundation => context.Personnel.FoundationAvailable,
            EventSource.Chaos => context.Personnel.ChaosAvailable,
            EventSource.Goi => context.Personnel.GoiAvailable,
            EventSource.ProfessionalCrisisResponse => context.Personnel.EligibleSpectators,
            _ => context.Personnel.EligibleSpectators,
        };
    }

    private static EventCandidate Reject(
        EventDefinition definition,
        CandidateRejectReason reason,
        string message,
        int available,
        int requested,
        int minimum)
    {
        return new EventCandidate(definition, false, message, available, requested, minimum, 0, reason);
    }
}
