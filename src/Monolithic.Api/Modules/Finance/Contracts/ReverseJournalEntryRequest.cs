using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Request to reverse a posted journal entry.
/// Creates a mirror-image entry (debits ↔ credits) with status <c>Reversal</c>
/// and links both entries together.
/// </summary>
public sealed class ReverseJournalEntryRequest
{
    /// <summary>Effective date for the new reversal entry. Defaults to today if omitted.</summary>
    public DateOnly? ReversalDate { get; init; }

    /// <summary>Reason for the reversal — required for audit trail.</summary>
    [Required, MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
