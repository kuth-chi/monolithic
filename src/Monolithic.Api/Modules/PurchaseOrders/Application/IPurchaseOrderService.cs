using Monolithic.Api.Modules.PurchaseOrders.Contracts;

namespace Monolithic.Api.Modules.PurchaseOrders.Application;

public interface IPurchaseOrderService
{
    Task<IReadOnlyCollection<PurchaseOrderDto>> GetAllAsync(Guid? businessId = null, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);
}
