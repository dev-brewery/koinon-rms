using FluentValidation;
using Koinon.Application.DTOs.Security;

namespace Koinon.Application.Validators.Security;

/// <summary>
/// Validator for <see cref="AssignPersonRequest"/>.
/// </summary>
public class AssignPersonRequestValidator : AbstractValidator<AssignPersonRequest>
{
    public AssignPersonRequestValidator()
    {
        RuleFor(x => x.PersonIdKey)
            .NotEmpty().WithMessage("PersonIdKey is required");
    }
}
