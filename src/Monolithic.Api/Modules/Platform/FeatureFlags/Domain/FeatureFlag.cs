namespace Monolithic.Api.Modules.Platform.FeatureFlags.Domain;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// A named boolean feature gate with optional JSON metadata for rollout config
/// (percentage rollout, segment targeting, expiry date, etc.).
///
/// Resolution order (first match wins):
///   1. User-scope flag for (UserId, BusinessId, Key)
///   2. Business-scope flag for (BusinessId, Key)
///   3. System-scope flag for (Key)
///
/// If no record exists for a key, behaviour is determined by the calling code's
/// default — conventionally <c>false</c> (opt-in model).
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public class FeatureFlag
{
    public Guid Id { get; set; }

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lowercase hyphen-separated key, e.g. "advanced-analytics", "beta-pos-ui".
    /// Unique within a (Scope, BusinessId, UserId) tuple.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    // ── Scope ─────────────────────────────────────────────────────────────────

    public FeatureFlagScope Scope { get; set; } = FeatureFlagScope.System;

    /// <summary>Null for System scope.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>Null unless Scope == User.</summary>
    public Guid? UserId { get; set; }

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Optional UTC expiry. When past, the flag behaves as disabled regardless
    /// of <see cref="IsEnabled"/>. Used for time-boxed beta features.
    /// </summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    // ── Rollout metadata (optional JSON) ──────────────────────────────────────

    /// <summary>
    /// Arbitrary JSON for advanced targeting rules:
    /// <c>{ "rolloutPercent": 10, "segments": ["enterprise"] }</c>
    /// Parsed by the service; basic boolean check is always safe.
    /// </summary>
    public string? MetadataJson { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public DateTimeOffset CreatedAtUtc   { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
public enum FeatureFlagScope
{
    /// <summary>Applies to all businesses (platform-level toggle).</summary>
    System = 0,

    /// <summary>Applies to one specific business.</summary>
    Business = 1,

    /// <summary>Applies to one user within a business.</summary>
    User = 2,
}
