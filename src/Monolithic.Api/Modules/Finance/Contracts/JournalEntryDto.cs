using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Read-only projection of a GL journal entry header, returned to API clients.
/// </summary>
public sealed class JournalEntryDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string EntryNumber { get; init; } = string.Empty;
    public string FiscalPeriod { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public JournalEntryStatus Status { get; init; }
    public JournalEntrySourceType SourceType { get; init; }
    public string? SourceDocumentReference { get; init; }
    public Guid? SourceDocumentId { get; init; }
    public Guid? ReversalOfEntryId { get; init; }
    public Guid? ReversedByEntryId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public decimal TotalDebits { get; init; }
    public decimal TotalCredits { get; init; }
    public bool IsBalanced => TotalDebits == TotalCredits;

    // Audit
    public Guid CreatedByUserId { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? PostedByUserId { get; init; }
    public DateTimeOffset? PostedAtUtc { get; init; }
    public Guid? ReversedByUserId { get; init; }
    public DateTimeOffset? ReversedAtUtc { get; init; }

    public IReadOnlyList<JournalEntryLineDto> Lines { get; init; } = [];
    public IReadOnlyList<JournalEntryAuditLogDto> AuditLogs { get; init; } = [];
}
