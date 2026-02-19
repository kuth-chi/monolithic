using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Finance.Contracts;

/// <summary>
/// Read-only projection of one audit-trail row for a journal entry.
/// </summary>
public sealed class JournalEntryAuditLogDto
{
    public Guid Id { get; init; }
    public Guid JournalEntryId { get; init; }
    public Guid UserId { get; init; }
    public string UserDisplayName { get; init; } = string.Empty;
    public JournalEntryAuditAction Action { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public string? Notes { get; init; }
}
