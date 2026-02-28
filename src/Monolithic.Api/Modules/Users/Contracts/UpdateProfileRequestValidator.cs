using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Users.Contracts;

/// <summary>Server-side FluentValidation for <see cref="UpdateProfileRequest"/>.</summary>
public sealed class UpdateProfileRequestValidator : AppValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.")
            .Matches(@"^[\p{L}\p{M}'\-\s\.]+$")
            .WithMessage("Full name contains invalid characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number must not exceed 30 characters.")
            .Matches(@"^[+\d\s\-()\.]*$").WithMessage("Phone number contains invalid characters.")
            .When(x => x.PhoneNumber is not null);
    }
}
