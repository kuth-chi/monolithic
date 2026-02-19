using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

/// <summary>
/// Combined request to create a new business + assign ownership in one call.
/// Automatically creates default HQ branch, settings, and standard COA.
/// </summary>
public sealed record CreateBusinessWithOwnerRequest(
    string Name,
    string? ShortName,
    string? Code,
    string? ShortDescription,
    BusinessType Type,
    string LocalName,
    string VatTin,
    string BaseCurrencyCode,
    string Address,
    string City,
    string StateProvince,
    string Country,
    string PostalCode,
    /// <summary>Initial HQ branch name (defaults to "Headquarters" if null).</summary>
    string? HeadquartersBranchName,
    /// <summary>Initial HQ branch address (defaults to business address if null).</summary>
    string? HeadquartersBranchAddress);

// ── License ───────────────────────────────────────────────────────────────────

public sealed record BusinessLicenseDto(
    Guid Id,
    Guid OwnerId,
    LicensePlan Plan,
    LicenseStatus Status,
    int MaxBusinesses,
    int MaxBranchesPerBusiness,
    int MaxEmployees,
    bool AllowAdvancedReporting,
    bool AllowMultiCurrency,
    bool AllowIntegrations,
    DateOnly StartsOn,
    DateOnly? ExpiresOn,
    string? ExternalSubscriptionId,
    DateTimeOffset CreatedAtUtc);

public sealed record UpsertBusinessLicenseRequest(
    Guid OwnerId,
    LicensePlan Plan,
    int MaxBusinesses,
    int MaxBranchesPerBusiness,
    int MaxEmployees,
    bool AllowAdvancedReporting,
    bool AllowMultiCurrency,
    bool AllowIntegrations,
    DateOnly StartsOn,
    DateOnly? ExpiresOn,
    string? ExternalSubscriptionId);

// ── Business Ownership ────────────────────────────────────────────────────────

public sealed record BusinessOwnershipDto(
    Guid Id,
    Guid OwnerId,
    Guid BusinessId,
    string BusinessName,
    Guid LicenseId,
    bool IsPrimaryOwner,
    DateTimeOffset GrantedAtUtc,
    bool IsActive);

// ── Branch ────────────────────────────────────────────────────────────────────

public sealed record BusinessBranchDto(
    Guid Id,
    Guid BusinessId,
    string Code,
    string Name,
    BranchType Type,
    bool IsHeadquarters,
    string Address,
    string City,
    string StateProvince,
    string Country,
    string PostalCode,
    string? PhoneNumber,
    string? Email,
    string? TimezoneId,
    Guid? ManagerId,
    bool IsActive,
    int SortOrder,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateBranchRequest(
    Guid BusinessId,
    string Code,
    string Name,
    BranchType Type,
    bool IsHeadquarters,
    string Address,
    string City,
    string StateProvince,
    string Country,
    string PostalCode,
    string? PhoneNumber,
    string? Email,
    string? TimezoneId,
    Guid? ManagerId,
    int SortOrder = 0);

public sealed record UpdateBranchRequest(
    string Name,
    BranchType Type,
    string Address,
    string City,
    string StateProvince,
    string Country,
    string PostalCode,
    string? PhoneNumber,
    string? Email,
    string? TimezoneId,
    Guid? ManagerId,
    int SortOrder,
    bool IsActive);

public sealed record PromoteHeadquartersRequest(
    /// <summary>Branch to promote to HQ — current HQ will be demoted to type Other.</summary>
    Guid NewHqBranchId);

// ── Branch Employee ───────────────────────────────────────────────────────────

public sealed record BranchEmployeeDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid EmployeeId,
    bool IsPrimary,
    DateOnly AssignedOn,
    DateOnly? ReleasedOn,
    bool IsActive);

public sealed record AssignEmployeeToBranchRequest(
    Guid EmployeeId,
    bool IsPrimary,
    DateOnly AssignedOn);

// ── Business Settings ─────────────────────────────────────────────────────────

