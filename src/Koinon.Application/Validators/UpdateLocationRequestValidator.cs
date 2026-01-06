using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

public class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().When(x => x.Name != null)
            .WithMessage("Location name cannot be empty.");

        RuleFor(x => x.SoftRoomThreshold)
            .GreaterThanOrEqualTo(0).When(x => x.SoftRoomThreshold.HasValue)
            .WithMessage("Soft room threshold must be non-negative.");

        RuleFor(x => x.FirmRoomThreshold)
            .GreaterThanOrEqualTo(0).When(x => x.FirmRoomThreshold.HasValue)
            .WithMessage("Firm room threshold must be non-negative.");
    }
}
