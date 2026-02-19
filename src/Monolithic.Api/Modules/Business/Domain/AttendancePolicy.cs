namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Attendance tracking scope.
/// </summary>
public enum AttendanceScope
{
    /// <summary>Policy applies to the entire branch.</summary>
    Branch = 0,
    /// <summary>Policy applies to a specific department within the branch.</summary>
    Department = 1,
    /// <summary>Policy applies to a specific employee.</summary>
    Individual = 2
}

/// <summary>
/// Defines working hour rules and attendance tracking configuration
/// at business, branch, department, or individual level.
///
/// Resolution priority: Individual > Department > Branch > Business default.
/// The service layer resolves the effective policy by walking this chain.
/// </summary>
public class AttendancePolicy
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Null = business-wide policy.</summary>
    public Guid? BranchId { get; set; }

    public AttendanceScope Scope { get; set; } = AttendanceScope.Branch;

    /// <summary>Department name (when Scope = Department).</summary>
    public string? Department { get; set; }

    /// <summary>Employee id (when Scope = Individual).</summary>
    public Guid? EmployeeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // ── Working Hours ─────────────────────────────────────────────────────────

    public TimeOnly ShiftStart { get; set; } = new TimeOnly(8, 0);
    public TimeOnly ShiftEnd { get; set; } = new TimeOnly(17, 0);

    /// <summary>Minutes of break time included in shift (not counted toward work hours).</summary>
    public int BreakMinutes { get; set; } = 60;

    /// <summary>Grace period in minutes before clock-in is marked late.</summary>
    public int LateGraceMinutes { get; set; } = 15;

    /// <summary>Required working hours per day (decimal, e.g. 8.0).</summary>
    public decimal RequiredHoursPerDay { get; set; } = 8m;

    // ── Work Days ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bitmask of working days (bit 0 = Monday … bit 6 = Sunday).
    /// Default 0b0111110 = Mon–Fri.
    /// </summary>
    public byte WorkingDaysMask { get; set; } = 0b0111110;

    // ── Visibility ────────────────────────────────────────────────────────────

    /// <summary>Employees covered by this policy can view their own records.</summary>
    public bool EmployeeCanViewOwn { get; set; } = true;

    /// <summary>Branch manager can view attendance of employees under this policy.</summary>
    public bool ManagerCanView { get; set; } = true;

    /// <summary>HR / business admin can view all attendance under this policy.</summary>
    public bool HrCanView { get; set; } = true;

    // ── Holiday Exclusions ────────────────────────────────────────────────────

    /// <summary>
    /// Comma-separated BusinessHoliday.Id GUIDs that this policy EXCLUDES
    /// (branch works on that business holiday).
    /// Stored as string for SQLite compatibility; parsed in service layer.
    /// </summary>
    public string? ExcludedHolidayIds { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;
    public virtual BusinessBranch? Branch { get; set; }
}
