using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for ProcessMembershipRequestDto.
/// </summary>
public class ProcessMembershipRequestDtoValidator : AbstractValidator<ProcessMembershipRequestDto>
{
    public ProcessMembershipRequestDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(status => status is "Approved" or "Denied")
            .WithMessage("Status must be either 'Approved' or 'Denied'");

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Note cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
