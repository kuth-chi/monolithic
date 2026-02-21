using Monolithic.Api.Modules.Purchases.Contracts;

namespace Monolithic.Api.Modules.Purchases.Application;

public interface IPurchaseReturnService
{
    Task<IReadOnlyList<PurchaseReturnDto>> GetByBusinessAsync(Guid businessId, Guid? vendorId = null, string? status = null, CancellationToken ct = default);
    Task<PurchaseReturnDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default);
    Task MarkShippedAsync(Guid id, CancellationToken ct = default);
    Task RecordVendorCreditAsync(Guid id, RecordVendorCreditRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
}
