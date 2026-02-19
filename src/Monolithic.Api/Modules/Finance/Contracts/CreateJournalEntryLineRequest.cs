using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// One debit or credit line within a <see cref="CreateJournalEntryRequest"/>.
/// Exactly one of <see cref="DebitAmount"/> or <see cref="CreditAmount"/> must be > 0.
/// </summary>
public sealed class CreateJournalEntryLineRequest
{
    [Required]
    public Guid AccountId { get; init; }

    /// <summary>Debit amount in the entry's functional currency. Set to 0 for credit lines.</summary>
    public decimal DebitAmount { get; init; }

    /// <summary>Credit amount in the entry's functional currency. Set to 0 for debit lines.</summary>
    public decimal CreditAmount { get; init; }

    [MaxLength(50)]
    public string? CostCenter { get; init; }

    [MaxLength(50)]
    public string? ProjectCode { get; init; }

    [MaxLength(300)]
    public string? LineDescription { get; init; }
}
