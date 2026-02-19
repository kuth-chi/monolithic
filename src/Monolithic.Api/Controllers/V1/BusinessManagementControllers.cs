using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Identity.Authorization;
using Monolithic.Api.Modules.Business.Application;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Business settings: primary color, timezone, currency, calendar, attendance defaults.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/settings")]
public sealed class BusinessSettingsController(IBusinessSettingService settingService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> Get(Guid businessId, CancellationToken ct)
        => Ok(await settingService.GetAsync(businessId, ct));

    [HttpPut]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Upsert(Guid businessId, [FromBody] UpsertBusinessSettingRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        return Ok(await settingService.UpsertAsync(request, ct));
    }
}

/// <summary>
/// Business branding media — logo, cover header, favicon.
/// Each type supports one active (current) file at a time; old uploads are retained for history.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/media")]
public sealed class BusinessMediaController(IBusinessMediaService mediaService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken ct)
        => Ok(await mediaService.GetAllAsync(businessId, ct));

    [HttpGet("{mediaType}")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetCurrent(Guid businessId, BusinessMediaType mediaType, CancellationToken ct)
    {
        var result = await mediaService.GetCurrentAsync(businessId, mediaType, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Upload a new logo, cover header, or other media for the business.
    /// Previous current media of the same type is automatically demoted.
    /// Accepted: image/jpeg, image/png, image/webp.
    /// </summary>
    [HttpPost("{mediaType}")]
    [RequirePermission("business:write")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> Upload(
        Guid businessId,
        BusinessMediaType mediaType,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");
        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLower()))
            return BadRequest("Only JPEG, PNG and WEBP images are accepted.");

        var result = await mediaService.UploadAsync(businessId, mediaType, file, ct);
        return Ok(result);
    }

    [HttpDelete("{mediaId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid mediaId, CancellationToken ct)
    {
        await mediaService.DeleteAsync(mediaId, ct);
        return NoContent();
    }
}

/// <summary>
/// Business holiday calendar management.
/// Includes manual holidays and auto-import from country public holiday calendars.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/holidays")]
public sealed class BusinessHolidaysController(IBusinessHolidayService holidayService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetByYear(Guid businessId, [FromQuery] int? year, CancellationToken ct)
        => Ok(await holidayService.GetByYearAsync(businessId, year ?? DateTime.UtcNow.Year, ct));

    [HttpPost]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Create(Guid businessId, [FromBody] CreateHolidayRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        return Ok(await holidayService.CreateAsync(request, ct));
    }

    [HttpPut("{holidayId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Update(Guid businessId, Guid holidayId, [FromBody] UpdateHolidayRequest request, CancellationToken ct)
        => Ok(await holidayService.UpdateAsync(holidayId, request, ct));

    [HttpDelete("{holidayId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid holidayId, CancellationToken ct)
    {
        await holidayService.DeleteAsync(holidayId, ct);
        return NoContent();
    }

    /// <summary>
    /// Auto-imports public holidays from the country calendar (Nager.Date or similar).
    /// Uses the business's HolidayCountryCode setting if countryCode is not specified.
    /// </summary>
    [HttpPost("import")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Import(
        Guid businessId,
        [FromQuery] string countryCode,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var count = await holidayService.ImportPublicHolidaysAsync(businessId, countryCode, year ?? DateTime.UtcNow.Year, ct);
        return Ok(new { imported = count });
    }

    /// <summary>Quickly checks if a specific date is a holiday for this business.</summary>
    [HttpGet("check")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> Check(Guid businessId, [FromQuery] DateOnly date, CancellationToken ct)
        => Ok(new { isHoliday = await holidayService.IsHolidayAsync(businessId, date, ct) });
}

/// <summary>
/// Attendance policy management.
/// Policies cascade: Individual → Department → Branch → Business default.
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/attendance-policies")]
public sealed class AttendancePoliciesController(IAttendancePolicyService policyService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetAll(Guid businessId, CancellationToken ct)
        => Ok(await policyService.GetByBusinessAsync(businessId, ct));

    [HttpGet("{policyId:guid}")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> GetById(Guid businessId, Guid policyId, CancellationToken ct)
    {
        var result = await policyService.GetByIdAsync(policyId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Upsert(Guid businessId, [FromBody] UpsertAttendancePolicyRequest request, CancellationToken ct)
    {
        if (request.BusinessId != businessId) return BadRequest("BusinessId mismatch.");
        return Ok(await policyService.UpsertAsync(request, ct));
    }

    [HttpDelete("{policyId:guid}")]
    [RequirePermission("business:write")]
    public async Task<IActionResult> Delete(Guid businessId, Guid policyId, CancellationToken ct)
    {
        await policyService.DeleteAsync(policyId, ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the effective attendance policy for a specific employee,
    /// resolved by walking the priority chain: Individual → Department → Branch → Business default.
    /// </summary>
    [HttpGet("resolve/employees/{employeeId:guid}")]
    [RequirePermission("business:read")]
    public async Task<IActionResult> ResolveForEmployee(Guid businessId, Guid employeeId, CancellationToken ct)
    {
        var result = await policyService.ResolveForEmployeeAsync(businessId, employeeId, ct);
        return result is null ? NotFound("No attendance policy could be resolved.") : Ok(result);
    }
}
