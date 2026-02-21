namespace Monolithic.Api.Modules.Finance.Domain;

/// <summary>Lookup table for expense classification (e.g. Travel, Meals, Software).</summary>
public class ExpenseCategory
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Default GL account to post to (payable account from Chart of Accounts).</summary>
    public Guid? DefaultChartOfAccountId { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }
}
