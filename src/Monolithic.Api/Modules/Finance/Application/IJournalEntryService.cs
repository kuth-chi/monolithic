using Monolithic.Api.Modules.Finance.Contracts;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// GL Journal Entry service contract.
/// Encapsulates all operations for creating, posting, reversing,
/// and querying general ledger journal entries.
/// </summary>
public interface IJournalEntryService
{
    /// <summary>
    /// Returns a paged, filtered list of journal entries for a business.
    /// Supports drill-down queries by account, period, status, and source type.
    /// </summary>
    Task<PagedResult<JournalEntryDto>> GetPagedAsync(
        JournalEntryFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single journal entry with all lines and audit logs, or null if not found.</summary>
    Task<JournalEntryDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Draft journal entry with the supplied lines.
    /// Does NOT enforce balance at this stage — allows saving incomplete drafts.
    /// </summary>
    Task<JournalEntryDto> CreateAsync(
        CreateJournalEntryRequest request,
        Guid createdByUserId,
        string createdByDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a Draft entry to the GL.
    /// Enforces: entry is in Draft status, debits = credits, ≥ 2 lines,
    /// all accounts are active posting accounts belonging to the same business.
    /// Once posted the entry is immutable.
    /// </summary>
    Task<JournalEntryDto> PostAsync(
        Guid id,
        PostJournalEntryRequest request,
        Guid postedByUserId,
        string postedByDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses a Posted entry by creating a mirror-image entry.
    /// Original entry's status becomes <c>Reversed</c>; new entry's status is <c>Reversal</c>.
    /// Both entries are linked via <c>ReversalOfEntryId</c> / <c>ReversedByEntryId</c>.
    /// The reversal entry is automatically posted.
    /// </summary>
    Task<JournalEntryDto> ReverseAsync(
        Guid id,
        ReverseJournalEntryRequest request,
        Guid reversedByUserId,
        string reversedByDisplayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the audit trail for a given journal entry.
    /// </summary>
    Task<IReadOnlyList<JournalEntryAuditLogDto>> GetAuditLogsAsync(
        Guid journalEntryId,
        CancellationToken cancellationToken = default);
}
