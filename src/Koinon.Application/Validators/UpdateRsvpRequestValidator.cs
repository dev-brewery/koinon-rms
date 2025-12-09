using FluentValidation;
using Koinon.Application.DTOs.GroupMeeting;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for UpdateRsvpRequest.
/// </summary>
public class UpdateRsvpRequestValidator : AbstractValidator<UpdateRsvpRequest>
{
    public UpdateRsvpRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid RSVP status");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note cannot exceed 500 characters");
    }
}
