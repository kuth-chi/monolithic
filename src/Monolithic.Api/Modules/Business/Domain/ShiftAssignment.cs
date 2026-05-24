namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Assignment scope for resolving effective shift templates.
/// </summary>
public enum ShiftAssignmentScope
{
    Business = 0,
    Branch = 1,
    Department = 2,
    Employee = 3
}

/// <summary>
/// Effective-dated shift assignment for business, branch, department, or employee.
/// </summary>
public class ShiftAssignment
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid ShiftTemplateId { get; set; }

    /// <summary>Null unless scoped to branch/department/employee within branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Used when scope is employee.</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Used when scope is department.</summary>
    public string? Department { get; set; }

    public ShiftAssignmentScope Scope { get; set; } = ShiftAssignmentScope.Business;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsPrimary { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // Navigation
    public virtual Business Business { get; set; } = null!;

    public virtual BusinessBranch? Branch { get; set; }

    public virtual ShiftTemplate ShiftTemplate { get; set; } = null!;
}
