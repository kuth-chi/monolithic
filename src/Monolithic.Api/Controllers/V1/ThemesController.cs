using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Themes.Application;
using Monolithic.Api.Modules.Platform.Themes.Contracts;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Full design-token theme management per business.
///
/// GET    /api/v1/themes                – list all themes for a business
/// GET    /api/v1/themes/default        – get active default theme
/// POST   /api/v1/themes               – create/update a theme profile
/// PUT    /api/v1/themes/{id}/default  – set as default
/// DELETE /api/v1/themes/{id}          – delete a non-default theme
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/themes")]
public sealed class ThemesController(IThemeService themeService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("platform:themes:read")]
    public async Task<IActionResult> List([FromQuery] Guid? businessId, CancellationToken ct)
        => Ok(await themeService.ListAsync(businessId, ct));

    [HttpGet("default")]
    [RequirePermission("platform:themes:read")]
    public async Task<IActionResult> GetDefault([FromQuery] Guid? businessId, CancellationToken ct)
    {
        var result = await themeService.GetDefaultAsync(businessId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("platform:themes:write")]
    public async Task<IActionResult> Upsert(
        [FromBody] UpsertThemeProfileRequest req, CancellationToken ct)
        => Ok(await themeService.UpsertAsync(req, ct));

    [HttpPut("{id:guid}/default")]
    [RequirePermission("platform:themes:write")]
    public async Task<IActionResult> SetDefault(Guid id, CancellationToken ct)
    {
        await themeService.SetDefaultAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("platform:themes:write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await themeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
