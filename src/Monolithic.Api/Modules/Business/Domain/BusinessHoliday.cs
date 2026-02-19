namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// How a holiday was sourced.
/// </summary>
public enum HolidaySource
{
    /// <summary>Manually created by business admin.</summary>
    Manual = 0,
    /// <summary>Imported from the system's country public-holiday calendar.</summary>
    SystemImport = 1
}

/// <summary>
/// A holiday at business level.
/// Applies to all branches unless overridden by an <see cref="AttendancePolicy"/>.
///
/// Best practices applied:
/// - Business-level holidays apply to all branches by default.
/// - A branch can exclude a business holiday via its AttendancePolicy.ExcludedHolidayIds.
/// - Public holidays are imported automatically from country calendars when AutoImportPublicHolidays = true.
/// - Recurring annual holidays (e.g. national day) set IsRecurring = true and only the month/day is used each year.
/// </summary>
public class BusinessHoliday
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>The holiday date.</summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// If true, this holiday repeats every year on the same month/day.
    /// The year in <see cref="Date"/> is ignored when evaluating recurrence.
    /// </summary>
    public bool IsRecurring { get; set; }

    public HolidaySource Source { get; set; } = HolidaySource.Manual;

    /// <summary>ISO 3166-1 alpha-2 country code if imported from public calendar.</summary>
    public string? CountryCode { get; set; }

    /// <summary>External identifier from public holiday API (for deduplication on re-import).</summary>
    public string? ExternalId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;
}
