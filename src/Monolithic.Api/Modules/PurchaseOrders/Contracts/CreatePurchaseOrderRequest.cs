using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.PurchaseOrders.Contracts;

public sealed class CreatePurchaseOrderRequest
{
    [Required]
    public Guid BusinessId { get; init; }

    [Required]
    public Guid VendorId { get; init; }

    public DateTimeOffset? ExpectedDeliveryDateUtc { get; init; }

    [MaxLength(1000)]
    public string Notes { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<CreatePurchaseOrderItemRequest> Items { get; init; } = [];
}
