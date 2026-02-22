using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Themes.Application;
using Monolithic.Api.Modules.Platform.Themes.Contracts;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Full design-token theme management per business.
///
/// Public (no auth):
///   GET    /api/v1/themes/shadcn-variables        – ShadCN/Tailwind CSS vars
///
/// Authenticated (platform:themes:read):
///   GET    /api/v1/themes                         – list all themes for a business
///   GET    /api/v1/themes/default                 – get active default theme DTO
///
/// Authenticated (platform:themes:write):
///   POST   /api/v1/themes                         – create/update a theme profile
///   PUT    /api/v1/themes/{id}/default            – set as default
///   POST   /api/v1/themes/{id}/extract-logo-colors – run logo colour extraction
///   DELETE /api/v1/themes/{id}                    – delete a non-default theme
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/themes")]
public sealed class ThemesController(IThemeService themeService) : ControllerBase
{
    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns ShadCN/Tailwind-compatible CSS variable maps for the business.
    /// Falls back to the system default theme when the business has no custom theme.
    ///
    /// This endpoint is intentionally unauthenticated so the frontend can
    /// inject CSS variables before the user logs in (e.g. landing pages, login form).
    /// All returned values are read-only design tokens — no sensitive data.
    /// </summary>
    [HttpGet("shadcn-variables")]
    public async Task<IActionResult> GetShadcnVariables(
        [FromQuery] Guid? businessId, CancellationToken ct)
    {
        var result = await themeService.GetShadcnCssVarsAsync(businessId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Authenticated reads ───────────────────────────────────────────────────

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

    // ── Authenticated writes ──────────────────────────────────────────────────

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

    /// <summary>
    /// Downloads the logo at the given URL, extracts dominant colours and
    /// optionally overwrites ColorPrimary / ColorSecondary on the theme profile.
    /// </summary>
    [HttpPost("{id:guid}/extract-logo-colors")]
    [RequirePermission("platform:themes:write")]
    public async Task<IActionResult> ExtractLogoColors(
        Guid id, [FromBody] ExtractLogoColorsRequest req, CancellationToken ct)
        => Ok(await themeService.ExtractLogoColorsAsync(id, req, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission("platform:themes:write")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await themeService.DeleteAsync(id, ct);
        return NoContent();
    }
}
