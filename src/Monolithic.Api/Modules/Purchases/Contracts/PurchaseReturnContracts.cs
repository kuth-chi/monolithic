namespace Monolithic.Api.Modules.Purchases.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record PurchaseReturnDto(
    Guid Id, Guid BusinessId, Guid VendorId,
    Guid? PurchaseOrderId, Guid? VendorBillId,
    string ReturnNumber, string Status,
    DateOnly ReturnDate, string Reason,
    string CurrencyCode, decimal ExchangeRate,
    decimal TotalAmount, decimal TotalAmountBase,
    string VendorCreditNoteReference,
    string Notes,
    DateTimeOffset? ConfirmedAtUtc, DateTimeOffset? ShippedAtUtc,
    DateTimeOffset? CreditedAtUtc, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<PurchaseReturnItemDto> Items);

public sealed record PurchaseReturnItemDto(
    Guid Id, Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    decimal TaxRate, decimal TaxAmount, decimal LineTotal,
    string Notes, int SortOrder);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record CreatePurchaseReturnRequest(
    Guid BusinessId, Guid VendorId,
    Guid? PurchaseOrderId, Guid? VendorBillId,
    string Reason, DateOnly ReturnDate,
    string CurrencyCode, decimal ExchangeRate,
    string Notes,
    IReadOnlyList<CreatePurchaseReturnItemRequest> Items);

public sealed record CreatePurchaseReturnItemRequest(
    Guid? InventoryItemId, string Description,
    decimal Quantity, string Unit, decimal UnitPrice,
    decimal TaxRate, string Notes, int SortOrder);

public sealed record RecordVendorCreditRequest(string VendorCreditNoteReference, string Notes);
