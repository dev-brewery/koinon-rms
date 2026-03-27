using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

public class UpdateDeviceRequestValidator : AbstractValidator<UpdateDeviceRequest>
{
    public UpdateDeviceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device name cannot be empty.")
            .MaximumLength(200).WithMessage("Device name cannot exceed 200 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address cannot exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));
    }
}
