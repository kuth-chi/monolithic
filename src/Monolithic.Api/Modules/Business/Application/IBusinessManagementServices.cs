using Microsoft.AspNetCore.Http;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Application;

/// <summary>
/// Manages business licenses — the quota authority for how many businesses
/// and branches an owner may create.
/// </summary>
public interface IBusinessLicenseService
{
    Task<BusinessLicenseDto?> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<BusinessLicenseDto> UpsertAsync(UpsertBusinessLicenseRequest request, CancellationToken ct = default);
    /// <summary>Checks whether the owner may create one more business under their current license.</summary>
    Task<bool> CanCreateBusinessAsync(Guid ownerId, CancellationToken ct = default);
    /// <summary>Checks whether the owner may add one more branch to the given business.</summary>
    Task<bool> CanCreateBranchAsync(Guid ownerId, Guid businessId, CancellationToken ct = default);
}

/// <summary>
/// Manages the relationship between owners and their businesses.
/// Enforces license quotas and ownership rules.
/// </summary>
public interface IBusinessOwnershipService
{
    /// <summary>Returns the owner dashboard with all businesses and aggregate stats.</summary>
    Task<OwnerDashboardDto> GetOwnerDashboardAsync(Guid ownerId, CancellationToken ct = default);
    /// <summary>Returns all business IDs owned by this user (active ownerships only).</summary>
    Task<IReadOnlyList<BusinessOwnershipDto>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    /// <summary>
    /// Creates a new business and assigns ownership to the user.
    /// Validates license quota before creation.
    /// Automatically creates: HQ branch + default settings + standard COA.
    /// </summary>
    Task<BusinessOwnershipDto> CreateBusinessAsync(Guid ownerId, CreateBusinessWithOwnerRequest request, CancellationToken ct = default);
    /// <summary>Revokes ownership (soft-delete).</summary>
    Task RevokeOwnershipAsync(Guid ownerId, Guid businessId, CancellationToken ct = default);
}

/// <summary>
/// Manages branches within a single business.
/// Enforces the "at least one HQ" invariant.
/// All list methods are paginated, filterable, and sortable via query parameters.
/// Results are cached using L1 (in-memory) + L2 (Redis) two-level caching.
/// </summary>
public interface IBusinessBranchService
{
    /// <summary>Returns a paginated, filtered, and sorted branch list for the given business.</summary>
    Task<PagedResult<BusinessBranchDto>> GetByBusinessAsync(
        Guid businessId,
        BranchQueryParameters query,
        CancellationToken ct = default);

    Task<BusinessBranchDto?> GetByIdAsync(Guid branchId, CancellationToken ct = default);
    Task<BusinessBranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<BusinessBranchDto> UpdateAsync(Guid branchId, UpdateBranchRequest request, CancellationToken ct = default);

    /// <summary>Promotes a branch to HQ and demotes the current HQ atomically.</summary>
    Task PromoteHeadquartersAsync(Guid businessId, Guid newHqBranchId, CancellationToken ct = default);

    /// <summary>Soft-deletes a branch. Fails if it is the only active branch.</summary>
    Task DeleteAsync(Guid branchId, CancellationToken ct = default);

    Task<BranchEmployeeDto> AssignEmployeeAsync(
        Guid branchId,
        AssignEmployeeToBranchRequest request,
        CancellationToken ct = default);

    Task ReleaseEmployeeAsync(Guid branchId, Guid employeeId, CancellationToken ct = default);

    /// <summary>Returns a paginated, filtered, and sorted employee list for the given branch.</summary>
    Task<PagedResult<BranchEmployeeDto>> GetEmployeesAsync(
        Guid branchId,
        BranchEmployeeQueryParameters query,
        CancellationToken ct = default);
}

/// <summary>
/// Manages per-business configurable settings.
/// </summary>
public interface IBusinessSettingService
{
    Task<BusinessSettingDto?> GetAsync(Guid businessId, CancellationToken ct = default);
    Task<BusinessSettingDto> UpsertAsync(UpsertBusinessSettingRequest request, CancellationToken ct = default);
}

/// <summary>
/// Manages branding media (logo, cover header) for a business.
/// </summary>
public interface IBusinessMediaService
{
    Task<IReadOnlyList<BusinessMediaDto>> GetAllAsync(Guid businessId, CancellationToken ct = default);
    Task<BusinessMediaDto?> GetCurrentAsync(Guid businessId, BusinessMediaType mediaType, CancellationToken ct = default);
    Task<BusinessMediaDto> UploadAsync(Guid businessId, BusinessMediaType mediaType, IFormFile file, CancellationToken ct = default);
    Task DeleteAsync(Guid mediaId, CancellationToken ct = default);
}

/// <summary>
/// Manages business-level holidays (manual + auto-imported from country calendars).
/// </summary>
public interface IBusinessHolidayService
{
    Task<IReadOnlyList<BusinessHolidayDto>> GetByYearAsync(Guid businessId, int year, CancellationToken ct = default);
    Task<BusinessHolidayDto> CreateAsync(CreateHolidayRequest request, CancellationToken ct = default);
    Task<BusinessHolidayDto> UpdateAsync(Guid holidayId, UpdateHolidayRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid holidayId, CancellationToken ct = default);
    /// <summary>
    /// Imports public holidays from a country calendar for a given year.
    /// Skips duplicates (matching ExternalId or Date+Name).
    /// </summary>
    Task<int> ImportPublicHolidaysAsync(Guid businessId, string countryCode, int year, CancellationToken ct = default);
    /// <summary>Checks whether a given date is a holiday for the business.</summary>
    Task<bool> IsHolidayAsync(Guid businessId, DateOnly date, CancellationToken ct = default);
}

/// <summary>
/// Manages attendance policies at business, branch, department, or individual scope.
/// Policy resolution: Individual > Department > Branch > Business.
/// </summary>
public interface IAttendancePolicyService
{
    Task<IReadOnlyList<AttendancePolicyDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default);
    Task<IReadOnlyList<AttendancePolicyDto>> GetByBranchAsync(Guid branchId, CancellationToken ct = default);
    Task<AttendancePolicyDto?> GetByIdAsync(Guid policyId, CancellationToken ct = default);
    Task<AttendancePolicyDto> UpsertAsync(UpsertAttendancePolicyRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid policyId, CancellationToken ct = default);
    /// <summary>
    /// Resolves the effective attendance policy for an employee.
    /// Walks: employee override → department → branch → business default.
    /// </summary>
    Task<AttendancePolicyDto?> ResolveForEmployeeAsync(Guid businessId, Guid employeeId, CancellationToken ct = default);
}
