using FluentValidation;
using Monolithic.Api.Common.Results;

namespace Monolithic.Api.Common.Validation;

/// <summary>
/// Base validator for all request DTOs / commands across every module.
///
/// Provides helper methods so each module validator stays readable and DRY:
///   - <see cref="ValidateToResult{T}"/> — runs validation and converts
///     failures to a <see cref="Result{T}"/> with an <see cref="ErrorType.Validation"/> error.
///
/// Usage:
/// <code>
///   public sealed class CreateCustomerValidator : AppValidator&lt;CreateCustomerRequest&gt;
///   {
///       public CreateCustomerValidator()
///       {
///           RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
///           RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
///       }
///   }
/// </code>
/// </summary>
public abstract class AppValidator<T> : AbstractValidator<T>
{
    /// <summary>
    /// Validates <paramref name="instance"/> and returns a <see cref="Result{TOut}"/>
    /// failure with aggregated validation errors if any rule fails.
    /// </summary>
    public Result<TOut> ValidateToResult<TOut>(T instance)
    {
        var result = Validate(instance);
        if (result.IsValid)
            throw new InvalidOperationException(
                "ValidateToResult should only be called when you intend to convert failures; " +
                "call Validate() directly when you want the full ValidationResult.");

        var errors = result.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return Error.Validation(
            $"{typeof(T).Name}.Invalid",
            "One or more validation errors occurred.",
            errors);
    }

    /// <summary>
    /// Validates <paramref name="instance"/> and returns a <see cref="Result{TOut}"/>:
    ///   - Success if valid (no value — callers must supply the value later)
    ///   - Failure with validation errors if invalid
    /// Returns a unit-like result indicating whether validation passed.
    /// </summary>
    public Result ValidateAsResult(T instance)
    {
        var result = Validate(instance);
        if (result.IsValid) return Result.Ok;

        var errors = result.Errors
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        return Error.Validation(
            $"{typeof(T).Name}.Invalid",
            "One or more validation errors occurred.",
            errors);
    }
}

/// <summary>Common reusable validation rule-sets shared across all modules.</summary>
public static class CommonRules
{
    /// <summary>ISO 4217 three-letter currency code (e.g. "USD", "KHR").</summary>
    public static IRuleBuilderOptions<T, string> IsCurrencyCode<T>(
        this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$").WithMessage("'{PropertyName}' must be a 3-letter ISO 4217 currency code.");

    /// <summary>ISO 3166-1 alpha-2 country code (e.g. "US", "KH").</summary>
    public static IRuleBuilderOptions<T, string> IsCountryCode<T>(
        this IRuleBuilder<T, string> rule)
        => rule
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$").WithMessage("'{PropertyName}' must be a 2-letter ISO 3166-1 alpha-2 country code.");

    /// <summary>Positive monetary amount (> 0).</summary>
    public static IRuleBuilderOptions<T, decimal> IsPositiveAmount<T>(
        this IRuleBuilder<T, decimal> rule)
        => rule.GreaterThan(0).WithMessage("'{PropertyName}' must be greater than 0.");

    /// <summary>Non-negative monetary amount (≥ 0).</summary>
    public static IRuleBuilderOptions<T, decimal> IsNonNegativeAmount<T>(
        this IRuleBuilder<T, decimal> rule)
        => rule.GreaterThanOrEqualTo(0).WithMessage("'{PropertyName}' must be 0 or greater.");

    /// <summary>IANA IETF BCP-47 locale string (e.g. "en-US", "km-KH").</summary>
    public static IRuleBuilderOptions<T, string?> IsLocale<T>(
        this IRuleBuilder<T, string?> rule)
        => rule
            .Matches(@"^[a-z]{2,3}(-[A-Z]{2,3})?$")
            .When(x => x is not null)
            .WithMessage("'{PropertyName}' must be a valid BCP-47 locale (e.g. 'en-US').");

    /// <summary>Safe short text — no HTML/script injection characters.</summary>
    public static IRuleBuilderOptions<T, string> IsSafeText<T>(
        this IRuleBuilder<T, string> rule)
        => rule
            .Matches(@"^[^<>""';]*$")
            .WithMessage("'{PropertyName}' must not contain HTML or script characters.");
}
