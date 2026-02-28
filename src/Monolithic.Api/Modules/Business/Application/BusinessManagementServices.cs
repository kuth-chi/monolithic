using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Common.Caching;
using Monolithic.Api.Common.Pagination;
using Monolithic.Api.Common.Storage;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

// ── Mappers (private static — DRY, reused across all services) ────────────────

file static class BusinessManagementMapper
{
    public static BusinessLicenseDto ToDto(this BusinessLicense l) => new(
        l.Id, l.OwnerId, l.Plan, l.Status,
        l.MaxBusinesses, l.MaxBranchesPerBusiness, l.MaxEmployees,
        l.AllowAdvancedReporting, l.AllowMultiCurrency, l.AllowIntegrations,
        l.StartsOn, l.ExpiresOn, l.ExternalSubscriptionId, l.CreatedAtUtc);

    public static BusinessBranchDto ToDto(this BusinessBranch b) => new(
        b.Id, b.BusinessId, b.Code, b.Name, b.Type, b.IsHeadquarters,
        b.Address, b.City, b.StateProvince, b.Country, b.PostalCode,
        b.PhoneNumber, b.Email, b.TimezoneId, b.ManagerId, b.IsActive,
        b.SortOrder, b.CreatedAtUtc);

    public static BranchEmployeeDto ToDto(this BranchEmployee be, string branchName) => new(
        be.Id, be.BranchId, branchName, be.EmployeeId,
        be.IsPrimary, be.AssignedOn, be.ReleasedOn,
        be.ReleasedOn is null || be.ReleasedOn >= DateOnly.FromDateTime(DateTime.UtcNow));

    public static BusinessSettingDto ToDto(this BusinessSetting s) => new(
        s.Id, s.BusinessId, s.PrimaryColor, s.SecondaryColor, s.AccentColor,
        s.TimezoneId, s.DisplayCurrencyCode, s.Locale,
        s.WeekStartDay, s.FiscalYearStartMonth, s.HolidayCountryCode,
        s.AutoImportPublicHolidays, s.DefaultShiftStart, s.DefaultShiftEnd,
        s.LateGraceMinutes, s.ManagerCanViewAttendance, s.EmployeeCanViewOwnAttendance,
        s.DefaultReportRangeDays, s.ModifiedAtUtc);

    public static BusinessMediaDto ToDto(this BusinessMedia m) => new(
        m.Id, m.BusinessId, m.MediaType, m.StoragePath, m.PublicUrl,
        m.ContentType, m.FileSizeBytes, m.OriginalFileName, m.AltText,
        m.IsCurrent, m.UploadedAtUtc);

    public static BusinessHolidayDto ToDto(this BusinessHoliday h) => new(
        h.Id, h.BusinessId, h.Name, h.Description, h.Date,
        h.IsRecurring, h.Source, h.CountryCode, h.IsActive);

    public static AttendancePolicyDto ToDto(this AttendancePolicy p, string? branchName = null) => new(
        p.Id, p.BusinessId, p.BranchId, branchName, p.Scope,
        p.Department, p.EmployeeId, p.Name, p.Description,
        p.ShiftStart, p.ShiftEnd, p.BreakMinutes, p.LateGraceMinutes,
        p.RequiredHoursPerDay, p.WorkingDaysMask,
        p.EmployeeCanViewOwn, p.ManagerCanView, p.HrCanView, p.IsActive);
}

// ── BusinessLicenseService ────────────────────────────────────────────────────

public sealed class BusinessLicenseService(ApplicationDbContext db) : IBusinessLicenseService
{
    public async Task<BusinessLicenseDto?> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        // NOTE: SQLite provider cannot translate ORDER BY on DateTimeOffset.
        // Keep filtering in SQL, then order in-memory for cross-provider behavior.
        var licenses = await db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .ToListAsync(ct);

