namespace Monolithic.Api.Modules.PurchaseOrders.Contracts;

public sealed class PurchaseOrderItemDto
{
    public Guid Id { get; init; }

    public Guid InventoryItemId { get; init; }

    public string InventoryItemName { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal QuantityReceived { get; init; }

    public decimal LineTotal { get; init; }

    public string Notes { get; init; } = string.Empty;
}
