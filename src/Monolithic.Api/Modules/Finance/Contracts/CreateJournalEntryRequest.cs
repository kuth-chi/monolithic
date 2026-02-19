using Monolithic.Api.Modules.Business.Domain;
using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Request to create a new GL journal entry (initially in Draft status).
/// Minimum 2 lines; system validates balance before posting.
/// </summary>
public sealed class CreateJournalEntryRequest
{
    [Required]
    public Guid BusinessId { get; init; }

    /// <summary>Accounting date (determines fiscal period).</summary>
    [Required]
    public DateOnly TransactionDate { get; init; }

    [Required, MaxLength(500)]
    public string Description { get; init; } = string.Empty;

    public JournalEntrySourceType SourceType { get; init; } = JournalEntrySourceType.Manual;

    [MaxLength(200)]
    public string? SourceDocumentReference { get; init; }

    public Guid? SourceDocumentId { get; init; }

    [Required, MaxLength(3)]
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>Exchange rate to base currency. Defaults to 1.0 (same currency).</summary>
    public decimal ExchangeRate { get; init; } = 1m;

    /// <summary>
    /// Debit/credit lines. Spec requires support for ≥ 999 lines.
    /// Minimum 2 lines required (at least one debit and one credit).
    /// </summary>
    [Required, MinLength(2)]
    public List<CreateJournalEntryLineRequest> Lines { get; init; } = [];
}
