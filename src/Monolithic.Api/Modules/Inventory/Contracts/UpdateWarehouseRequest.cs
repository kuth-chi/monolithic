using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record UpdateWarehouseRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string Description,
    [MaxLength(300)] string Address,
    [MaxLength(100)] string City,
    [MaxLength(100)] string StateProvince,
    [MaxLength(100)] string Country,
    [MaxLength(20)] string PostalCode,
    bool IsDefault,
    bool IsActive
);