        var license = licenses
            .OrderByDescending(l => l.CreatedAtUtc)
            .FirstOrDefault();
        return license?.ToDto();
    }

    public async Task<BusinessLicenseDto> UpsertAsync(UpsertBusinessLicenseRequest req, CancellationToken ct = default)
    {
        var existing = await db.BusinessLicenses
            .Where(l => l.OwnerId == req.OwnerId && l.Status == LicenseStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (existing is null)
        {
            existing = new BusinessLicense { OwnerId = req.OwnerId };
            db.BusinessLicenses.Add(existing);
        }
        else
        {
            existing.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }

        existing.Plan = req.Plan;
        existing.MaxBusinesses = req.MaxBusinesses;
        existing.MaxBranchesPerBusiness = req.MaxBranchesPerBusiness;
        existing.MaxEmployees = req.MaxEmployees;
        existing.AllowAdvancedReporting = req.AllowAdvancedReporting;
        existing.AllowMultiCurrency = req.AllowMultiCurrency;
        existing.AllowIntegrations = req.AllowIntegrations;
        existing.StartsOn = req.StartsOn;
        existing.ExpiresOn = req.ExpiresOn;
        existing.ExternalSubscriptionId = req.ExternalSubscriptionId;

        await db.SaveChangesAsync(ct);
        return existing.ToDto();
    }

    public async Task<bool> CanCreateBusinessAsync(Guid ownerId, CancellationToken ct = default)
    {
        var license = await db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (license is null) return false;
        if (license.ExpiresOn.HasValue && license.ExpiresOn.Value < DateOnly.FromDateTime(DateTime.UtcNow)) return false;

        var currentCount = await db.BusinessOwnerships
            .CountAsync(o => o.OwnerId == ownerId && o.RevokedAtUtc == null, ct);

        return currentCount < license.MaxBusinesses;
    }

    public async Task<bool> CanCreateBranchAsync(Guid ownerId, Guid businessId, CancellationToken ct = default)
    {
        var license = await db.BusinessLicenses
            .Where(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (license is null) return false;

        var branchCount = await db.BusinessBranches
            .CountAsync(b => b.BusinessId == businessId && b.IsActive, ct);

        return branchCount < license.MaxBranchesPerBusiness;
    }
}

// ── BusinessOwnershipService ──────────────────────────────────────────────────

public sealed class BusinessOwnershipService(
    ApplicationDbContext db,
    IBusinessLicenseService licenseService,
    IChartOfAccountService coaService) : IBusinessOwnershipService
{
    public async Task<OwnerDashboardDto> GetOwnerDashboardAsync(Guid ownerId, CancellationToken ct = default)
    {
        var license = await licenseService.GetByOwnerAsync(ownerId, ct);

        var ownerships = await db.BusinessOwnerships
            .Include(o => o.Business)
                .ThenInclude(b => b.Branches)
            .Include(o => o.Business)
                .ThenInclude(b => b.Employees)
            .Include(o => o.Business)
                .ThenInclude(b => b.Media.Where(m => m.IsCurrent))
            .Where(o => o.OwnerId == ownerId && o.RevokedAtUtc == null)
            .ToListAsync(ct);

        var summaries = ownerships.Select(o =>
        {
            var biz = o.Business;
            var logo = biz.Media.FirstOrDefault(m => m.MediaType == BusinessMediaType.Logo)?.PublicUrl;
            var cover = biz.Media.FirstOrDefault(m => m.MediaType == BusinessMediaType.CoverHeader)?.PublicUrl;
            return new BusinessSummaryDto(
                biz.Id, biz.Name, biz.ShortName,
                biz.Branches.Count(br => br.IsActive),
                biz.Employees.Count,
                biz.IsActive,
                biz.BaseCurrencyCode,
                logo, cover);
        }).ToList();

        return new OwnerDashboardDto(
            ownerId,
            license!,
            summaries.Count,
            summaries.Sum(s => s.BranchCount),
            summaries.Sum(s => s.EmployeeCount),
            summaries);
    }

    public async Task<IReadOnlyList<BusinessOwnershipDto>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await db.BusinessOwnerships
            .Include(o => o.Business)
            .Where(o => o.OwnerId == ownerId && o.RevokedAtUtc == null)
            .Select(o => new BusinessOwnershipDto(
                o.Id, o.OwnerId, o.BusinessId, o.Business.Name,
                o.LicenseId, o.IsPrimaryOwner, o.GrantedAtUtc, true))
            .ToListAsync(ct);
    }

    public async Task<BusinessOwnershipDto> CreateBusinessAsync(
        Guid ownerId,
        CreateBusinessWithOwnerRequest req,
        CancellationToken ct = default)
    {
        if (!await licenseService.CanCreateBusinessAsync(ownerId, ct))
            throw new InvalidOperationException("License quota exceeded: cannot create more businesses.");

        var license = await db.BusinessLicenses
            .FirstAsync(l => l.OwnerId == ownerId && l.Status == LicenseStatus.Active, ct);

        // 1. Create business
        var business = new Domain.Business
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            ShortName = req.ShortName,
            Code = req.Code,
            ShortDescription = req.ShortDescription,
            Type = req.Type,
            LocalName = req.LocalName,
            VatTin = req.VatTin,
            BaseCurrencyCode = req.BaseCurrencyCode,
            Address = req.Address,
            City = req.City,
            StateProvince = req.StateProvince,
            Country = req.Country,
            PostalCode = req.PostalCode
        };
        db.Businesses.Add(business);

        // 2. Create HQ branch (mandatory)
        var hq = new BusinessBranch
        {
            Id = Guid.NewGuid(),
            BusinessId = business.Id,
            Code = "HQ",
            Name = req.HeadquartersBranchName ?? "Headquarters",
            Type = BranchType.Headquarters,
            IsHeadquarters = true,
            Address = req.HeadquartersBranchAddress ?? req.Address,
            City = req.City,
            StateProvince = req.StateProvince,
            Country = req.Country,
            PostalCode = req.PostalCode,
            SortOrder = 0
        };
        db.BusinessBranches.Add(hq);

        // 3. Create default settings
        var settings = new BusinessSetting
        {
            BusinessId = business.Id,
            DisplayCurrencyCode = req.BaseCurrencyCode
        };
        db.BusinessSettings.Add(settings);

        // 4. Create ownership record
        var ownership = new BusinessOwnership
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            BusinessId = business.Id,
            LicenseId = license.Id,
            IsPrimaryOwner = true
        };
        db.BusinessOwnerships.Add(ownership);

        // 5. Create UserBusiness membership for the owner so the JWT
        //    gains a business_id claim immediately after creation.
        //    IsDefault=true when the owner has no other default business yet.
        var hasDefaultBusiness = await db.UserBusinesses
            .AnyAsync(ub => ub.UserId == ownerId && ub.IsDefault, ct);

        var userBusiness = new UserBusiness
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            BusinessId = business.Id,
            IsDefault = !hasDefaultBusiness,
            IsActive = true,
            JoinedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserBusinesses.Add(userBusiness);

        await db.SaveChangesAsync(ct);

        // 6. Seed standard chart of accounts (after save so BusinessId FK is valid)
        await coaService.SeedStandardCOAAsync(business.Id, req.BaseCurrencyCode, ct);

        return new BusinessOwnershipDto(
            ownership.Id, ownerId, business.Id, business.Name,
            license.Id, true, ownership.GrantedAtUtc, true);
    }

    public async Task RevokeOwnershipAsync(Guid ownerId, Guid businessId, CancellationToken ct = default)
    {
        var ownership = await db.BusinessOwnerships
            .FirstOrDefaultAsync(o => o.OwnerId == ownerId && o.BusinessId == businessId && o.RevokedAtUtc == null, ct)
            ?? throw new KeyNotFoundException("Ownership not found.");

        ownership.RevokedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ── BusinessBranchService ─────────────────────────────────────────────────────

/// <summary>
/// Cache TTLs: lists → L2 5 min / L1 60 s; single items → L2 10 min / L1 2 min.
/// Write operations evict the affected single-item cache key immediately.
/// Paginated list caches expire naturally via TTL (eventual consistency).
/// </summary>
public sealed class BusinessBranchService(
    ApplicationDbContext db,
    IBusinessLicenseService licenseService,
    ITwoLevelCache cache) : IBusinessBranchService
{
    private static readonly TimeSpan ListL2Ttl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ListL1Ttl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ItemL2Ttl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ItemL1Ttl = TimeSpan.FromMinutes(2);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<PagedResult<BusinessBranchDto>> GetByBusinessAsync(
        Guid businessId,
        BranchQueryParameters query,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Branches(businessId, query.ToCacheSegment());

        return await cache.GetOrCreateAsync(
            cacheKey,
            async innerCt =>
            {
                // ─ Base filter ──────────────────────────────────────────────
                IQueryable<BusinessBranch> q = db.BusinessBranches
                    .Where(b => b.BusinessId == businessId);

                // ─ Optional filters ─────────────────────────────────────────
                if (query.IsActive.HasValue)
                    q = q.Where(b => b.IsActive == query.IsActive.Value);

                if (query.IsHeadquarters.HasValue)
                    q = q.Where(b => b.IsHeadquarters == query.IsHeadquarters.Value);

                if (!string.IsNullOrWhiteSpace(query.Country))
                    q = q.Where(b => b.Country.ToLower().Contains(query.Country.ToLower()));

                if (!string.IsNullOrWhiteSpace(query.City))
                    q = q.Where(b => b.City.ToLower().Contains(query.City.ToLower()));

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var s = query.Search.ToLower();
                    q = q.Where(b =>
                        b.Name.ToLower().Contains(s) ||
                        b.Code.ToLower().Contains(s) ||
                        b.City.ToLower().Contains(s));
                }

                // ─ Sorting ──────────────────────────────────────────────────
                q = query.SortBy?.ToLowerInvariant() switch
                {
                    "name"      => query.SortDesc ? q.OrderByDescending(b => b.Name)         : q.OrderBy(b => b.Name),
                    "code"      => query.SortDesc ? q.OrderByDescending(b => b.Code)         : q.OrderBy(b => b.Code),
                    "city"      => query.SortDesc ? q.OrderByDescending(b => b.City)         : q.OrderBy(b => b.City),
                    "country"   => query.SortDesc ? q.OrderByDescending(b => b.Country)      : q.OrderBy(b => b.Country),
                    "createdat" => query.SortDesc ? q.OrderByDescending(b => b.CreatedAtUtc) : q.OrderBy(b => b.CreatedAtUtc),
                    "sortorder" => query.SortDesc ? q.OrderByDescending(b => b.SortOrder)    : q.OrderBy(b => b.SortOrder),
                    _           => q.OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
                };

                return await q.Select(b => b.ToDto()).ToPagedResultAsync(query, innerCt);
            },
            l2Ttl: ListL2Ttl,
            l1Ttl: ListL1Ttl,
            cancellationToken: ct);
    }

    public async Task<BusinessBranchDto?> GetByIdAsync(Guid branchId, CancellationToken ct = default)
    {
        // Wrap nullable result so the cache can store a definitive "not found" entry.
        var wrapper = await cache.GetOrCreateAsync<BranchDtoWrapper>(
            CacheKeys.BranchById(branchId),
            async innerCt =>
            {
                var b = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, innerCt);
                return new BranchDtoWrapper(b?.ToDto());
            },
            l2Ttl: ItemL2Ttl,
            l1Ttl: ItemL1Ttl,
            cancellationToken: ct);

        return wrapper.Value;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<BusinessBranchDto> CreateAsync(CreateBranchRequest req, CancellationToken ct = default)
    {
        var ownership = await db.BusinessOwnerships
            .FirstOrDefaultAsync(o => o.BusinessId == req.BusinessId && o.RevokedAtUtc == null && o.IsPrimaryOwner, ct)
            ?? throw new KeyNotFoundException("Business ownership not found.");

        if (!await licenseService.CanCreateBranchAsync(ownership.OwnerId, req.BusinessId, ct))
            throw new InvalidOperationException("License quota exceeded: cannot create more branches.");

        if (req.IsHeadquarters)
            await DemoteCurrentHqAsync(req.BusinessId, ct);

        var branch = new BusinessBranch
        {
            Id             = Guid.NewGuid(),
            BusinessId     = req.BusinessId,
            Code           = req.Code,
            Name           = req.Name,
            Type           = req.Type,
            IsHeadquarters = req.IsHeadquarters,
            Address        = req.Address,
            City           = req.City,
            StateProvince  = req.StateProvince,
            Country        = req.Country,
            PostalCode     = req.PostalCode,
            PhoneNumber    = req.PhoneNumber,
            Email          = req.Email,
            TimezoneId     = req.TimezoneId,
            ManagerId      = req.ManagerId,
            SortOrder      = req.SortOrder
        };
        db.BusinessBranches.Add(branch);
        await db.SaveChangesAsync(ct);

        await cache.RemoveAsync(CacheKeys.BranchById(branch.Id), ct);
        return branch.ToDto();
    }

    public async Task<BusinessBranchDto> UpdateAsync(Guid branchId, UpdateBranchRequest req, CancellationToken ct = default)
    {
        var branch = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, ct)
            ?? throw new KeyNotFoundException("Branch not found.");

        branch.Name          = req.Name;
        branch.Type          = req.Type;
        branch.Address       = req.Address;
        branch.City          = req.City;
        branch.StateProvince = req.StateProvince;
        branch.Country       = req.Country;
        branch.PostalCode    = req.PostalCode;
        branch.PhoneNumber   = req.PhoneNumber;
        branch.Email         = req.Email;
        branch.TimezoneId    = req.TimezoneId;
        branch.ManagerId     = req.ManagerId;
        branch.SortOrder     = req.SortOrder;
        branch.IsActive      = req.IsActive;
        branch.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.BranchById(branchId), ct);

        return branch.ToDto();
    }

    public async Task PromoteHeadquartersAsync(Guid businessId, Guid newHqBranchId, CancellationToken ct = default)
    {
        await DemoteCurrentHqAsync(businessId, ct);

        var newHq = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == newHqBranchId, ct)
            ?? throw new KeyNotFoundException("Target branch not found.");

        if (newHq.BusinessId != businessId)
            throw new InvalidOperationException("Branch does not belong to this business.");

        newHq.IsHeadquarters = true;
        newHq.Type           = BranchType.Headquarters;
        newHq.ModifiedAtUtc  = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.BranchById(newHqBranchId), ct);
    }

    public async Task DeleteAsync(Guid branchId, CancellationToken ct = default)
    {
        var branch = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, ct)
            ?? throw new KeyNotFoundException("Branch not found.");

        if (branch.IsHeadquarters)
            throw new InvalidOperationException("Cannot delete the headquarters branch. Promote another branch first.");

        var activeCount = await db.BusinessBranches
            .CountAsync(b => b.BusinessId == branch.BusinessId && b.IsActive, ct);

        if (activeCount <= 1)
            throw new InvalidOperationException("A business must have at least one active branch.");

        branch.IsActive      = false;
        branch.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.BranchById(branchId), ct);
    }

    // ── Branch Employees ──────────────────────────────────────────────────────

    public async Task<PagedResult<BranchEmployeeDto>> GetEmployeesAsync(
        Guid branchId,
        BranchEmployeeQueryParameters query,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.BranchEmployees(branchId, query.ToCacheSegment());

        return await cache.GetOrCreateAsync(
            cacheKey,
            async innerCt =>
            {
                var branch = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, innerCt)
                    ?? throw new KeyNotFoundException("Branch not found.");

                IQueryable<BranchEmployee> q = db.BranchEmployees.Where(be => be.BranchId == branchId);

                // ─ Optional filters ─────────────────────────────────────────
                if (query.IsActive.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    q = query.IsActive.Value
                        ? q.Where(be => be.ReleasedOn == null || be.ReleasedOn >= today)
                        : q.Where(be => be.ReleasedOn != null && be.ReleasedOn < today);
                }
                else
                {
                    q = q.Where(be => be.ReleasedOn == null); // default: active only
                }

                if (query.IsPrimary.HasValue)
                    q = q.Where(be => be.IsPrimary == query.IsPrimary.Value);

                // ─ Sorting ──────────────────────────────────────────────────
                q = query.SortBy?.ToLowerInvariant() switch
                {
                    "assignedon" => query.SortDesc ? q.OrderByDescending(be => be.AssignedOn) : q.OrderBy(be => be.AssignedOn),
                    "releasedon" => query.SortDesc ? q.OrderByDescending(be => be.ReleasedOn) : q.OrderBy(be => be.ReleasedOn),
                    "isprimary"  => query.SortDesc ? q.OrderByDescending(be => be.IsPrimary)  : q.OrderBy(be => be.IsPrimary),
                    _            => q.OrderByDescending(be => be.IsPrimary).ThenBy(be => be.AssignedOn)
                };

                var branchName = branch.Name;
                return await q.Select(be => be.ToDto(branchName)).ToPagedResultAsync(query, innerCt);
            },
            l2Ttl: ListL2Ttl,
            l1Ttl: ListL1Ttl,
            cancellationToken: ct);
    }

    public async Task<BranchEmployeeDto> AssignEmployeeAsync(Guid branchId, AssignEmployeeToBranchRequest req, CancellationToken ct = default)
    {
        var branch = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, ct)
            ?? throw new KeyNotFoundException("Branch not found.");

        if (req.IsPrimary)
        {
            var existing = await db.BranchEmployees
                .Where(be => be.EmployeeId == req.EmployeeId && be.IsPrimary && be.ReleasedOn == null)
                .ToListAsync(ct);
            foreach (var e in existing) e.IsPrimary = false;
        }

        var assignment = new BranchEmployee
        {
            Id         = Guid.NewGuid(),
            BranchId   = branchId,
            EmployeeId = req.EmployeeId,
            IsPrimary  = req.IsPrimary,
            AssignedOn = req.AssignedOn
        };
        db.BranchEmployees.Add(assignment);
        await db.SaveChangesAsync(ct);
        return assignment.ToDto(branch.Name);
    }

    public async Task ReleaseEmployeeAsync(Guid branchId, Guid employeeId, CancellationToken ct = default)
    {
        var assignment = await db.BranchEmployees
            .FirstOrDefaultAsync(be => be.BranchId == branchId && be.EmployeeId == employeeId && be.ReleasedOn == null, ct)
            ?? throw new KeyNotFoundException("Active assignment not found.");

        assignment.ReleasedOn = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task DemoteCurrentHqAsync(Guid businessId, CancellationToken ct)
    {
        var currentHq = await db.BusinessBranches
            .FirstOrDefaultAsync(b => b.BusinessId == businessId && b.IsHeadquarters, ct);
        if (currentHq is null) return;
        currentHq.IsHeadquarters = false;
        if (currentHq.Type == BranchType.Headquarters)
            currentHq.Type = BranchType.RegionalOffice;
        currentHq.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await cache.RemoveAsync(CacheKeys.BranchById(currentHq.Id), ct);
    }

    // Nullable-value wrapper: lets the cache store a definitive "not found" entry
    // rather than treating a null result as a cache miss on every call.
    private sealed record BranchDtoWrapper(BusinessBranchDto? Value);
}

