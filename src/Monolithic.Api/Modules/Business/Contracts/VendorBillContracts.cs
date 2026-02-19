using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ── Vendor Bill DTOs ─────────────────────────────────────────────────────────

public sealed class VendorBillItemDto
{
    public Guid Id { get; init; }
    public Guid? PurchaseOrderItemId { get; init; }
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
    public decimal LineTotalBeforeDiscount { get; init; }
    public decimal LineTotalAfterDiscount { get; init; }
    public decimal LineTotal { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class VendorBillPaymentDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public decimal AmountBase { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateOnly PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class VendorBillDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public Guid? PurchaseOrderId { get; init; }
    public string? PoNumber { get; init; }
    public Guid? ChartOfAccountId { get; init; }
    public string? ChartOfAccountName { get; init; }
    public string BillNumber { get; init; } = string.Empty;
    public string VendorInvoiceNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateOnly BillDate { get; init; }
    public DateOnly DueDate { get; init; }
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
    public decimal AmountPaid { get; init; }
    public decimal AmountDue { get; init; }
    public int DaysOverdue { get; init; }
    public bool IsOverdue { get; init; }
    public string Notes { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyList<VendorBillItemDto> Items { get; init; } = [];
    public IReadOnlyList<VendorBillPaymentDto> Payments { get; init; } = [];
}

// ── Vendor Bill Requests ──────────────────────────────────────────────────────

public sealed class CreateVendorBillItemRequest
{
    public Guid? PurchaseOrderItemId { get; init; }
    public Guid InventoryItemId { get; init; }
    public Guid? InventoryItemVariantId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public DiscountType DiscountType { get; init; } = DiscountType.None;
    public decimal DiscountValue { get; init; }
    public decimal TaxRate { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class CreateVendorBillRequest
{
    public Guid BusinessId { get; init; }
    public Guid VendorId { get; init; }
    public Guid? PurchaseOrderId { get; init; }
    public Guid? ChartOfAccountId { get; init; }
    public string VendorInvoiceNumber { get; init; } = string.Empty;
    public DateOnly BillDate { get; init; }
    public DateOnly DueDate { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1;
    public DiscountType OrderDiscountType { get; init; } = DiscountType.None;
    public decimal OrderDiscountValue { get; init; }
    public decimal ShippingFee { get; init; }
    public string Notes { get; init; } = string.Empty;
    public IList<CreateVendorBillItemRequest> Items { get; init; } = [];
}

public sealed class RecordVendorBillPaymentRequest
{
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1;
    public DateOnly PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = "BankTransfer";
    public Guid? BankAccountId { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

// ── Overdue Summary ───────────────────────────────────────────────────────────

public sealed class VendorOverdueSummaryDto
{
    public Guid VendorId { get; init; }
    public string VendorName { get; init; } = string.Empty;
    public int OverdueBillCount { get; init; }
    public decimal TotalOverdueAmount { get; init; }
    public decimal TotalOverdueAmountBase { get; init; }
    public string BaseCurrencyCode { get; init; } = string.Empty;
    public int MaxDaysOverdue { get; init; }
    public IReadOnlyList<VendorBillDto> OverdueBills { get; init; } = [];
}
