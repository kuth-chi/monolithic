using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Users.Contracts;

public sealed class UpdateRolePermissionsRequestValidator : AppValidator<UpdateRolePermissionsRequest>
{
    public UpdateRolePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds)
            .NotNull().WithMessage("PermissionIds is required.");

        RuleFor(x => x.PermissionIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("PermissionIds must be unique.");
    }
}
