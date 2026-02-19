using Monolithic.Api.Modules.Inventory.Contracts;

namespace Monolithic.Api.Modules.Inventory.Application;

public interface IWarehouseService
{
    // ── Warehouses ─────────────────────────────────────────────────────────
    Task<IReadOnlyCollection<WarehouseDto>> GetWarehousesAsync(Guid? businessId = null, bool? isActive = null, CancellationToken cancellationToken = default);

    Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);

    Task<WarehouseDto?> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteWarehouseAsync(Guid id, CancellationToken cancellationToken = default);

    // ── Warehouse Locations ────────────────────────────────────────────────
    Task<IReadOnlyCollection<WarehouseLocationDto>> GetLocationsAsync(Guid warehouseId, bool? isActive = null, CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto?> GetLocationByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto> CreateLocationAsync(CreateWarehouseLocationRequest request, CancellationToken cancellationToken = default);

    Task<WarehouseLocationDto?> UpdateLocationAsync(Guid id, UpdateWarehouseLocationRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteLocationAsync(Guid id, CancellationToken cancellationToken = default);
}
