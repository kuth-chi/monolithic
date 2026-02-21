using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record SalesInvoiceDto(
    Guid Id, Guid BusinessId, Guid CustomerId, Guid? SalesOrderId,
    string InvoiceNumber, string CustomerReference, string Status,
    DateOnly InvoiceDate, DateOnly DueDate,
    string CurrencyCode, decimal ExchangeRate,
    decimal SubTotal, decimal OrderDiscountAmount, decimal ShippingFee,
    decimal TaxAmount, decimal TotalAmount, decimal TotalAmountBase,
    decimal AmountPaid, decimal AmountDue, int DaysOverdue,
    string Notes,
    DateTimeOffset? SentAtUtc, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<SalesInvoiceItemDto> Items,
    IReadOnlyList<SalesInvoicePaymentDto> Payments);

public sealed record SalesInvoiceItemDto(
    Guid Id, Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue, decimal DiscountAmount,
    decimal TaxRate, decimal TaxAmount, decimal LineTotal,
    string Notes, int SortOrder);

public sealed record SalesInvoicePaymentDto(
    Guid Id, string PaymentReference, string PaymentMethod,
    decimal Amount, string CurrencyCode, decimal AmountBase,
    DateOnly PaymentDate, string Notes, DateTimeOffset CreatedAtUtc);

// ── AR Dashboard ──────────────────────────────────────────────────────────────
public sealed record ArDashboardDto(
    Guid BusinessId,
    decimal TotalOutstanding,
    decimal TotalOverdue,
    int OverdueCount,
    IReadOnlyList<ArCustomerSummaryDto> CustomerSummaries);

public sealed record ArCustomerSummaryDto(
    Guid CustomerId, string CustomerName,
    decimal Outstanding, decimal Overdue, int OpenInvoiceCount);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record SalesInvoiceListRequest(
    Guid BusinessId, Guid? CustomerId = null, string? Status = null,
    int Page = 1, int PageSize = 20);

public sealed record CreateSalesInvoiceRequest(
    Guid BusinessId, Guid CustomerId, Guid? SalesOrderId,
    Guid? ChartOfAccountId,
    string CustomerReference,
    DateOnly InvoiceDate, DateOnly DueDate,
    string CurrencyCode, decimal ExchangeRate,
    string OrderDiscountType, decimal OrderDiscountValue,
    decimal ShippingFee,
    string Notes, string TermsAndConditions,
    IReadOnlyList<CreateSalesInvoiceItemRequest> Items);

public sealed record CreateSalesInvoiceItemRequest(
    Guid? InventoryItemId, Guid? SalesOrderItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue,
    decimal TaxRate, string Notes, int SortOrder);

public sealed record RecordSalesPaymentRequest(
    string PaymentReference, string PaymentMethod,
    decimal Amount, string CurrencyCode, decimal ExchangeRate,
    DateOnly PaymentDate, string Notes);
