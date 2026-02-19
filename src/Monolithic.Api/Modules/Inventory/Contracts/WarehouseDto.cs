namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Read model for a Warehouse.
/// </summary>
public sealed record WarehouseDto(
    Guid Id,
    Guid BusinessId,
    string Code,
    string Name,
    string Description,
    string Address,
    string City,
    string StateProvince,
    string Country,
    string PostalCode,
    bool IsDefault,
    bool IsActive,
    int LocationCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);
