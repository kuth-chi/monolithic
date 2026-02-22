namespace Monolithic.Api.Common.Results;

/// <summary>
/// Discriminated union of possible error kinds.
/// Maps 1-to-1 with HTTP status codes in <see cref="ResultExtensions"/>.
/// </summary>
public enum ErrorType
{
    /// <summary>400 Bad Request — caller input is invalid.</summary>
    Validation,

    /// <summary>401 Unauthorized — missing or invalid credentials.</summary>
    Unauthorized,

    /// <summary>403 Forbidden — authenticated but lacks permission.</summary>
    Forbidden,

    /// <summary>404 Not Found — requested resource does not exist.</summary>
    NotFound,

    /// <summary>409 Conflict — state conflict (duplicate, stale version, etc.).</summary>
    Conflict,

    /// <summary>422 Unprocessable Entity — domain / business rule violation.</summary>
    DomainRule,

    /// <summary>500 Internal Server Error — unexpected failure.</summary>
    Unexpected,
}

/// <summary>
/// Immutable, lightweight error descriptor.
/// Carry semantic meaning via <see cref="Type"/> and include a developer-facing
/// <see cref="Description"/>.  UI localisation must live in the presentation layer.
/// </summary>
public sealed record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }

    /// <summary>For validation errors — per-field error details (nullable).</summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    private Error(string code, string description, ErrorType type,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        Code = code;
        Description = description;
        Type = type;
        ValidationErrors = validationErrors;
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static Error NotFound(string code, string description)
        => new(code, description, ErrorType.NotFound);

    public static Error NotFound<T>(object id)
        => new($"{typeof(T).Name}.NotFound",
               $"{typeof(T).Name} with id '{id}' was not found.",
               ErrorType.NotFound);

    public static Error Validation(string code, string description,
        IDictionary<string, string[]>? errors = null)
        => new(code, description, ErrorType.Validation,
               errors?.AsReadOnly());

    public static Error Conflict(string code, string description)
        => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code = "Auth.Unauthorized",
        string description = "Authentication is required.")
        => new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code = "Auth.Forbidden",
        string description = "You do not have permission to perform this action.")
        => new(code, description, ErrorType.Forbidden);

    public static Error DomainRule(string code, string description)
        => new(code, description, ErrorType.DomainRule);

    public static Error Unexpected(string code = "Unexpected.Error",
        string description = "An unexpected error occurred.")
        => new(code, description, ErrorType.Unexpected);

    public override string ToString() => $"[{Type}] {Code}: {Description}";
}
