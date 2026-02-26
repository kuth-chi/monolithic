using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Users.Contracts;

public sealed class CreatePermissionRequestValidator : AppValidator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .MaximumLength(100).WithMessage("Source must not exceed 100 characters.");

        RuleFor(x => x.Group)
            .MaximumLength(100).WithMessage("Group must not exceed 100 characters.");

        RuleFor(x => x.Feature)
            .NotEmpty().WithMessage("Feature is required.")
            .MaximumLength(150).WithMessage("Feature must not exceed 150 characters.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(50).WithMessage("Action must not exceed 50 characters.");

        RuleFor(x => x.Permission)
            .MaximumLength(256).WithMessage("Permission key must not exceed 256 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Permission));

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(512).WithMessage("Description must not exceed 512 characters.");
    }
}
