namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Calendar day classification used by attendance and payroll calculations.
/// </summary>
public enum WorkCalendarDayType
{
    Working = 0,
    WeeklyOff = 1,
    PublicHoliday = 2,
    CompanyHoliday = 3
}

/// <summary>
/// A concrete calendar day record for business-wide or branch-scoped scheduling.
/// </summary>
public class WorkCalendarDay
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Null means business-wide day rule.</summary>
    public Guid? BranchId { get; set; }

    public DateOnly Date { get; set; }

    public WorkCalendarDayType DayType { get; set; } = WorkCalendarDayType.Working;

    public string? Name { get; set; }

    public string? Description { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country code when imported from public holiday source.</summary>
    public string? CountryCode { get; set; }

    public string? ExternalId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // Navigation
    public virtual Business Business { get; set; } = null!;

    public virtual BusinessBranch? Branch { get; set; }
}
