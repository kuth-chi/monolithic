using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public interface IVendorBillService
{
    // ── Bill CRUD ─────────────────────────────────────────────────────────────
    Task<VendorBillDto> CreateAsync(CreateVendorBillRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<VendorBillDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<VendorBillDto>> GetByBusinessAsync(
        Guid businessId,
        string? statusFilter = null,
        Guid? vendorId = null,
        CancellationToken ct = default);

    Task ConfirmAsync(Guid billId, Guid confirmedByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid billId, string reason, CancellationToken ct = default);

    // ── Payments ──────────────────────────────────────────────────────────────
    Task<VendorBillPaymentDto> RecordPaymentAsync(Guid billId, RecordVendorBillPaymentRequest request, Guid createdByUserId, CancellationToken ct = default);

    // ── Overdue ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns overdue bills per vendor for the given business.
    /// A bill is overdue when DueDate &lt; today and AmountDue &gt; 0.
    /// </summary>
    Task<IReadOnlyList<VendorOverdueSummaryDto>> GetOverdueSummaryByVendorAsync(Guid businessId, CancellationToken ct = default);

    Task<IReadOnlyList<VendorBillDto>> GetOverdueBillsAsync(Guid businessId, Guid? vendorId = null, CancellationToken ct = default);

    /// <summary>
    /// Recalculates DaysOverdue and marks bills with AmountDue &gt; 0 and DueDate &lt; today as Overdue.
    /// Should be called by a daily background job.
    /// </summary>
    Task RefreshOverdueStatusAsync(Guid businessId, CancellationToken ct = default);
}
