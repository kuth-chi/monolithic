using Monolithic.Api.Modules.Business.Domain;
using BusinessEntity = Monolithic.Api.Modules.Business.Domain.Business;

namespace Monolithic.Api.Modules.Identity.Domain;

/// <summary>
/// Represents an employee that inherits from ApplicationUser.
/// Enables matching employees with businesses and permission assignment.
/// </summary>
public class Employee : ApplicationUser
{
    /// <summary>
    /// The business this employee belongs to.
    /// </summary>
    public Guid BusinessId { get; set; }

    /// <summary>
    /// Employee ID or staff number for internal reference.
    /// </summary>
    public string EmployeeNumber { get; set; } = string.Empty;

    /// <summary>
    /// Job title or position within the business.
    /// </summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>
    /// Department or team assignment.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Employment status (Active, Inactive, OnLeave, etc).
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Date employee was hired.
    /// </summary>
    public DateTimeOffset HiredAtUtc { get; set; }

    /// <summary>
    /// Date employee left the business, if any.
    /// </summary>
    public DateTimeOffset? TerminatedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the business.
    /// </summary>
    public virtual BusinessEntity Business { get; set; } = null!;
}
