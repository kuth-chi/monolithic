namespace Monolithic.Api.Modules.Finance.Contracts;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public sealed record ExpenseCategoryDto(
    Guid Id, Guid BusinessId, string Name, string Description,
    Guid? DefaultChartOfAccountId, bool IsActive, int SortOrder);

public sealed record ExpenseDto(
    Guid Id, Guid BusinessId, Guid SubmittedByUserId,
    string ExpenseNumber, string Title, string Status,
    DateOnly ExpenseDate,
    string CurrencyCode, decimal ExchangeRate,
    decimal TotalAmount, decimal TotalAmountBase,
    string Notes, string RejectionReason,
    DateTimeOffset? ReviewedAtUtc, DateTimeOffset? PaidAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ExpenseItemDto> Items);

public sealed record ExpenseItemDto(
    Guid Id, Guid? ExpenseCategoryId, string Description,
    decimal Amount, string CurrencyCode, decimal AmountBase,
    DateOnly ExpenseDate, string? ReceiptUrl,
    bool IsBillable, Guid? CustomerId,
    string Notes, int SortOrder);

// ── Requests ──────────────────────────────────────────────────────────────────
public sealed record ExpenseListRequest(
    Guid BusinessId, Guid? UserId = null, string? Status = null,
    int Page = 1, int PageSize = 20);

public sealed record CreateExpenseCategoryRequest(
    Guid BusinessId, string Name, string Description,
    Guid? DefaultChartOfAccountId, int SortOrder);

public sealed record UpdateExpenseCategoryRequest(
    string Name, string Description,
    Guid? DefaultChartOfAccountId, bool IsActive, int SortOrder);

public sealed record CreateExpenseRequest(
    Guid BusinessId, Guid SubmittedByUserId,
    string Title, DateOnly ExpenseDate,
    string CurrencyCode, decimal ExchangeRate,
    string Notes,
    IReadOnlyList<CreateExpenseItemRequest> Items);

public sealed record CreateExpenseItemRequest(
    Guid? ExpenseCategoryId, string Description,
    decimal Amount, string CurrencyCode, decimal ExchangeRate,
    DateOnly ExpenseDate, string? ReceiptUrl,
    bool IsBillable, Guid? CustomerId,
    string Notes, int SortOrder);

public sealed record UpdateExpenseRequest(
    string Title, DateOnly ExpenseDate,
    string CurrencyCode, decimal ExchangeRate,
    string Notes,
    IReadOnlyList<CreateExpenseItemRequest> Items);

public sealed record ReviewExpenseRequest(bool Approved, string Reason);
