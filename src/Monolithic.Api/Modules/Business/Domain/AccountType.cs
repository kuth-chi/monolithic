namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Top-level type for a Chart of Account entry.
/// Maps to the balance sheet / income statement classification.
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}
