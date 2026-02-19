using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Finance.Application;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Identity.Authorization;
using System.Security.Claims;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// General Ledger Journal Entries API.
///
/// Endpoints:
///   GET    /api/v1/businesses/{businessId}/gl/journal-entries           — paged list / GL view
///   GET    /api/v1/businesses/{businessId}/gl/journal-entries/{id}      — detail + drill-down
///   POST   /api/v1/businesses/{businessId}/gl/journal-entries           — create draft entry
///   POST   /api/v1/businesses/{businessId}/gl/journal-entries/{id}/post — post entry to GL
///   POST   /api/v1/businesses/{businessId}/gl/journal-entries/{id}/reverse — reverse posted entry
///   GET    /api/v1/businesses/{businessId}/gl/journal-entries/{id}/audit — audit trail
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/gl/journal-entries")]
public sealed class JournalEntriesController(IJournalEntryService journalEntryService) : ControllerBase
{
    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paged GL view of journal entries for a business.
    /// Supports filtering by fiscal period, date range, status, source type, account, and free text.
    /// Each posted entry in the result links back to its source document.
    /// </summary>
    [HttpGet]
    [RequirePermission("accounting:read")]
    [ProducesResponseType<PagedResult<JournalEntryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        Guid businessId,
        [FromQuery] string? fiscalPeriod,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] int? status,
        [FromQuery] int? sourceType,
        [FromQuery] Guid? accountId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new JournalEntryFilter
        {
            BusinessId = businessId,
            FiscalPeriod = fiscalPeriod,
            TransactionDateFrom = dateFrom,
            TransactionDateTo = dateTo,
            Status = status.HasValue
                ? (JournalEntryStatus)status.Value
                : null,
            SourceType = sourceType.HasValue
                ? (JournalEntrySourceType)sourceType.Value
                : null,
            AccountId = accountId,
            SearchTerm = search,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 200)
        };

        return Ok(await journalEntryService.GetPagedAsync(filter, ct));
    }

    /// <summary>
    /// Returns a single journal entry with all lines and audit log.
    /// Satisfies the acceptance criterion for drill-down to source document.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission("accounting:read")]
    [ProducesResponseType<JournalEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid businessId, Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await journalEntryService.GetByIdAsync(id, userId, ct);
        if (result is null || result.BusinessId != businessId) return NotFound();
        return Ok(result);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new Draft journal entry (supports ≥ 999 lines).
    /// Does not enforce balance at this stage — use /post when ready.
    /// </summary>
    [HttpPost]
    [RequirePermission("accounting:create")]
    [ProducesResponseType<JournalEntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        Guid businessId,
        [FromBody] CreateJournalEntryRequest request,
        CancellationToken ct)
    {
        if (request.BusinessId != businessId)
            return BadRequest("BusinessId in URL and body do not match.");

        var (userId, displayName) = GetCurrentUserContext();

        var result = await journalEntryService.CreateAsync(request, userId, displayName, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    /// <summary>
    /// Posts a Draft entry to the GL.
    /// System enforces: balanced totals (Σ debits = Σ credits), ≥ 2 lines, valid accounts.
    /// Once posted, the entry is immutable.
    /// </summary>
    [HttpPost("{id:guid}/post")]
    [RequirePermission("accounting:create")]
    [ProducesResponseType<JournalEntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Post(
        Guid businessId,
        Guid id,
        [FromBody] PostJournalEntryRequest request,
        CancellationToken ct)
    {
        var (userId, displayName) = GetCurrentUserContext();
        var result = await journalEntryService.PostAsync(id, request, userId, displayName, ct);
        return Ok(result);
    }

    /// <summary>
    /// Reverses a Posted entry by generating a balanced mirror-image entry.
    /// The original entry is marked Reversed; the new entry is auto-posted with status Reversal.
    /// Both entries are linked together for full drill-down.
    /// </summary>
    [HttpPost("{id:guid}/reverse")]
    [RequirePermission("accounting:create")]
    [ProducesResponseType<JournalEntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reverse(
        Guid businessId,
        Guid id,
        [FromBody] ReverseJournalEntryRequest request,
        CancellationToken ct)
    {
        var (userId, displayName) = GetCurrentUserContext();
        var result = await journalEntryService.ReverseAsync(id, request, userId, displayName, ct);
        return CreatedAtAction(nameof(GetById), new { businessId, id = result.Id }, result);
    }

    /// <summary>
    /// Returns the full audit trail for a journal entry:
    /// who created, posted, or reversed it with timestamps.
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [RequirePermission("accounting:read")]
    [ProducesResponseType<IReadOnlyList<JournalEntryAuditLogDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs(Guid businessId, Guid id, CancellationToken ct)
        => Ok(await journalEntryService.GetAuditLogsAsync(id, ct));

    // ── Private helpers ───────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private (Guid UserId, string DisplayName) GetCurrentUserContext()
    {
        var userId = GetCurrentUserId();
        var displayName = User.FindFirstValue("name")
                       ?? User.FindFirstValue(ClaimTypes.Name)
                       ?? User.FindFirstValue(ClaimTypes.Email)
                       ?? "System";
        return (userId, displayName);
    }
}
