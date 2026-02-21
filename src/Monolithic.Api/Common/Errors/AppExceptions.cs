namespace Monolithic.Api.Common.Errors;

// ═══════════════════════════════════════════════════════════════════════════════
// Domain exception hierarchy
// All handlers in GlobalExceptionHandler map these to appropriate HTTP status
// codes and RFC-7807 ProblemDetails bodies.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Resource not found — maps to 404 Not Found.</summary>
public sealed class NotFoundException(string message, string? resourceType = null, object? resourceId = null)
    : AppException(message)
{
    public string? ResourceType { get; } = resourceType;
    public object? ResourceId   { get; } = resourceId;

    public static NotFoundException For<T>(object id) =>
        new($"{typeof(T).Name} with id '{id}' was not found.", typeof(T).Name, id);
}

/// <summary>Business/domain rule violation — maps to 422 Unprocessable Entity.</summary>
public sealed class DomainException(string message) : AppException(message);

/// <summary>Request payload is invalid — maps to 400 Bad Request.</summary>
public sealed class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = (errors ?? new Dictionary<string, string[]>())
            .AsReadOnly();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors.AsReadOnly();
    }
}

/// <summary>Caller is not authenticated — maps to 401 Unauthorized.</summary>
public sealed class UnauthorizedException(string message = "Authentication is required.")
    : AppException(message);

/// <summary>Caller is authenticated but lacks permission — maps to 403 Forbidden.</summary>
public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : AppException(message);

/// <summary>Action would create a duplicate resource — maps to 409 Conflict.</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Base class for all application-specific exceptions.</summary>
public abstract class AppException(string message) : Exception(message);
