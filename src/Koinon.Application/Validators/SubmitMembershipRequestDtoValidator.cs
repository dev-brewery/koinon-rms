using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

/// <summary>
/// Validator for SubmitMembershipRequestDto.
/// </summary>
public class SubmitMembershipRequestDtoValidator : AbstractValidator<SubmitMembershipRequestDto>
{
    public SubmitMembershipRequestDtoValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Note cannot exceed 2000 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));
    }
}
