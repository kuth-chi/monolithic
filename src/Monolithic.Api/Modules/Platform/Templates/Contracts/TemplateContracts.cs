using Monolithic.Api.Modules.Platform.Templates.Domain;

namespace Monolithic.Api.Modules.Platform.Templates.Contracts;

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs — immutable records for API responses (serialization-friendly)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record TemplateDefinitionDto(
    Guid Id,
    string Slug,
    string DisplayName,
    string? Description,
    TemplateType Type,
    TemplateScope Scope,
    Guid? BusinessId,
    Guid? UserId,
    string? AvailableVariables,
    Guid? ActiveVersionId,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);

public sealed record TemplateVersionDto(
    Guid Id,
    Guid TemplateDefinitionId,
    string? Subject,
    string Content,
    string? PlainTextFallback,
    string VersionLabel,
    string? ChangeNotes,
    DateTimeOffset CreatedAtUtc
);

// ═══════════════════════════════════════════════════════════════════════════════
// Requests — validated input records
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record UpsertTemplateDefinitionRequest(
    string Slug,
    string DisplayName,
    string? Description,
    TemplateType Type,
    TemplateScope Scope,
    Guid? BusinessId,
    Guid? UserId,
    string? AvailableVariables
);

public sealed record CreateTemplateVersionRequest(
    Guid TemplateDefinitionId,
    string Content,
    string? Subject,
    string? PlainTextFallback,
    string VersionLabel,
    string? ChangeNotes,
    Guid CreatedByUserId,
    bool SetAsActive = true
);

public sealed record RenderTemplateRequest(
    /// <summary>Template slug, e.g. "invoice.pdf".</summary>
    string Slug,

    /// <summary>Variables to inject into the Scriban template.</summary>
    Dictionary<string, object?> Variables,

    /// <summary>
    /// Scope resolution order: tries User first, then Business, then System.
    /// Pass null to resolve System only.
    /// </summary>
    Guid? BusinessId = null,
    Guid? UserId = null
);

public sealed record RenderTemplateResult(
    bool Success,
    string? RenderedSubject,
    string? RenderedBody,
    string? PlainTextBody,
    string? ErrorMessage = null
);

public sealed record TemplateListRequest(
    TemplateType? Type = null,
    TemplateScope? Scope = null,
    Guid? BusinessId = null,
    bool ActiveOnly = true,
    int Page = 1,
    int PageSize = 20
);
