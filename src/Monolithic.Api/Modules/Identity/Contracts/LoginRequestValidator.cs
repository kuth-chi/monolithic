using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Identity.Contracts;

/// <summary>
/// Validates <see cref="LoginRequest"/>.
///
/// OWASP A07 — Identification &amp; Authentication Failures:
///   - Email length cap prevents enumeration via oversized inputs.
///   - Password length cap prevents DoS via massive password hashing.
///   - No specific format validation on password to avoid leaking policy details.
/// </summary>
public sealed class LoginRequestValidator : AppValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            // Cap to prevent DoS via bcrypt with huge passwords (OWASP A04)
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.");
    }
}
