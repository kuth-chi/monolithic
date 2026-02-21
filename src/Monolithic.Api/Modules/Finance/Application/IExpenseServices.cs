using Monolithic.Api.Modules.Finance.Contracts;

namespace Monolithic.Api.Modules.Finance.Application;

public interface IExpenseCategoryService
{
    Task<IReadOnlyList<ExpenseCategoryDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default);
    Task<ExpenseCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);
    Task<ExpenseCategoryDto> UpdateAsync(Guid id, UpdateExpenseCategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> GetByBusinessAsync(Guid businessId, Guid? userId = null, string? status = null, CancellationToken ct = default);
    Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default);
    Task<ExpenseDto> UpdateAsync(Guid id, UpdateExpenseRequest request, CancellationToken ct = default);

    /// <summary>Submits a draft expense for approval.</summary>
    Task SubmitAsync(Guid id, CancellationToken ct = default);

    /// <summary>Approves or rejects a submitted expense.</summary>
    Task ReviewAsync(Guid id, ReviewExpenseRequest request, Guid reviewedByUserId, CancellationToken ct = default);

    /// <summary>Marks an approved expense as paid.</summary>
    Task MarkPaidAsync(Guid id, CancellationToken ct = default);

    Task CancelAsync(Guid id, CancellationToken ct = default);
}
