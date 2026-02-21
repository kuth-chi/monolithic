using Monolithic.Api.Modules.Business.Contracts;

namespace Monolithic.Api.Modules.Business.Application;

public interface IEstimatePurchaseOrderService
{
    Task<EstimatePurchaseOrderDto> CreateAsync(CreateEstimatePurchaseOrderRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<EstimatePurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<EstimatePurchaseOrderDto>> GetByBusinessAsync(
        Guid businessId,
        string? statusFilter = null,
        Guid? vendorId = null,
        CancellationToken ct = default);

    Task SendToVendorAsync(Guid estimateId, CancellationToken ct = default);
    Task RecordVendorQuoteAsync(Guid estimateId, DateTimeOffset quoteExpiry, string vendorQuoteRef, CancellationToken ct = default);
    Task ApproveAsync(Guid estimateId, Guid approvedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Converts an approved RFQ into a Purchase Order.
    /// </summary>
    Task<Monolithic.Api.Modules.Purchases.Contracts.PurchaseOrderDto> ConvertToPurchaseOrderAsync(
        Guid estimateId,
        ConvertEstimateToPurchaseOrderRequest request,
        Guid createdByUserId,
        CancellationToken ct = default);
}
