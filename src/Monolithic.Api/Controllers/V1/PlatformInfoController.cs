using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Platform.Core.Infrastructure;

namespace Monolithic.Api.Controllers.V1;

// ═══════════════════════════════════════════════════════════════════════════════
/// <summary>
/// Platform meta-information — registered modules, widget catalog, system status.
///
/// GET  /api/v1/platform/modules   – all registered modules (id, version, deps)
/// GET  /api/v1/platform/widgets   – all available dashboard widgets across modules
/// </summary>
// ═══════════════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/v1/platform")]
public sealed class PlatformInfoController(ModuleRegistry registry) : ControllerBase
{
    [HttpGet("modules")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetModules()
        => Ok(registry.Modules.Select(m => new
        {
            m.ModuleId,
            m.DisplayName,
            m.Version,
            Dependencies = m.Dependencies.ToArray(),
        }));

    [HttpGet("widgets")]
    [RequirePermission("platform:info:read")]
    public IActionResult GetWidgets()
        => Ok(registry.GetAllWidgets());
}
