using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Templates.Application;
using Monolithic.Api.Modules.Platform.Templates.Contracts;
using Monolithic.Api.Modules.Platform.Templates.Domain;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Template CRUD, versioning, and Scriban-based rendering.
///
/// Endpoints:
///   GET    /api/v1/templates                    – paginated list
///   GET    /api/v1/templates/{id}               – get by ID
///   GET    /api/v1/templates/resolve/{slug}     – scope-resolved lookup
///   POST   /api/v1/templates/definitions        – create/upsert definition
///   POST   /api/v1/templates/versions           – add version
///   PUT    /api/v1/templates/{id}/activate/{versionId} – activate version
///   GET    /api/v1/templates/{id}/versions      – list versions
///   DELETE /api/v1/templates/{id}               – deactivate
///   POST   /api/v1/templates/render             – render template with data
///   POST   /api/v1/templates/validate           – syntax check only
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/templates")]
public sealed class TemplatesController(
    ITemplateService templateService,
    ITemplateRenderService renderService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> List([FromQuery] TemplateListRequest req, CancellationToken ct)
        => Ok(await templateService.ListAsync(req, ct));

    [HttpGet("{id:guid}")]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await templateService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("resolve/{slug}")]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> Resolve(
        string slug,
        [FromQuery] Guid? businessId,
        [FromQuery] Guid? userId,
        CancellationToken ct)
    {
        var result = await templateService.ResolveAsync(slug, businessId, userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("definitions")]
    [RequirePermission("platform:templates:write")]
    public async Task<IActionResult> UpsertDefinition(
        [FromBody] UpsertTemplateDefinitionRequest req, CancellationToken ct)
        => Ok(await templateService.UpsertDefinitionAsync(req, ct));

    [HttpPost("versions")]
    [RequirePermission("platform:templates:write")]
    public async Task<IActionResult> AddVersion(
        [FromBody] CreateTemplateVersionRequest req, CancellationToken ct)
        => Ok(await templateService.AddVersionAsync(req, ct));

    [HttpPut("{id:guid}/activate/{versionId:guid}")]
    [RequirePermission("platform:templates:write")]
    public async Task<IActionResult> ActivateVersion(
        Guid id, Guid versionId, CancellationToken ct)
    {
        await templateService.ActivateVersionAsync(id, versionId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/versions")]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
        => Ok(await templateService.GetVersionsAsync(id, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("platform:templates:write")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await templateService.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Render a named template with provided variables.
    /// Returns rendered subject + body.
    /// </summary>
    [HttpPost("render")]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> Render(
        [FromBody] RenderTemplateRequest req, CancellationToken ct)
    {
        var result = await renderService.RenderAsync(req, ct);
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });
        return Ok(result);
    }

    /// <summary>Validate Scriban template syntax without rendering.</summary>
    [HttpPost("validate")]
    [RequirePermission("platform:templates:read")]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateTemplateRequest req, CancellationToken ct)
    {
        var error = await renderService.ValidateSyntaxAsync(req.Content, ct);
        return Ok(new { valid = error is null, error });
    }
}

public sealed record ValidateTemplateRequest(string Content);
