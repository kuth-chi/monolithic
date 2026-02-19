using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Query filter for listing journal entries in the GL.
/// All properties are optional — omitting a property means "no filter on that field".
/// </summary>
public sealed class JournalEntryFilter
{
    public Guid BusinessId { get; init; }

    /// <summary>Filter to a specific fiscal period, e.g. "2026-02".</summary>
    public string? FiscalPeriod { get; init; }

    public DateOnly? TransactionDateFrom { get; init; }
    public DateOnly? TransactionDateTo { get; init; }

    public JournalEntryStatus? Status { get; init; }

    public JournalEntrySourceType? SourceType { get; init; }

    /// <summary>Filter entries that touch a specific GL account.</summary>
    public Guid? AccountId { get; init; }

    /// <summary>Free-text search against EntryNumber, Description, or SourceDocumentReference.</summary>
    public string? SearchTerm { get; init; }

    // Paging
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