// ── BusinessSettingService ────────────────────────────────────────────────────

public sealed class BusinessSettingService(ApplicationDbContext db) : IBusinessSettingService
{
    public async Task<BusinessSettingDto?> GetAsync(Guid businessId, CancellationToken ct = default)
    {
        var s = await db.BusinessSettings.FirstOrDefaultAsync(x => x.BusinessId == businessId, ct);
        return s?.ToDto();
    }

    public async Task<BusinessSettingDto> UpsertAsync(UpsertBusinessSettingRequest req, CancellationToken ct = default)
    {
        var s = await db.BusinessSettings.FirstOrDefaultAsync(x => x.BusinessId == req.BusinessId, ct);
        if (s is null)
        {
            s = new BusinessSetting { BusinessId = req.BusinessId };
            db.BusinessSettings.Add(s);
        }
        else
        {
            s.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }

        s.PrimaryColor = req.PrimaryColor;
        s.SecondaryColor = req.SecondaryColor;
        s.AccentColor = req.AccentColor;
        s.TimezoneId = req.TimezoneId;
        s.DisplayCurrencyCode = req.DisplayCurrencyCode;
        s.Locale = req.Locale;
        s.WeekStartDay = req.WeekStartDay;
        s.FiscalYearStartMonth = req.FiscalYearStartMonth;
        s.HolidayCountryCode = req.HolidayCountryCode;
        s.AutoImportPublicHolidays = req.AutoImportPublicHolidays;
        s.DefaultShiftStart = req.DefaultShiftStart;
        s.DefaultShiftEnd = req.DefaultShiftEnd;
        s.LateGraceMinutes = req.LateGraceMinutes;
        s.ManagerCanViewAttendance = req.ManagerCanViewAttendance;
        s.EmployeeCanViewOwnAttendance = req.EmployeeCanViewOwnAttendance;
        s.DefaultReportRangeDays = req.DefaultReportRangeDays;

        await db.SaveChangesAsync(ct);
        return s.ToDto();
    }
}

