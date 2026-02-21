using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record SalesOrderDto(
    Guid Id, Guid BusinessId, Guid CustomerId, Guid? QuotationId,
    string OrderNumber, string Status,
    DateOnly OrderDate, DateOnly? ExpectedDeliveryDate,
    string DeliveryAddress, string ShippingMethod,
    string CurrencyCode, decimal ExchangeRate,
    decimal SubTotal, decimal OrderDiscountAmount, decimal ShippingFee,
    decimal TaxAmount, decimal TotalAmount, decimal TotalAmountBase,
    string Notes, string TermsAndConditions,
    DateTimeOffset? ConfirmedAtUtc, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<SalesOrderItemDto> Items);

public sealed record SalesOrderItemDto(
    Guid Id, Guid? InventoryItemId, string Description,
    decimal Quantity, decimal QuantityInvoiced,
    string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue, decimal DiscountAmount,
    decimal TaxRate, decimal TaxAmount, decimal LineTotal,
    string Notes, int SortOrder);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record SalesOrderListRequest(
    Guid BusinessId, Guid? CustomerId = null, string? Status = null,
    int Page = 1, int PageSize = 20);

public sealed record CreateSalesOrderRequest(
    Guid BusinessId, Guid CustomerId,
    DateOnly OrderDate, DateOnly? ExpectedDeliveryDate,
    string CurrencyCode, decimal ExchangeRate,
    string OrderDiscountType, decimal OrderDiscountValue,
    decimal ShippingFee, string DeliveryAddress, string ShippingMethod,
    string Notes, string TermsAndConditions,
    IReadOnlyList<CreateSalesOrderItemRequest> Items);

public sealed record CreateSalesOrderItemRequest(
    Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    string DiscountType, decimal DiscountValue,
    decimal TaxRate, string Notes, int SortOrder);

public sealed record UpdateSalesOrderRequest(
    DateOnly? ExpectedDeliveryDate, string DeliveryAddress, string ShippingMethod,
    string CurrencyCode, decimal ExchangeRate,
    string OrderDiscountType, decimal OrderDiscountValue, decimal ShippingFee,
    string Notes, string TermsAndConditions,
    IReadOnlyList<CreateSalesOrderItemRequest> Items);
