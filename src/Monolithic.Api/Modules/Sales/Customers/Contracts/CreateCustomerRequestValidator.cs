using FluentValidation;
using Monolithic.Api.Common.Validation;

namespace Monolithic.Api.Modules.Sales.Customers.Contracts;

/// <summary>
/// Validates <see cref="CreateCustomerRequest"/> using FluentValidation.
/// Replaces DataAnnotations attributes — registered automatically via
/// <c>services.AddValidatorsFromAssemblyContaining&lt;Program&gt;()</c>.
///
/// OWASP A03 — Injection:
///   - <c>IsSafeText()</c> blocks HTML/script characters in free-text fields.
///   - <c>MaximumLength()</c> prevents buffer-overflow-style inputs.
/// </summary>
public sealed class CreateCustomerRequestValidator : AppValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.BusinessId)
            .NotEmpty().WithMessage("BusinessId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200)
            .IsSafeText();

        RuleFor(x => x.CustomerCode)
            .MaximumLength(50)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerCode));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(@"^[\d\s\+\-\(\)]+$").WithMessage("Phone number contains invalid characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.TaxId)
            .MaximumLength(50)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.TaxId));

        RuleFor(x => x.PaymentTerms)
            .MaximumLength(100)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.PaymentTerms));

        RuleFor(x => x.Website)
            .MaximumLength(300)
            .Must(uri => string.IsNullOrWhiteSpace(uri) ||
                         Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
                         (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Website must be a valid HTTP/HTTPS URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x.Address)
            .MaximumLength(300)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(100)
            .IsSafeText()
            .When(x => !string.IsNullOrWhiteSpace(x.City));
    }
}
