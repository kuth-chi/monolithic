using Monolithic.Api.Modules.Sales.Domain;

namespace Monolithic.Api.Modules.Sales.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record ArCreditNoteDto(
    Guid Id, Guid BusinessId, Guid CustomerId, Guid? SalesInvoiceId,
    string CreditNoteNumber, string Status, string Reason,
    DateOnly CreditNoteDate,
    string CurrencyCode, decimal ExchangeRate,
    decimal SubTotal, decimal TaxAmount, decimal TotalAmount, decimal TotalAmountBase,
    decimal RemainingAmount,
    string Notes,
    DateTimeOffset? ConfirmedAtUtc, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ArCreditNoteItemDto> Items,
    IReadOnlyList<ArCreditNoteApplicationDto> Applications);

public sealed record ArCreditNoteItemDto(
    Guid Id, Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    decimal TaxRate, decimal TaxAmount, decimal LineTotal,
    string Notes, int SortOrder);

public sealed record ArCreditNoteApplicationDto(
    Guid Id, Guid SalesInvoiceId,
    decimal AmountApplied, DateOnly ApplicationDate, string Notes,
    DateTimeOffset CreatedAtUtc);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record CreateArCreditNoteRequest(
    Guid BusinessId, Guid CustomerId, Guid? SalesInvoiceId,
    string Reason,
    DateOnly CreditNoteDate,
    string CurrencyCode, decimal ExchangeRate,
    string Notes,
    IReadOnlyList<CreateArCreditNoteItemRequest> Items);

public sealed record CreateArCreditNoteItemRequest(
    Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    decimal TaxRate, string Notes, int SortOrder);

public sealed record ApplyArCreditNoteRequest(
    Guid SalesInvoiceId, decimal AmountToApply,
    DateOnly ApplicationDate, string Notes);
