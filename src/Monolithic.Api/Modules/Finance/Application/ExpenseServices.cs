using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Finance.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Finance.Application;

// ═══════════════════════════════════════════════════════════════════════════════
// ExpenseCategoryService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ExpenseCategoryService(ApplicationDbContext db) : IExpenseCategoryService
{
    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default)
        => await db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => MapToDto(c))
            .ToListAsync(ct);

    public async Task<ExpenseCategoryDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ExpenseCategories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => MapToDto(c))
            .FirstOrDefaultAsync(ct);

    public async Task<ExpenseCategoryDto> CreateAsync(CreateExpenseCategoryRequest req, CancellationToken ct = default)
    {
        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            Name = req.Name,
            Description = req.Description,
            DefaultChartOfAccountId = req.DefaultChartOfAccountId,
            SortOrder = req.SortOrder,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync(ct);
        return MapToDto(category);
    }

    public async Task<ExpenseCategoryDto> UpdateAsync(Guid id, UpdateExpenseCategoryRequest req, CancellationToken ct = default)
    {
        var category = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Category not found.");

        category.Name = req.Name;
        category.Description = req.Description;
        category.DefaultChartOfAccountId = req.DefaultChartOfAccountId;
        category.IsActive = req.IsActive;
        category.SortOrder = req.SortOrder;
        category.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return MapToDto(category);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Category not found.");
        db.ExpenseCategories.Remove(category);
        await db.SaveChangesAsync(ct);
    }

    private static ExpenseCategoryDto MapToDto(ExpenseCategory c) => new(
        c.Id, c.BusinessId, c.Name, c.Description,
        c.DefaultChartOfAccountId, c.IsActive, c.SortOrder);
}

