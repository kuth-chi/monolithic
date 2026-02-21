using Monolithic.Api.Modules.Purchases.Contracts;

namespace Monolithic.Api.Modules.Purchases.Application;

public interface IPurchaseOrderService
{
    Task<IReadOnlyCollection<PurchaseOrderDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
    Task ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken cancellationToken = default);
    Task ReceiveAsync(Guid id, ReceivePurchaseOrderRequest request, Guid receivedByUserId, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default);
}
