using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Monolithic.Api.Common.Validation;

/// <summary>
/// Global action filter that auto-validates every request body annotated with
/// <c>[FromBody]</c> before the controller action executes.
///
/// If a matching <see cref="IValidator{T}"/> is registered in DI, validation
/// runs automatically.  On failure, a 400 ProblemDetails response with per-field
/// errors is returned — the action method is never invoked.
///
/// Registration:
///   services.AddControllers(o => o.Filters.Add&lt;ValidationActionFilter&gt;());
///
/// OWASP A03 — Injection: combined with <see cref="CommonRules.IsSafeText{T}"/>
/// rules this layer blocks HTML/script injection at the boundary.
/// </summary>
public sealed class ValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var (_, value) in context.ActionArguments)
        {
            if (value is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(value);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (result.IsValid) continue;

            var errors = result.Errors
                .GroupBy(f => f.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(f => f.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status   = StatusCodes.Status400BadRequest,
                Title    = "Validation Failed",
                Instance = context.HttpContext.Request.Path,
            };

            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        await next();
    }
}
