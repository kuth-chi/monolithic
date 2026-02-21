namespace Monolithic.Api.Modules.Platform.Templates.Domain;

/// <summary>
/// Classifies what a template is used for and which renderer handles it.
/// Drives channel routing in <see cref="INotificationService"/> and the
/// PDF generation pipeline.
/// </summary>
public enum TemplateType
{
    /// <summary>HTML/plain-text email body. Rendered via Scriban → sent via SMTP/API.</summary>
    Email = 1,

    /// <summary>Plain-text SMS up to ~160 chars. Rendered via Scriban → sent via SMS gateway.</summary>
    Sms = 2,

    /// <summary>PDF document layouts. Rendered via Scriban → QuestPDF for binary output.</summary>
    Pdf = 3,

    /// <summary>Web Push notification title + body. Short text, rendered via Scriban.</summary>
    Push = 4,

    /// <summary>In-app rich notification body (HTML snippet).</summary>
    InApp = 5,
}
