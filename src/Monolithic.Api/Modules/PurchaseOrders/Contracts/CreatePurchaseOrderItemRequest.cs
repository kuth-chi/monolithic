using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.PurchaseOrders.Contracts;

public sealed class CreatePurchaseOrderItemRequest
{
    [Required]
    public Guid InventoryItemId { get; init; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Quantity { get; init; }

    /// <summary>
    /// Optional. If null or <= 0, the item's current CostPrice is used.
    /// </summary>
    public decimal? UnitPrice { get; init; }

    [MaxLength(500)]
    public string Notes { get; init; } = string.Empty;
}
