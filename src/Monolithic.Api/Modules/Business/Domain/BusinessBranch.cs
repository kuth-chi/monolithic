namespace Monolithic.Api.Modules.Business.Domain;

/// <summary>
/// Branch type classification.
/// </summary>
public enum BranchType
{
    Headquarters = 0,
    RegionalOffice = 1,
    SalesOffice = 2,
    Warehouse = 3,
    ServiceCenter = 4,
    Franchise = 5,
    Other = 99
}

/// <summary>
/// A physical or logical branch of a <see cref="Business"/>.
/// Every business MUST have at least one branch designated as <see cref="BranchType.Headquarters"/>.
///
/// Business rules (enforced in service layer):
/// - Exactly one branch per business must have IsHeadquarters = true.
/// - Branch count is bounded by the owner's <see cref="BusinessLicense.MaxBranchesPerBusiness"/>.
/// - Deleting the last branch is forbidden.
/// - Downgrading HQ is only allowed when another branch is promoted simultaneously.
/// </summary>
public class BusinessBranch
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Short unique code within the business (e.g. "HQ", "BKK-01").</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public BranchType Type { get; set; } = BranchType.Headquarters;

    /// <summary>
    /// Convenience flag mirroring Type == Headquarters.
    /// Stored separately to allow efficient indexing.
    /// </summary>
    public bool IsHeadquarters { get; set; }

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Contact phone for this branch.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Contact email for this branch.</summary>
    public string? Email { get; set; }

    /// <summary>IANA timezone identifier (e.g. "Asia/Phnom_Penh"). Overrides business-level setting.</summary>
    public string? TimezoneId { get; set; }

    /// <summary>Branch manager (Employee.Id).</summary>
    public Guid? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public virtual Business Business { get; set; } = null!;

    /// <summary>Branch-level attendance policies.</summary>
    public virtual ICollection<AttendancePolicy> AttendancePolicies { get; set; } = [];

    /// <summary>Employees assigned to this branch.</summary>
    public virtual ICollection<BranchEmployee> BranchEmployees { get; set; } = [];
}
