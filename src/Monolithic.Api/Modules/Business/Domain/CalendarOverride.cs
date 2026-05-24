namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Source of a calendar override event.
/// </summary>
public enum CalendarOverrideSource
{
    Manual = 0,
    Policy = 1,
    Import = 2
}

/// <summary>
/// Explicit day-level override that supersedes base calendar day classification.
/// </summary>
public class CalendarOverride
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Null means business-wide override.</summary>
    public Guid? BranchId { get; set; }

    public DateOnly Date { get; set; }

    public WorkCalendarDayType OverrideType { get; set; }

    public string Reason { get; set; } = string.Empty;

    public CalendarOverrideSource Source { get; set; } = CalendarOverrideSource.Manual;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // Navigation
    public virtual Business Business { get; set; } = null!;

    public virtual BusinessBranch? Branch { get; set; }
}
