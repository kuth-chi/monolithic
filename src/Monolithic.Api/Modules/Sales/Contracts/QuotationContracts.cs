using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record QuotationDto(
    Guid Id, Guid BusinessId, Guid CustomerId,
    string QuotationNumber, string Status,
    DateOnly QuotationDate, DateOnly ExpiryDate,
    string CurrencyCode, decimal ExchangeRate,
    decimal SubTotal, decimal OrderDiscountAmount, decimal ShippingFee,
    decimal TaxAmount, decimal TotalAmount,
    string Notes, string TermsAndConditions,
    Guid? ConvertedToSalesOrderId,
    DateTimeOffset? SentAtUtc, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<QuotationItemDto> Items);

public sealed record QuotationItemDto(
    Guid Id, Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue, decimal DiscountAmount,
    decimal TaxRate, decimal TaxAmount,
    decimal LineTotalBeforeDiscount, decimal LineTotalAfterDiscount, decimal LineTotal,
    string Notes, int SortOrder);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record QuotationListRequest(
    Guid BusinessId, Guid? CustomerId = null, string? Status = null,
    int Page = 1, int PageSize = 20);

public sealed record CreateQuotationRequest(
    Guid BusinessId, Guid CustomerId,
    DateOnly QuotationDate, DateOnly ExpiryDate,
    string CurrencyCode, decimal ExchangeRate,
    string OrderDiscountType, decimal OrderDiscountValue,
    decimal ShippingFee,
    string Notes, string TermsAndConditions,
    IReadOnlyList<CreateQuotationItemRequest> Items);

public sealed record CreateQuotationItemRequest(
    Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue,
    decimal TaxRate, string Notes, int SortOrder);

public sealed record UpdateQuotationRequest(
    DateOnly ExpiryDate, string CurrencyCode, decimal ExchangeRate,
    string OrderDiscountType, decimal OrderDiscountValue, decimal ShippingFee,
    string Notes, string TermsAndConditions,
    IReadOnlyList<CreateQuotationItemRequest> Items);

public sealed record ConvertQuotationRequest(
    DateOnly OrderDate, DateOnly? ExpectedDeliveryDate,
    string DeliveryAddress, string ShippingMethod);
