using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ── Estimate PO Contracts ─────────────────────────────────────────────────────

public sealed class EstimatePurchaseOrderItemDto
{
    public Guid Id { get; init; }
    public Guid InventoryItemId { get; init; }
    public string InventoryItemName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public Guid? InventoryItemVariantId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal LineTotal { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class EstimatePurchaseOrderDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public string RfqNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset RequestDateUtc { get; init; }
    public DateTimeOffset? QuoteReceivedDateUtc { get; init; }
    public DateTimeOffset? QuoteExpiryDateUtc { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public decimal SubTotal { get; init; }
    public string OrderDiscountType { get; init; } = string.Empty;
    public decimal OrderDiscountValue { get; init; }
    public decimal OrderDiscountAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal TotalAmountBase { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string VendorQuoteReference { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyList<EstimatePurchaseOrderItemDto> Items { get; init; } = [];
}

public sealed class CreateEstimateLineRequest
{
    public Guid InventoryItemId { get; init; }
    public Guid? InventoryItemVariantId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public DiscountType DiscountType { get; init; } = DiscountType.None;
    public decimal DiscountValue { get; init; }
    public decimal TaxRate { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class CreateEstimatePurchaseOrderRequest
{
    public Guid BusinessId { get; init; }
    public Guid VendorId { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1;
    public DiscountType OrderDiscountType { get; init; } = DiscountType.None;
    public decimal OrderDiscountValue { get; init; }
    public decimal ShippingFee { get; init; }
    public string Notes { get; init; } = string.Empty;
    public IList<CreateEstimateLineRequest> Items { get; init; } = [];
}

/// <summary>Request to convert an approved RFQ into a Purchase Order.</summary>
public sealed class ConvertEstimateToPurchaseOrderRequest
{
    /// <summary>Override the exchange rate if different from when the RFQ was created.</summary>
    public decimal? ExchangeRateOverride { get; init; }
    public string Notes { get; init; } = string.Empty;
}
