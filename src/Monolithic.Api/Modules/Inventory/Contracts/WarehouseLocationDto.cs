namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for a WarehouseLocation (bin / rack / shelf inside a warehouse).
/// </summary>
public sealed record WarehouseLocationDto(
    Guid Id,
    Guid WarehouseId,
    string WarehouseName,
    string Code,
    string Name,
    string Zone,
    string Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);
