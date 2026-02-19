using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Inventory.Contracts;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Application;

public sealed class WarehouseService(ApplicationDbContext db) : IWarehouseService
{
    // ── Warehouses ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<WarehouseDto>> GetWarehousesAsync(
        Guid? businessId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Warehouses
            .Include(w => w.Locations)
            .AsQueryable();

        if (businessId.HasValue)
            query = query.Where(w => w.BusinessId == businessId.Value);

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        var warehouses = await query.OrderBy(w => w.Name).ToListAsync(cancellationToken);
        return warehouses.Select(MapWarehouseToDto).ToList();
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await db.Warehouses
            .Include(w => w.Locations)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        return warehouse is null ? null : MapWarehouseToDto(warehouse);
    }

    public async Task<WarehouseDto> CreateWarehouseAsync(
        CreateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        // Enforce single default per business
        if (request.IsDefault)
        {
            await ClearDefaultFlagAsync(request.BusinessId, null, cancellationToken);
        }

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Address = request.Address ?? string.Empty,
            City = request.City ?? string.Empty,
            StateProvince = request.StateProvince ?? string.Empty,
            Country = request.Country ?? string.Empty,
            PostalCode = request.PostalCode ?? string.Empty,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(cancellationToken);
        return MapWarehouseToDto(warehouse);
    }

    public async Task<WarehouseDto?> UpdateWarehouseAsync(
        Guid id,
        UpdateWarehouseRequest request,
        CancellationToken cancellationToken = default)
    {
        var warehouse = await db.Warehouses
            .Include(w => w.Locations)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (warehouse is null) return null;

        // Enforce single default per business
        if (request.IsDefault && !warehouse.IsDefault)
        {
            await ClearDefaultFlagAsync(warehouse.BusinessId, id, cancellationToken);
        }

        warehouse.Name = request.Name;
        warehouse.Description = request.Description ?? string.Empty;
        warehouse.Address = request.Address ?? string.Empty;
        warehouse.City = request.City ?? string.Empty;
        warehouse.StateProvince = request.StateProvince ?? string.Empty;
        warehouse.Country = request.Country ?? string.Empty;
        warehouse.PostalCode = request.PostalCode ?? string.Empty;
        warehouse.IsDefault = request.IsDefault;
        warehouse.IsActive = request.IsActive;
        warehouse.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapWarehouseToDto(warehouse);
    }

    public async Task<bool> DeleteWarehouseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse is null) return false;

        db.Warehouses.Remove(warehouse);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Warehouse Locations ────────────────────────────────────────────────

    public async Task<IReadOnlyCollection<WarehouseLocationDto>> GetLocationsAsync(
        Guid warehouseId,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.WarehouseLocations
            .Include(wl => wl.Warehouse)
            .Where(wl => wl.WarehouseId == warehouseId)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(wl => wl.IsActive == isActive.Value);

        var locations = await query.OrderBy(wl => wl.Code).ToListAsync(cancellationToken);
        return locations.Select(MapLocationToDto).ToList();
    }

    public async Task<WarehouseLocationDto?> GetLocationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await db.WarehouseLocations
            .Include(wl => wl.Warehouse)
            .FirstOrDefaultAsync(wl => wl.Id == id, cancellationToken);

        return location is null ? null : MapLocationToDto(location);
    }

    public async Task<WarehouseLocationDto> CreateLocationAsync(
        CreateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = new WarehouseLocation
        {
            Id = Guid.NewGuid(),
            WarehouseId = request.WarehouseId,
            Code = request.Code,
            Name = request.Name,
            Zone = request.Zone ?? string.Empty,
            Description = request.Description ?? string.Empty,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.WarehouseLocations.Add(location);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(location).Reference(l => l.Warehouse).LoadAsync(cancellationToken);
        return MapLocationToDto(location);
    }

    public async Task<WarehouseLocationDto?> UpdateLocationAsync(
        Guid id,
        UpdateWarehouseLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var location = await db.WarehouseLocations
            .Include(wl => wl.Warehouse)
            .FirstOrDefaultAsync(wl => wl.Id == id, cancellationToken);

        if (location is null) return null;

        location.Name = request.Name;
        location.Zone = request.Zone ?? string.Empty;
        location.Description = request.Description ?? string.Empty;
        location.IsActive = request.IsActive;
        location.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return MapLocationToDto(location);
    }

    public async Task<bool> DeleteLocationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var location = await db.WarehouseLocations.FirstOrDefaultAsync(wl => wl.Id == id, cancellationToken);
        if (location is null) return false;

        db.WarehouseLocations.Remove(location);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private async Task ClearDefaultFlagAsync(Guid businessId, Guid? excludeId, CancellationToken cancellationToken)
    {
        var existing = await db.Warehouses
            .Where(w => w.BusinessId == businessId && w.IsDefault && (excludeId == null || w.Id != excludeId))
            .ToListAsync(cancellationToken);

        foreach (var w in existing)
            w.IsDefault = false;
    }

    private static WarehouseDto MapWarehouseToDto(Warehouse w) => new(
        w.Id,
        w.BusinessId,
        w.Code,
        w.Name,
        w.Description,
        w.Address,
        w.City,
        w.StateProvince,
        w.Country,
        w.PostalCode,
        w.IsDefault,
        w.IsActive,
        w.Locations.Count,
        w.CreatedAtUtc,
        w.ModifiedAtUtc
    );

    private static WarehouseLocationDto MapLocationToDto(WarehouseLocation wl) => new(
        wl.Id,
        wl.WarehouseId,
        wl.Warehouse.Name,
        wl.Code,
        wl.Name,
        wl.Zone,
        wl.Description,
        wl.IsActive,
        wl.CreatedAtUtc,
        wl.ModifiedAtUtc
    );
}
