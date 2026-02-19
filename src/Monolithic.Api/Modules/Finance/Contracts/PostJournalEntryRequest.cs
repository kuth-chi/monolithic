using System.ComponentModel.DataAnnotations;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Request to post a draft journal entry (makes it immutable).
/// System validates balanced totals before accepting.
/// </summary>
public sealed class PostJournalEntryRequest
{
    /// <summary>Optional notes attached to the posting audit event.</summary>
    [MaxLength(500)]
    public string? Notes { get; init; }
}
