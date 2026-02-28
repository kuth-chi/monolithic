using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Identity.Contracts;

/// <summary>
/// Validates <see cref="SignUpRequest"/>.
///
/// OWASP A07 — Identification &amp; Authentication Failures:
///   - Email uniqueness is enforced at the service layer (not here) to give a
///     clear 409 Conflict rather than a validation error, which leaks less detail.
///   - Password confirmation must match before the service layer hashes anything.
///   - Minimum policy mirrors Identity options: 8 chars, upper, lower, digit.
///   - Upper cap on password prevents DoS via bcrypt with huge payloads (OWASP A04).
/// </summary>
public sealed class SignUpRequestValidator : AppValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
