using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Platform.Core.Abstractions;
using Monolithic.Api.Modules.Platform.Templates.Contracts;
using Monolithic.Api.Modules.Platform.Templates.Domain;

namespace Monolithic.Api.Modules.Platform.Templates.Application;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// CRUD and version management for <see cref="TemplateDefinition"/> and
/// <see cref="TemplateVersion"/> entities.
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public interface ITemplateService
{
    /// <summary>Paginated list with optional filters.</summary>
    Task<PagedResult<TemplateDefinitionDto>> ListAsync(TemplateListRequest req, CancellationToken ct = default);

    /// <summary>Get by internal ID.</summary>
    Task<TemplateDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Resolve by slug using scope-fall-through:
    /// UserId scope → BusinessId scope → System scope.
    /// Returns null if no template is found in any scope.
    /// </summary>
    Task<TemplateDefinitionDto?> ResolveAsync(string slug, Guid? businessId, Guid? userId, CancellationToken ct = default);

    /// <summary>Create a new definition or return the existing one by slug+scope.</summary>
    Task<TemplateDefinitionDto> UpsertDefinitionAsync(UpsertTemplateDefinitionRequest req, CancellationToken ct = default);

    /// <summary>Add a new version and optionally activate it.</summary>
    Task<TemplateVersionDto> AddVersionAsync(CreateTemplateVersionRequest req, CancellationToken ct = default);

    /// <summary>Activate a specific historical version.</summary>
    Task ActivateVersionAsync(Guid definitionId, Guid versionId, CancellationToken ct = default);

    /// <summary>List all versions for a definition.</summary>
    Task<IReadOnlyList<TemplateVersionDto>> GetVersionsAsync(Guid definitionId, CancellationToken ct = default);

    /// <summary>Soft-delete (deactivate) a template definition.</summary>
    Task DeactivateAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Seed system-scope default templates from module descriptors.
    /// Skips slugs that already exist to make startup idempotent.
    /// </summary>
    Task SeedDefaultsAsync(IEnumerable<DefaultTemplateDescriptor> descriptors, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// Renders a template's Scriban content against a dynamic variable dictionary.
/// Isolated into its own interface so the rendering engine can be swapped
/// without touching CRUD logic (Strategy pattern).
/// </summary>
// ─────────────────────────────────────────────────────────────────────────────
public interface ITemplateRenderService
{
    /// <summary>
    /// Render subject + body using Scriban.
    /// Returns <see cref="RenderTemplateResult.Success"/> = false if the
    /// template contains a syntax error, with the error captured in
    /// <see cref="RenderTemplateResult.ErrorMessage"/>.
    /// </summary>
    Task<RenderTemplateResult> RenderAsync(RenderTemplateRequest req, CancellationToken ct = default);

    /// <summary>
    /// Validate template syntax without rendering.
    /// Returns null on success, or an error message string on failure.
    /// </summary>
    Task<string?> ValidateSyntaxAsync(string templateContent, CancellationToken ct = default);
}
