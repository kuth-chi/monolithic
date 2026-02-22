using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Common.Results;

namespace Monolithic.Api.Common.Extensions;

/// <summary>
/// Controller extension methods that convert a <see cref="Result{T}"/>
/// into the correct <see cref="IActionResult"/> / HTTP status code.
///
/// Typical usage:
/// <code>
///   var result = await _service.GetByIdAsync(id, ct);
///   return result.ToActionResult(this);
///
///   // Or for created resources:
///   var result = await _service.CreateAsync(request, ct);
///   return result.ToCreatedResult(this, nameof(GetById), new { id = result.Value?.Id });
/// </code>
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to the appropriate <see cref="IActionResult"/>.
    /// Success → 200 OK with value.
    /// Failure → mapped ProblemDetails response.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller)
        => result.Match(
            value => controller.Ok(value),
            error => error.ToProblemResult(controller));

    /// <summary>Maps to 201 Created on success.</summary>
    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string actionName,
        object? routeValues = null)
        => result.Match(
            value => controller.CreatedAtAction(actionName, routeValues, value),
            error => error.ToProblemResult(controller));

    /// <summary>Maps to 204 No Content on success.</summary>
    public static IActionResult ToNoContentResult(
        this Result result,
        ControllerBase controller)
        => result.Match(
            () => (IActionResult)controller.NoContent(),
            error => error.ToProblemResult(controller));

    // ─────────────────────────────────────────────────────────────────────────

    private static IActionResult ToProblemResult(this Error error, ControllerBase controller)
    {
        var problem = new ProblemDetails
        {
            Title    = error.Type.ToTitle(),
            Detail   = error.Description,
            Instance = controller.HttpContext.Request.Path,
            Status   = error.Type.ToStatusCode(),
        };
        problem.Extensions["errorCode"] = error.Code;

        if (error.ValidationErrors is { Count: > 0 })
            problem.Extensions["errors"] = error.ValidationErrors;

        controller.HttpContext.Response.StatusCode = problem.Status!.Value;
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    private static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation   => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden    => StatusCodes.Status403Forbidden,
        ErrorType.NotFound     => StatusCodes.Status404NotFound,
        ErrorType.Conflict     => StatusCodes.Status409Conflict,
        ErrorType.DomainRule   => StatusCodes.Status422UnprocessableEntity,
        _                      => StatusCodes.Status500InternalServerError,
    };

    private static string ToTitle(this ErrorType type) => type switch
    {
        ErrorType.Validation   => "Validation Failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden    => "Forbidden",
        ErrorType.NotFound     => "Resource Not Found",
        ErrorType.Conflict     => "Conflict",
        ErrorType.DomainRule   => "Business Rule Violation",
        _                      => "Internal Server Error",
    };
}