// ── BusinessMediaService ──────────────────────────────────────────────────────

public sealed class BusinessMediaService(
    ApplicationDbContext db,
    IImageStorageService storage) : IBusinessMediaService
{
    public async Task<IReadOnlyList<BusinessMediaDto>> GetAllAsync(Guid businessId, CancellationToken ct = default)
        => await db.BusinessMedia
            .Where(m => m.BusinessId == businessId)
            .OrderByDescending(m => m.UploadedAtUtc)
            .Select(m => m.ToDto())
            .ToListAsync(ct);

    public async Task<BusinessMediaDto?> GetCurrentAsync(Guid businessId, BusinessMediaType mediaType, CancellationToken ct = default)
    {
        var m = await db.BusinessMedia
            .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.MediaType == mediaType && x.IsCurrent, ct);
        return m?.ToDto();
    }

    public async Task<BusinessMediaDto> UploadAsync(
        Guid businessId, BusinessMediaType mediaType,
        IFormFile file,
        CancellationToken ct = default)
    {
        // Demote existing current media of same type
        var previous = await db.BusinessMedia
            .Where(m => m.BusinessId == businessId && m.MediaType == mediaType && m.IsCurrent)
            .ToListAsync(ct);
        foreach (var p in previous) p.IsCurrent = false;

        var folder = $"businesses/{businessId}/{mediaType.ToString().ToLower()}";
        var storagePath = await storage.SaveAsync(file, folder, ct);
        var publicUrl = storage.GetPublicUrl(storagePath);

        var media = new BusinessMedia
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            MediaType = mediaType,
            StoragePath = storagePath,
            PublicUrl = publicUrl,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            OriginalFileName = file.FileName,
            IsCurrent = true
        };
        db.BusinessMedia.Add(media);
        await db.SaveChangesAsync(ct);
        return media.ToDto();
    }

    public async Task DeleteAsync(Guid mediaId, CancellationToken ct = default)
    {
        var media = await db.BusinessMedia.FirstOrDefaultAsync(x => x.Id == mediaId, ct)
            ?? throw new KeyNotFoundException("Media not found.");
        await storage.DeleteAsync(media.StoragePath, ct);
        db.BusinessMedia.Remove(media);
        await db.SaveChangesAsync(ct);
    }
}

