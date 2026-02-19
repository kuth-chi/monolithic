using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Inventory.Contracts;

public sealed record CreateInventoryItemRequest(
    [Required] Guid BusinessId,
    [Required, MaxLength(100)] string Sku,
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string Description,
    [MaxLength(50)] string Unit,
    decimal ReorderLevel,
    decimal ReorderQuantity,
    decimal CostPrice,
    decimal SellingPrice
);
