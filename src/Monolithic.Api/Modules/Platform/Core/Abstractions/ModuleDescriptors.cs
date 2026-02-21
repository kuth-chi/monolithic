namespace Monolithic.Api.Modules.Platform.Core.Abstractions;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Describes a UI widget that a module contributes to the dashboard catalog.
/// Stored once at startup; users select and position widgets in their personal
/// <c>UserPreference.DashboardLayout</c>.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed record WidgetDescriptor(
    /// <summary>Stable key, e.g. "inventory.low-stock-alert".</summary>
    string WidgetKey,

    /// <summary>Human-readable label shown in the widget picker.</summary>
    string DisplayName,

    /// <summary>Short markdown description shown in widget picker tooltips.</summary>
    string Description,

    /// <summary>Module that owns this widget.</summary>
    string ModuleId,

    /// <summary>Relative API endpoint that supplies widget data.</summary>
    string DataEndpoint,

    /// <summary>Default column-span (1–12 CSS grid columns).</summary>
    int DefaultColSpan = 4,

    /// <summary>Default row-span.</summary>
    int DefaultRowSpan = 2,

    /// <summary>Minimum column width this widget requires.</summary>
    int MinColSpan = 2,

    /// <summary>Whether this widget is enabled by default for new users.</summary>
    bool DefaultVisible = true,

    /// <summary>Optional icon identifier (e.g. Heroicons slug).</summary>
    string? Icon = null
);

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Describes a seed template provided by a module (Email / SMS / PDF / Push).
/// Seeded once on first run if no template with the same
/// <see cref="Slug"/> exists for the system scope.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public sealed record DefaultTemplateDescriptor(
    /// <summary>Stable slug, e.g. "invoice.pdf", "password-reset.email".</summary>
    string Slug,

    /// <summary>Display name shown in the template editor.</summary>
    string DisplayName,

    /// <summary><see cref="TemplateType"/> enum value name: Email, Sms, Pdf, Push.</summary>
    string TemplateType,

    /// <summary>Scriban template source. Supports {{ variable }} interpolation.</summary>
    string DefaultContent,

    /// <summary>Subject line (Email / Push). Not used for SMS/PDF.</summary>
    string? DefaultSubject = null,

    /// <summary>Comma-separated list of available variable names shown in the UI.</summary>
    string? AvailableVariables = null
);
