namespace Monolithic.Api.Modules.Platform.Templates.Domain;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Master template definition — the "blueprint" for a reusable communication
/// artifact (email, SMS, PDF, push notification).
///
/// A <see cref="TemplateDefinition"/> is identified by its <see cref="Slug"/>
/// and resolved by <see cref="TemplateScope"/>: System → Business → User.
///
/// Actual content lives in <see cref="TemplateVersion"/> allowing full version
/// history and rollback without changing the definition record.
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
public class TemplateDefinition
{
    public Guid Id { get; set; }

    // ── Identity ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Stable, lowercase, dot-separated slug. Unique within a scope+business.
    /// Examples: "invoice.pdf", "password-reset.email", "system.2fa.sms".
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Human-readable name displayed in the template editor UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional markdown description / usage notes.</summary>
    public string? Description { get; set; }

    // ── Classification ────────────────────────────────────────────────────────

    public TemplateType Type { get; set; }

    public TemplateScope Scope { get; set; } = TemplateScope.System;

    // ── Ownership ─────────────────────────────────────────────────────────────

    /// <summary>Null for System-scope templates.</summary>
    public Guid? BusinessId { get; set; }

    /// <summary>Null unless Scope == User.</summary>
    public Guid? UserId { get; set; }

    // ── Template variables ────────────────────────────────────────────────────

    /// <summary>
    /// Comma-separated list of available Scriban variable names
    /// (e.g. "customer_name,invoice_number,total").
    /// Displayed in the UI template editor for discoverability.
    /// </summary>
    public string? AvailableVariables { get; set; }

    // ── Versioning ────────────────────────────────────────────────────────────

    /// <summary>ID of the currently active <see cref="TemplateVersion"/>.</summary>
    public Guid? ActiveVersionId { get; set; }

    /// <summary>Navigation: all historical versions of this template.</summary>
    public ICollection<TemplateVersion> Versions { get; set; } = [];

    // ── Audit ─────────────────────────────────────────────────────────────────

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }
}
