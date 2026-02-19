namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Business-level configurable settings.
/// One record per business. Created automatically when a business is created.
///
/// Design: all nullable fields fall back to system defaults (never hardcoded — resolved in service layer).
/// </summary>
public class BusinessSetting
{
    public Guid Id { get; set; }

    /// <summary>One-to-one with Business.</summary>
    public Guid BusinessId { get; set; }

    // ── Appearance ────────────────────────────────────────────────────────────

    /// <summary>Primary brand color (CSS hex, e.g. "#1E40AF").</summary>
    public string? PrimaryColor { get; set; }

    /// <summary>Secondary brand color.</summary>
    public string? SecondaryColor { get; set; }

    /// <summary>Accent color.</summary>
    public string? AccentColor { get; set; }

    // ── Localisation ──────────────────────────────────────────────────────────

    /// <summary>
    /// IANA timezone identifier (e.g. "Asia/Phnom_Penh", "America/New_York").
    /// Applies to all branches unless overridden at branch level.
    /// </summary>
    public string TimezoneId { get; set; } = "UTC";

    /// <summary>
    /// ISO 4217 currency code for UI display (invoice printing, reports).
    /// Distinct from <see cref="Business.BaseCurrencyCode"/> which is the accounting base.
    /// </summary>
    public string DisplayCurrencyCode { get; set; } = "USD";

    /// <summary>
    /// IETF BCP-47 locale for number/date formatting (e.g. "en-US", "km-KH").
    /// </summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>ISO 8601 day of week for start of business week (1=Monday … 7=Sunday).</summary>
    public int WeekStartDay { get; set; } = 1;

    /// <summary>
    /// Fiscal year start month (1–12). Used for financial year grouping in reports.
    /// </summary>
    public int FiscalYearStartMonth { get; set; } = 1;

    // ── Calendar ──────────────────────────────────────────────────────────────

    /// <summary>ISO 3166-1 alpha-2 country code used to load public holiday calendar (e.g. "KH", "US").</summary>
    public string? HolidayCountryCode { get; set; }

    /// <summary>
    /// Allow employees to request leave on public holidays (if auto-loaded from country calendar).
    /// </summary>
    public bool AutoImportPublicHolidays { get; set; } = true;

    // ── Attendance ────────────────────────────────────────────────────────────

    /// <summary>Default shift start time (local time of the branch). Used when no per-branch policy applies.</summary>
    public TimeOnly DefaultShiftStart { get; set; } = new TimeOnly(8, 0);

    /// <summary>Default shift end time.</summary>
    public TimeOnly DefaultShiftEnd { get; set; } = new TimeOnly(17, 0);

    /// <summary>Grace period in minutes before marking an employee late.</summary>
    public int LateGraceMinutes { get; set; } = 15;

    /// <summary>Whether managers can view attendance of all employees in their branch.</summary>
    public bool ManagerCanViewAttendance { get; set; } = true;

    /// <summary>Whether employees can view their own attendance records.</summary>
    public bool EmployeeCanViewOwnAttendance { get; set; } = true;

    // ── Reporting ─────────────────────────────────────────────────────────────

    /// <summary>Default report date range in days (e.g. 30 = last 30 days).</summary>
    public int DefaultReportRangeDays { get; set; } = 30;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;
}
