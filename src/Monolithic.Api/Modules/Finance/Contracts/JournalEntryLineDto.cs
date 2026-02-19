namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Read-only projection of a single debit/credit line within a journal entry.
/// </summary>
public sealed class JournalEntryLineDto
{
    public Guid Id { get; init; }
    public Guid JournalEntryId { get; init; }
    public Guid AccountId { get; init; }

    /// <summary>Account number + name snapshot (e.g. "1010 – Petty Cash").</summary>
    public string AccountLabel { get; init; } = string.Empty;

    public int LineNumber { get; init; }
    public decimal DebitAmount { get; init; }
    public decimal CreditAmount { get; init; }
    public decimal DebitAmountBase { get; init; }
    public decimal CreditAmountBase { get; init; }
    public string? CostCenter { get; init; }
    public string? ProjectCode { get; init; }
    public string? LineDescription { get; init; }
}
