using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record CreateWarehouseLocationRequest(
    [Required] Guid WarehouseId,
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(200)] string Name,
    [MaxLength(100)] string Zone,
    [MaxLength(500)] string Description
);
