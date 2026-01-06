using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

public class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Location name is required.");

        RuleFor(x => x.SoftRoomThreshold)
            .GreaterThanOrEqualTo(0).When(x => x.SoftRoomThreshold.HasValue)
            .WithMessage("Soft room threshold must be non-negative.");

        RuleFor(x => x.FirmRoomThreshold)
            .GreaterThanOrEqualTo(0).When(x => x.FirmRoomThreshold.HasValue)
            .WithMessage("Firm room threshold must be non-negative.");

        RuleFor(x => x)
            .Must(x => !x.SoftRoomThreshold.HasValue || !x.FirmRoomThreshold.HasValue || x.SoftRoomThreshold.Value <= x.FirmRoomThreshold.Value)
            .WithMessage("Soft room threshold must be less than or equal to firm room threshold.");
    }
}
