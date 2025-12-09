using FluentValidation;
using Koinon.Application.DTOs.VolunteerSchedule;
using Koinon.Domain.Enums;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateAssignmentStatusRequest.
/// Ensures status is valid and decline reason is provided when declining.
/// </summary>
public class UpdateAssignmentStatusRequestValidator : AbstractValidator<UpdateAssignmentStatusRequest>
{
    public UpdateAssignmentStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .Must(s => s == VolunteerScheduleStatus.Confirmed || s == VolunteerScheduleStatus.Declined)
            .WithMessage("Status must be Confirmed or Declined");

        RuleFor(x => x.DeclineReason)
            .NotEmpty()
            .When(x => x.Status == VolunteerScheduleStatus.Declined)
            .WithMessage("Decline reason is required when declining");

        RuleFor(x => x.DeclineReason)
            .MaximumLength(500)
            .When(x => x.DeclineReason != null);
    }
}
