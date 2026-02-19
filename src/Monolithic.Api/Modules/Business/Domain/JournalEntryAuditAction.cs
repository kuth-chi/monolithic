namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Discrete action types captured in the GL audit trail.
/// </summary>
public enum JournalEntryAuditAction
{
    Created = 0,
    Posted = 1,
    Reversed = 2,
    Viewed = 3
}
