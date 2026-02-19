using Monolithic.Api.Modules.Finance.Contracts;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Generates multi-currency financial reports (P&amp;L, Balance Sheet, Trial Balance)
/// from posted General Ledger journal entries, with IAS 21 exchange-rate translation.
/// </summary>
public interface IFinancialReportService
{
    /// <summary>
    /// Generates a financial report based on the request parameters.
    /// Supports consolidation across multiple entities and currency translation using
    /// Average, Current, or Historical rates per IAS 21.
    /// </summary>
    /// <param name="request">Report configuration including period, currencies, consolidation level.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Fully populated <see cref="FinancialReportDto"/> ready for display or export.</returns>
    Task<FinancialReportDto> GenerateAsync(FinancialReportRequest request, CancellationToken ct = default);
}
