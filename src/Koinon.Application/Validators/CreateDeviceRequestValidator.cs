using FluentValidation;
using Koinon.Application.DTOs.Requests;

namespace Koinon.Application.Validators;

public class CreateDeviceRequestValidator : AbstractValidator<CreateDeviceRequest>
{
    public CreateDeviceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Device name is required.")
            .MaximumLength(200).WithMessage("Device name cannot exceed 200 characters.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(45).WithMessage("IP address cannot exceed 45 characters.")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));
    }
}