// ── BusinessHolidayService ────────────────────────────────────────────────────

public sealed class BusinessHolidayService(ApplicationDbContext db) : IBusinessHolidayService
{
    public async Task<IReadOnlyList<BusinessHolidayDto>> GetByYearAsync(Guid businessId, int year, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.BusinessHolidays
            .Where(h => h.BusinessId == businessId && h.IsActive &&
                        (h.IsRecurring || h.Date.Year == year))
            .OrderBy(h => h.Date)
            .Select(h => h.ToDto())
            .ToListAsync(ct);
    }

    public async Task<BusinessHolidayDto> CreateAsync(CreateHolidayRequest req, CancellationToken ct = default)
    {
        var holiday = new BusinessHoliday
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            Name = req.Name,
            Description = req.Description,
            Date = req.Date,
            IsRecurring = req.IsRecurring,
            Source = HolidaySource.Manual
        };
        db.BusinessHolidays.Add(holiday);
        await db.SaveChangesAsync(ct);
        return holiday.ToDto();
    }

    public async Task<BusinessHolidayDto> UpdateAsync(Guid holidayId, UpdateHolidayRequest req, CancellationToken ct = default)
    {
        var h = await db.BusinessHolidays.FirstOrDefaultAsync(x => x.Id == holidayId, ct)
            ?? throw new KeyNotFoundException("Holiday not found.");

        h.Name = req.Name;
        h.Description = req.Description;
        h.Date = req.Date;
        h.IsRecurring = req.IsRecurring;
        h.IsActive = req.IsActive;
        h.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return h.ToDto();
    }

    public async Task DeleteAsync(Guid holidayId, CancellationToken ct = default)
    {
        var h = await db.BusinessHolidays.FirstOrDefaultAsync(x => x.Id == holidayId, ct)
            ?? throw new KeyNotFoundException("Holiday not found.");
        db.BusinessHolidays.Remove(h);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Stub: integrates with a public holiday API such as Nager.Date (https://date.nager.at/api/v3).
    /// In production, inject an IPublicHolidayProvider abstraction and call the external API.
    /// Returns the count of holidays inserted.
    /// </summary>
    public Task<int> ImportPublicHolidaysAsync(Guid businessId, string countryCode, int year, CancellationToken ct = default)
    {
        // TODO: inject IPublicHolidayProvider — resolved from DI by country codes.
        // Pattern: GET https://date.nager.at/api/v3/PublicHolidays/{year}/{countryCode}
        // Map each result → BusinessHoliday { Source=SystemImport, ExternalId="{countryCode}-{year}-{date}", IsRecurring=false }
        // Skip where ExternalId already exists (idempotent).
        return Task.FromResult(0);
    }

    public async Task<bool> IsHolidayAsync(Guid businessId, DateOnly date, CancellationToken ct = default)
    {
        return await db.BusinessHolidays.AnyAsync(h =>
            h.BusinessId == businessId &&
            h.IsActive &&
            (h.IsRecurring
                ? h.Date.Month == date.Month && h.Date.Day == date.Day
                : h.Date == date),
            ct);
    }
}

// ── AttendancePolicyService ───────────────────────────────────────────────────

public sealed class AttendancePolicyService(ApplicationDbContext db) : IAttendancePolicyService
{
    public async Task<IReadOnlyList<AttendancePolicyDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default)
        => await db.AttendancePolicies
            .Include(p => p.Branch)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .Select(p => p.ToDto(p.Branch != null ? p.Branch.Name : null))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AttendancePolicyDto>> GetByBranchAsync(Guid branchId, CancellationToken ct = default)
    {
        var branch = await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == branchId, ct)
            ?? throw new KeyNotFoundException("Branch not found.");
        return await db.AttendancePolicies
            .Where(p => p.BranchId == branchId && p.IsActive)
            .Select(p => p.ToDto(branch.Name))
            .ToListAsync(ct);
    }

    public async Task<AttendancePolicyDto?> GetByIdAsync(Guid policyId, CancellationToken ct = default)
    {
        var p = await db.AttendancePolicies
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == policyId, ct);
        return p?.ToDto(p.Branch?.Name);
    }

    public async Task<AttendancePolicyDto> UpsertAsync(UpsertAttendancePolicyRequest req, CancellationToken ct = default)
    {
        AttendancePolicy? policy = null;

        // Find existing by exact scope key
        if (req.Scope == AttendanceScope.Individual && req.EmployeeId.HasValue)
            policy = await db.AttendancePolicies.FirstOrDefaultAsync(p =>
                p.BusinessId == req.BusinessId && p.EmployeeId == req.EmployeeId, ct);
        else if (req.Scope == AttendanceScope.Department && req.Department != null)
            policy = await db.AttendancePolicies.FirstOrDefaultAsync(p =>
                p.BusinessId == req.BusinessId && p.BranchId == req.BranchId && p.Department == req.Department, ct);
        else
            policy = await db.AttendancePolicies.FirstOrDefaultAsync(p =>
                p.BusinessId == req.BusinessId && p.BranchId == req.BranchId && p.Scope == req.Scope, ct);

        if (policy is null)
        {
            policy = new AttendancePolicy { BusinessId = req.BusinessId };
            db.AttendancePolicies.Add(policy);
        }
        else
        {
            policy.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }

        policy.BranchId = req.BranchId;
        policy.Scope = req.Scope;
        policy.Department = req.Department;
        policy.EmployeeId = req.EmployeeId;
        policy.Name = req.Name;
        policy.Description = req.Description;
        policy.ShiftStart = req.ShiftStart;
        policy.ShiftEnd = req.ShiftEnd;
        policy.BreakMinutes = req.BreakMinutes;
        policy.LateGraceMinutes = req.LateGraceMinutes;
        policy.RequiredHoursPerDay = req.RequiredHoursPerDay;
        policy.WorkingDaysMask = req.WorkingDaysMask;
        policy.EmployeeCanViewOwn = req.EmployeeCanViewOwn;
        policy.ManagerCanView = req.ManagerCanView;
        policy.HrCanView = req.HrCanView;
        policy.IsActive = true;

        await db.SaveChangesAsync(ct);

        var branch = policy.BranchId.HasValue
            ? await db.BusinessBranches.FirstOrDefaultAsync(x => x.Id == policy.BranchId.Value, ct)
            : null;
        return policy.ToDto(branch?.Name);
    }

    public async Task DeleteAsync(Guid policyId, CancellationToken ct = default)
    {
        var p = await db.AttendancePolicies.FirstOrDefaultAsync(x => x.Id == policyId, ct)
            ?? throw new KeyNotFoundException("Policy not found.");
        p.IsActive = false;
        p.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<AttendancePolicyDto?> ResolveForEmployeeAsync(Guid businessId, Guid employeeId, CancellationToken ct = default)
    {
        // Determine the employee's primary branch
        var branchAssignment = await db.BranchEmployees
            .Include(be => be.Branch)
            .FirstOrDefaultAsync(be => be.EmployeeId == employeeId && be.IsPrimary && be.ReleasedOn == null, ct);

        var branchId = branchAssignment?.BranchId;
        var department = branchAssignment?.Branch?.Name; // placeholder — real system uses Employee.Department

        // Get all active policies for this business, ordered by most specific first
        var policies = await db.AttendancePolicies
            .Include(p => p.Branch)
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .ToListAsync(ct);

        // Priority chain: Individual → Department → Branch → Business-default (BranchId null)
        var resolved =
            policies.FirstOrDefault(p => p.Scope == AttendanceScope.Individual && p.EmployeeId == employeeId)
            ?? policies.FirstOrDefault(p => p.Scope == AttendanceScope.Department && p.BranchId == branchId)
            ?? policies.FirstOrDefault(p => p.Scope == AttendanceScope.Branch && p.BranchId == branchId)
            ?? policies.FirstOrDefault(p => p.BranchId == null);

        return resolved?.ToDto(resolved.Branch?.Name);
    }
}
