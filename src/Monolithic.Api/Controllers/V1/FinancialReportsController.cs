using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Modules.Finance.Application;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Finance.Domain;
using Monolithic.Api.Modules.Identity.Authorization;

namespace Monolithic.Api.Controllers.V1;

/// <summary>
/// Multi-currency financial reports: Profit &amp; Loss, Balance Sheet, and Trial Balance.
///
/// Supports:
///   • ≥5 concurrent currencies with IAS 21 exchange-rate translation (Average / Current / Historical).
///   • Group, Company, or Division consolidation level.
///   • Export to PDF, Excel (.xlsx), or CSV with full formatting.
/// </summary>
[ApiController]
[Route("api/v1/financial-reports")]
public sealed class FinancialReportsController(
    IFinancialReportService reportService,
    IReportExportService exportService) : ControllerBase
{
    // ── Generate (JSON response) ──────────────────────────────────────────────

    /// <summary>
    /// Generates a Profit &amp; Loss statement for the requested period and currencies.
    /// Revenue and Expense accounts only; summarises Gross Profit and Net Income.
    /// </summary>
    [HttpPost("profit-and-loss")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> ProfitAndLoss(
        [FromBody] FinancialReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with { ReportType = FinancialReportType.ProfitAndLoss };
        var result = await reportService.GenerateAsync(canonical, ct);
        return Ok(result);
    }

    /// <summary>
    /// Generates a Balance Sheet (statement of financial position) as of the <c>ToDate</c>.
    /// Cumulative balances for Assets, Liabilities, and Equity; validates Assets = Liabilities + Equity.
    /// </summary>
    [HttpPost("balance-sheet")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> BalanceSheet(
        [FromBody] FinancialReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with { ReportType = FinancialReportType.BalanceSheet };
        var result = await reportService.GenerateAsync(canonical, ct);
        return Ok(result);
    }

    /// <summary>
    /// Generates a Trial Balance listing all account Debit and Credit movement totals.
    /// Useful for pre-adjustment period-end review and audit validation.
    /// </summary>
    [HttpPost("trial-balance")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> TrialBalance(
        [FromBody] FinancialReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with { ReportType = FinancialReportType.TrialBalance };
        var result = await reportService.GenerateAsync(canonical, ct);
        return Ok(result);
    }

    /// <summary>
    /// Generic endpoint — use when the report type is provided inside the request body.
    /// Useful for programmatic / batch reporting.
    /// </summary>
    [HttpPost("generate")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> Generate(
        [FromBody] FinancialReportRequest request,
        CancellationToken ct)
        => Ok(await reportService.GenerateAsync(request, ct));

    // ── Export (file download) ────────────────────────────────────────────────

    /// <summary>
    /// Generates a report and immediately exports it to PDF, Excel, or CSV.
    /// Returns the file as a binary download with the appropriate MIME type.
    /// </summary>
    [HttpPost("export")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> Export(
        [FromBody] ExportReportRequest request,
        CancellationToken ct)
    {
        var report = await reportService.GenerateAsync(request.ReportRequest, ct);
        var (data, contentType, fileName) = await exportService.ExportAsync(report, request.Format, ct);
        return File(data, contentType, fileName);
    }

    /// <summary>
    /// Convenience: generate and export a Profit &amp; Loss report in one call.
    /// Format choices: Csv = 1, Excel = 2, Pdf = 3.
    /// </summary>
    [HttpPost("profit-and-loss/export")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> ExportPnL(
        [FromBody] ExportReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with
        {
            ReportRequest = request.ReportRequest with { ReportType = FinancialReportType.ProfitAndLoss }
        };
        return await Export(canonical, ct);
    }

    /// <summary>
    /// Convenience: generate and export a Balance Sheet in one call.
    /// </summary>
    [HttpPost("balance-sheet/export")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> ExportBalanceSheet(
        [FromBody] ExportReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with
        {
            ReportRequest = request.ReportRequest with { ReportType = FinancialReportType.BalanceSheet }
        };
        return await Export(canonical, ct);
    }

    /// <summary>
    /// Convenience: generate and export a Trial Balance in one call.
    /// </summary>
    [HttpPost("trial-balance/export")]
    [RequirePermission("finance:read")]
    public async Task<IActionResult> ExportTrialBalance(
        [FromBody] ExportReportRequest request,
        CancellationToken ct)
    {
        var canonical = request with
        {
            ReportRequest = request.ReportRequest with { ReportType = FinancialReportType.TrialBalance }
        };
        return await Export(canonical, ct);
    }
}
