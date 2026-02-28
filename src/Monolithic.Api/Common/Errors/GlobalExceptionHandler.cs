using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Monolithic.Api.Common.Errors;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Centralized exception → ProblemDetails (RFC 7807) mapper.
/// Registered via <c>services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c>
/// and activated by <c>app.UseExceptionHandler()</c>.
///
/// Mapping table:
///   NotFoundException         → 404 Not Found
///   ValidationException       → 400 Bad Request  (includes field errors)
///   DomainException           → 422 Unprocessable Entity
///   UnauthorizedException     → 401 Unauthorized
///   ForbiddenException        → 403 Forbidden
///   ConflictException         → 409 Conflict
///   OperationCanceledException → 499 Client Closed Request  (no body)
///   Everything else           → 500 Internal Server Error
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Let the runtime absorb cancelled requests gracefully — no response needed.
        if (exception is OperationCanceledException)
        {
            httpContext.Response.StatusCode = 499; // Client Closed Request
            return true;
        }

        var (statusCode, title, detail, extensions) = MapException(exception);

        logger.LogError(
            exception,
            "Unhandled exception: {ExceptionType} — {Detail}",
            exception.GetType().Name,
            detail);

        var problem = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = httpContext.Request.Path,
        };

        foreach (var (key, value) in extensions)
            problem.Extensions[key] = value;

        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private static (int statusCode, string title, string detail, Dictionary<string, object?> extensions)
        MapException(Exception exception) => exception switch
    {
        NotFoundException ex => (
            StatusCodes.Status404NotFound,
            "Resource Not Found",
            ex.Message,
            BuildExtensions(ex.ResourceType is null
                ? null
                : new { resourceType = ex.ResourceType, resourceId = ex.ResourceId?.ToString() })),

        ValidationException ex => (
            StatusCodes.Status400BadRequest,
            "Validation Failed",
            ex.Message,
            BuildExtensions(new { errors = ex.Errors })),

        DomainException ex => (
            StatusCodes.Status422UnprocessableEntity,
            "Business Rule Violation",
            ex.Message,
            BuildExtensions(null)),

        UnauthorizedException ex => (
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            ex.Message,
            BuildExtensions(null)),

        ForbiddenException ex => (
            StatusCodes.Status403Forbidden,
            "Forbidden",
            ex.Message,
            BuildExtensions(null)),

        ConflictException ex => (
            StatusCodes.Status409Conflict,
            "Conflict",
            ex.Message,
            BuildExtensions(null)),

        LicenseException ex => (
            StatusCodes.Status402PaymentRequired,
            "License Required",
            ex.Message,
            BuildExtensions(new { code = ex.Code.ToString() })),

        ArgumentException ex => (
            StatusCodes.Status400BadRequest,
            "Bad Request",
            ex.Message,
            BuildExtensions(null)),

        _ => (
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "An unexpected error occurred. Please try again later.",
            BuildExtensions(null))
    };

    private static Dictionary<string, object?> BuildExtensions(object? data)
    {
        var dict = new Dictionary<string, object?>();
        if (data is not null)
            dict["data"] = data;
        return dict;
    }
}