public sealed record BusinessSettingDto(
    Guid Id,
    Guid BusinessId,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string TimezoneId,
    string DisplayCurrencyCode,
    string Locale,
    int WeekStartDay,
    int FiscalYearStartMonth,
    string? HolidayCountryCode,
    bool AutoImportPublicHolidays,
    TimeOnly DefaultShiftStart,
    TimeOnly DefaultShiftEnd,
    int LateGraceMinutes,
    bool ManagerCanViewAttendance,
    bool EmployeeCanViewOwnAttendance,
    int DefaultReportRangeDays,
    DateTimeOffset? ModifiedAtUtc);

public sealed record UpsertBusinessSettingRequest(
    Guid BusinessId,
    string? PrimaryColor,
    string? SecondaryColor,
    string? AccentColor,
    string TimezoneId,
    string DisplayCurrencyCode,
    string Locale,
    int WeekStartDay,
    int FiscalYearStartMonth,
    string? HolidayCountryCode,
    bool AutoImportPublicHolidays,
    TimeOnly DefaultShiftStart,
    TimeOnly DefaultShiftEnd,
    int LateGraceMinutes,
    bool ManagerCanViewAttendance,
    bool EmployeeCanViewOwnAttendance,
    int DefaultReportRangeDays);

// ── Business Media ────────────────────────────────────────────────────────────

public sealed record BusinessMediaDto(
    Guid Id,
    Guid BusinessId,
    BusinessMediaType MediaType,
    string StoragePath,
    string? PublicUrl,
    string? ContentType,
    long FileSizeBytes,
    string? OriginalFileName,
    string? AltText,
    bool IsCurrent,
    DateTimeOffset UploadedAtUtc);

// ── Holiday ───────────────────────────────────────────────────────────────────

public sealed record BusinessHolidayDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Description,
    DateOnly Date,
    bool IsRecurring,
    HolidaySource Source,
    string? CountryCode,
    bool IsActive);

public sealed record CreateHolidayRequest(
    Guid BusinessId,
    string Name,
    string? Description,
    DateOnly Date,
    bool IsRecurring);

public sealed record UpdateHolidayRequest(
    string Name,
    string? Description,
    DateOnly Date,
    bool IsRecurring,
    bool IsActive);

// ── Attendance Policy ─────────────────────────────────────────────────────────

public sealed record AttendancePolicyDto(
    Guid Id,
    Guid BusinessId,
    Guid? BranchId,
    string? BranchName,
    AttendanceScope Scope,
    string? Department,
    Guid? EmployeeId,
    string Name,
    string? Description,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    int BreakMinutes,
    int LateGraceMinutes,
    decimal RequiredHoursPerDay,
    byte WorkingDaysMask,
    bool EmployeeCanViewOwn,
    bool ManagerCanView,
    bool HrCanView,
    bool IsActive);

public sealed record UpsertAttendancePolicyRequest(
    Guid BusinessId,
    Guid? BranchId,
    AttendanceScope Scope,
    string? Department,
    Guid? EmployeeId,
    string Name,
    string? Description,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    int BreakMinutes,
    int LateGraceMinutes,
    decimal RequiredHoursPerDay,
    byte WorkingDaysMask,
    bool EmployeeCanViewOwn,
    bool ManagerCanView,
    bool HrCanView);

// ── Cross-business Reporting ──────────────────────────────────────────────────

/// <summary>
/// Summary view of a single business for the owner's dashboard.
/// </summary>
public sealed record BusinessSummaryDto(
    Guid BusinessId,
    string BusinessName,
    string? ShortName,
    int BranchCount,
    int EmployeeCount,
    bool IsActive,
    string BaseCurrencyCode,
    string? LogoUrl,
    string? CoverUrl);

/// <summary>
/// Owner dashboard: all businesses plus aggregate performance.
/// </summary>
public sealed record OwnerDashboardDto(
    Guid OwnerId,
    BusinessLicenseDto License,
    int TotalBusinesses,
    int TotalBranches,
    int TotalEmployees,
    IReadOnlyList<BusinessSummaryDto> Businesses);
