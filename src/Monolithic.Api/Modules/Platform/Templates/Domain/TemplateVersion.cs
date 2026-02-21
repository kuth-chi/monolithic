namespace Monolithic.Api.Modules.Platform.Templates.Domain;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// An immutable, timestamped version of a template's content.
///
/// Creating a new version never modifies history; the parent
/// <see cref="TemplateDefinition.ActiveVersionId"/> pointer is updated to
/// activate the new version. Full rollback is trivially supported.
///
/// The Scriban template engine is used for {{ variable }} interpolation,
/// supporting conditionals, loops, formatting filters, etc.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public class TemplateVersion
{
    public Guid Id { get; set; }

    public Guid TemplateDefinitionId { get; set; }

    public TemplateDefinition Definition { get; set; } = null!;

    // ── Content ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Subject line for Email / Push notifications.
    /// May also contain Scriban variables: "Invoice {{ invoice_number }} – {{ business_name }}".
    /// Not used for SMS / PDF types.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Main template body using Scriban syntax.
    /// For Pdf type this is a structured JSON layout descriptor consumed by the PDF renderer.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text fallback for Email templates
    /// (auto-stripped HTML alternative sent with multipart/alternative MIME).
    /// </summary>
    public string? PlainTextFallback { get; set; }

    // ── Metadata ──────────────────────────────────────────────────────────────

    /// <summary>SemVer-style label, e.g. "1.0", "2.1".</summary>
    public string VersionLabel { get; set; } = "1.0";

    /// <summary>Optional change notes describing what was updated in this version.</summary>
    public string? ChangeNotes { get; set; }

    // ── Audit ─────────────────────────────────────────────────────────────────

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
