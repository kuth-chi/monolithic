using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Purchases.Contracts;

public sealed class UpdatePurchaseOrderRequest
{
    public DateTimeOffset? ExpectedDeliveryDateUtc { get; init; }

    [MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;

    [MinLength(1)]
    public IReadOnlyCollection<CreatePurchaseOrderItemRequest>? Items { get; init; }
}

public sealed class ReceivePurchaseOrderRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<ReceivePurchaseOrderItemRequest> Items { get; init; } = [];

    [MaxLength(500)]
    public string Notes { get; init; } = string.Empty;
}

public sealed class ReceivePurchaseOrderItemRequest
{
    [Required]
    public Guid PurchaseOrderItemId { get; init; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal QuantityReceived { get; init; }
}
