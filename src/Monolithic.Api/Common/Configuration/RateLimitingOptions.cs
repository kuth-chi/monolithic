namespace Monolithic.Api.Common.Configuration;

/// <summary>Rate limiting thresholds — tunable per environment via appsettings.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // ── Auth endpoints (login, register, forgot-password) ────────────────────
    /// <summary>Max requests per <see cref="AuthWindowMinutes"/> for auth endpoints.</summary>
    public int AuthPermitLimit { get; init; } = 10;

    /// <summary>Rolling window (minutes) for the auth sliding-window limiter.</summary>
    public int AuthWindowMinutes { get; init; } = 1;

    // ── Standard API token bucket ─────────────────────────────────────────────
    /// <summary>Token bucket capacity for standard API requests.</summary>
    public int ApiTokenLimit { get; init; } = 100;

    /// <summary>Tokens replenished per minute for standard API requests.</summary>
    public int ApiTokensPerPeriod { get; init; } = 100;

    // ── Global fixed-window fallback ──────────────────────────────────────────
    /// <summary>Max requests per minute for any unlabelled endpoint.</summary>
    public int FixedPermitLimit { get; init; } = 200;
}
