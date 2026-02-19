namespace Monolithic.Api.Modules.PurchaseOrders.Contracts;

public sealed class PurchaseOrderDto
{
    public Guid Id { get; init; }

    public Guid BusinessId { get; init; }

    public Guid VendorId { get; init; }

    public string VendorName { get; init; } = string.Empty;

    public string PoNumber { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset OrderDateUtc { get; init; }

    public DateTimeOffset? ExpectedDeliveryDateUtc { get; init; }

    public DateTimeOffset? ReceivedDateUtc { get; init; }

    public decimal TotalAmount { get; init; }

    public string Notes { get; init; } = string.Empty;

    public Guid? CreatedByUserId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public IReadOnlyCollection<PurchaseOrderItemDto> Items { get; init; } = [];
}
