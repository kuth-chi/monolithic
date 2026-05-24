namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Shift structure type used for scheduling and attendance evaluation.
/// </summary>
public enum ShiftTemplateType
{
    Fixed = 0,
    Flexible = 1,
    Rotating = 2,
    Split = 3,
    Overnight = 4
}

/// <summary>
/// Reusable shift definition scoped to a business and optionally to a branch.
/// </summary>
public class ShiftTemplate
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Null means business-wide template.</summary>
    public Guid? BranchId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ShiftTemplateType Type { get; set; } = ShiftTemplateType.Fixed;

    public TimeOnly ShiftStart { get; set; } = new(8, 0);

    public TimeOnly ShiftEnd { get; set; } = new(17, 0);

    public int BreakMinutes { get; set; } = 60;

    public int LateGraceMinutes { get; set; } = 15;

    public int OvertimeThresholdMinutes { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // Navigation
    public virtual Business Business { get; set; } = null!;

    public virtual BusinessBranch? Branch { get; set; }

    public virtual ICollection<ShiftAssignment> Assignments { get; set; } = [];
}
