namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Assigns an employee to a specific branch.
/// An employee may have a primary branch and secondary assignments.
/// </summary>
public class BranchEmployee
{
    public Guid Id { get; set; }

    public Guid BranchId { get; set; }

    public Guid EmployeeId { get; set; }

    /// <summary>The employee's primary branch assignment.</summary>
    public bool IsPrimary { get; set; } = true;

    public DateOnly AssignedOn { get; set; }

    public DateOnly? ReleasedOn { get; set; }

    public bool IsActive => ReleasedOn is null || ReleasedOn >= DateOnly.FromDateTime(DateTime.UtcNow);

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual BusinessBranch Branch { get; set; } = null!;
}