// ═══════════════════════════════════════════════════════════════════════════════
// ExpenseService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ExpenseService(ApplicationDbContext db) : IExpenseService
{
    public async Task<IReadOnlyList<ExpenseDto>> GetByBusinessAsync(
        Guid businessId, Guid? userId = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.Expenses.AsNoTracking()
            .Include(e => e.Items)
            .Where(e => e.BusinessId == businessId);

        if (userId.HasValue) query = query.Where(e => e.SubmittedByUserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ExpenseStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        return (await query.OrderByDescending(e => e.ExpenseDate).ToListAsync(ct))
            .Select(MapToDto).ToList();
    }

    public async Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => (await db.Expenses.AsNoTracking()
            .Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id, ct)) is { } e ? MapToDto(e) : null;

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest req, CancellationToken ct = default)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            SubmittedByUserId = req.SubmittedByUserId,
            ExpenseNumber = await GenerateNumberAsync(req.BusinessId, ct),
            Title = req.Title,
            Status = ExpenseStatus.Draft,
            ExpenseDate = req.ExpenseDate,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            Notes = req.Notes,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var items = req.Items.Select(i => new ExpenseItem
        {
            Id = Guid.NewGuid(),
            ExpenseId = expense.Id,
            ExpenseCategoryId = i.ExpenseCategoryId,
            Description = i.Description,
            Amount = i.Amount,
            CurrencyCode = i.CurrencyCode,
            ExchangeRate = i.ExchangeRate,
            AmountBase = decimal.Round(i.Amount * i.ExchangeRate, 2),
            ExpenseDate = i.ExpenseDate,
            ReceiptUrl = i.ReceiptUrl,
            IsBillable = i.IsBillable,
            CustomerId = i.CustomerId,
            Notes = i.Notes,
            SortOrder = i.SortOrder
        }).ToList();

        expense.TotalAmount = decimal.Round(items.Sum(i => i.Amount), 2);
        expense.TotalAmountBase = decimal.Round(items.Sum(i => i.AmountBase), 2);

        db.Expenses.Add(expense);
        db.ExpenseItems.AddRange(items);
        await db.SaveChangesAsync(ct);

        expense.Items = items;
        return MapToDto(expense);
    }

    public async Task<ExpenseDto> UpdateAsync(Guid id, UpdateExpenseRequest req, CancellationToken ct = default)
    {
        var expense = await db.Expenses.Include(e => e.Items)
            .FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Expense not found.");

        if (expense.Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be updated.");

        expense.Title = req.Title;
        expense.ExpenseDate = req.ExpenseDate;
        expense.CurrencyCode = req.CurrencyCode;
        expense.ExchangeRate = req.ExchangeRate;
        expense.Notes = req.Notes;
        expense.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.ExpenseItems.RemoveRange(expense.Items);
        var items = req.Items.Select(i => new ExpenseItem
        {
            Id = Guid.NewGuid(),
            ExpenseId = expense.Id,
            ExpenseCategoryId = i.ExpenseCategoryId,
            Description = i.Description,
            Amount = i.Amount,
            CurrencyCode = i.CurrencyCode,
            ExchangeRate = i.ExchangeRate,
            AmountBase = decimal.Round(i.Amount * i.ExchangeRate, 2),
            ExpenseDate = i.ExpenseDate,
            ReceiptUrl = i.ReceiptUrl,
            IsBillable = i.IsBillable,
            CustomerId = i.CustomerId,
            Notes = i.Notes,
            SortOrder = i.SortOrder
        }).ToList();

        expense.TotalAmount = decimal.Round(items.Sum(i => i.Amount), 2);
        expense.TotalAmountBase = decimal.Round(items.Sum(i => i.AmountBase), 2);
        expense.Items = items;
        db.ExpenseItems.AddRange(items);
        await db.SaveChangesAsync(ct);
        return MapToDto(expense);
    }

    public async Task SubmitAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Expense not found.");

        if (expense.Status != ExpenseStatus.Draft)
            throw new InvalidOperationException("Only draft expenses can be submitted.");

        expense.Status = ExpenseStatus.Submitted;
        expense.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReviewAsync(Guid id, ReviewExpenseRequest req, Guid reviewedByUserId, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Expense not found.");

        if (expense.Status != ExpenseStatus.Submitted)
            throw new InvalidOperationException("Only submitted expenses can be reviewed.");

        expense.Status = req.Approved ? ExpenseStatus.Approved : ExpenseStatus.Rejected;
        expense.ReviewedByUserId = reviewedByUserId;
        expense.ReviewedAtUtc = DateTimeOffset.UtcNow;
        expense.RejectionReason = req.Approved ? string.Empty : req.Reason;
        expense.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkPaidAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Expense not found.");

        if (expense.Status != ExpenseStatus.Approved)
            throw new InvalidOperationException("Only approved expenses can be marked as paid.");

        expense.Status = ExpenseStatus.Paid;
        expense.PaidAtUtc = DateTimeOffset.UtcNow;
        expense.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new InvalidOperationException("Expense not found.");

        if (expense.Status is ExpenseStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid expense.");

        expense.Status = ExpenseStatus.Cancelled;
        expense.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<string> GenerateNumberAsync(Guid businessId, CancellationToken ct)
    {
        var count = await db.Expenses.CountAsync(e => e.BusinessId == businessId, ct);
        return $"EXP-{DateTime.UtcNow.Year}-{(count + 1):D5}";
    }

    private static ExpenseDto MapToDto(Expense e) => new(
        e.Id, e.BusinessId, e.SubmittedByUserId,
        e.ExpenseNumber, e.Title, e.Status.ToString(),
        e.ExpenseDate,
        e.CurrencyCode, e.ExchangeRate,
        e.TotalAmount, e.TotalAmountBase,
        e.Notes, e.RejectionReason,
        e.ReviewedAtUtc, e.PaidAtUtc, e.CreatedAtUtc,
        e.Items.OrderBy(i => i.SortOrder).Select(i => new ExpenseItemDto(
            i.Id, i.ExpenseCategoryId, i.Description,
            i.Amount, i.CurrencyCode, i.AmountBase,
            i.ExpenseDate, i.ReceiptUrl,
            i.IsBillable, i.CustomerId,
            i.Notes, i.SortOrder)).ToList());
}
