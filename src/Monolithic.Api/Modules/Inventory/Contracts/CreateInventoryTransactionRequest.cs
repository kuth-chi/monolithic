using System.ComponentModel.DataAnnotations;
using Monolithic.Api.Modules.Inventory.Domain;

namespace Monolithic.Api.Modules.Inventory.Contracts;

/// <summary>
/// Request to record an inventory movement (In / Out / Adjustment / Transfer).
/// </summary>
public sealed record CreateInventoryTransactionRequest(
    [Required] Guid InventoryItemId,
    [Required] Guid WarehouseLocationId,
    [Required] InventoryTransactionType TransactionType,
    decimal Quantity,
    [MaxLength(100)] string ReferenceNumber,
    [MaxLength(500)] string Notes
);
