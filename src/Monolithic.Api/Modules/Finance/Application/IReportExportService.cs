using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Finance.Domain;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Serialises a <see cref="FinancialReportDto"/> to PDF, Excel (.xlsx), or CSV byte arrays.
/// Each format preserves all sections, section subtotals, exchange-rate metadata, and summary totals.
/// </summary>
public interface IReportExportService
{
    /// <summary>
    /// Exports the report to the requested format.
    /// </summary>
    /// <param name="report">Pre-generated report data from <see cref="IFinancialReportService"/>.</param>
    /// <param name="format">Target output format.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple of:
    /// <list type="bullet">
    ///   <item><term>Data</term>Raw file bytes.</item>
    ///   <item><term>ContentType</term>MIME type string (e.g., "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").</item>
    ///   <item><term>FileName</term>Suggested download filename with extension.</item>
    /// </list>
    /// </returns>
    Task<(byte[] Data, string ContentType, string FileName)> ExportAsync(
        FinancialReportDto report,
        ExportFormat format,
        CancellationToken ct = default);
}
