using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;

namespace Monolithic.Api.Modules.Business.Application;

// ═══════════════════════════════════════════════════════════════════════════════
// DRY Mapper — single-source mapping for all AP entities
// ═══════════════════════════════════════════════════════════════════════════════
file static class ApMapper
{
    public static VendorCreditTermDto ToDto(this VendorCreditTerm t) => new(
        t.Id, t.BusinessId, t.Name, t.NetDays,
        t.EarlyPayDiscountPercent, t.EarlyPayDiscountDays,
        t.IsCod, t.IsDefault, t.IsActive, t.CreatedAtUtc);

    public static VendorClassDto ToDto(this VendorClass c) => new(
        c.Id, c.BusinessId, c.Name, c.Code, c.Description,
        c.ColorHex, c.SortOrder, c.IsActive, c.CreatedAtUtc);

    public static VendorProfileDto ToDto(this VendorProfile p) => new(
        p.VendorId,
        p.DefaultVatPercent,
        p.VatRegistrationNumber,
        p.IsVatRegistered,
        p.CreditTermId,
        p.CreditTerm?.Name,
        p.CreditTermDaysOverride,
        p.CreditTermDaysOverride ?? p.CreditTerm?.NetDays ?? 30,
        p.CreditLimitBase,
        p.PreferredPaymentMethod,
        p.PreferredBankAccountId,
        p.MinimumPaymentAmount,
        p.VendorClassId,
        p.VendorClass?.Name,
        p.VendorClass?.ColorHex,
        p.PerformanceRating,
        p.RelationshipNotes,
        p.IsOnHold,
        p.HoldReason,
        p.IsBlacklisted,
        p.BlacklistReason,
        p.CreatedAtUtc,
        p.ModifiedAtUtc);

    public static ApPaymentSessionLineDto ToDto(this ApPaymentSessionLine l, DateOnly today) => new(
        l.Id,
        l.VendorBillId,
        l.VendorBill?.BillNumber ?? string.Empty,
        l.VendorBill?.DueDate ?? DateOnly.MinValue,
        l.VendorBill is { } b && b.DueDate < today && b.AmountDue > 0
            ? (today.ToDateTime(TimeOnly.MinValue) - b.DueDate.ToDateTime(TimeOnly.MinValue)).Days
            : 0,
        l.AllocatedAmount,
        l.BillAmountDueBefore,
        l.BillAmountDueAfter,
        l.IsPartialPayment,
        l.VendorBillPaymentId);

    public static ApPaymentSessionDto ToDto(this ApPaymentSession s, DateOnly today) => new(
        s.Id, s.BusinessId, s.VendorId,
        s.Vendor?.Name ?? string.Empty,
        s.PaymentMode.ToString(),
        s.Status.ToString(),
        s.Reference,
        s.BankAccountId,
        s.TotalAmount, s.TotalAmountBase,
        s.CurrencyCode, s.ExchangeRate,
        s.PaymentMethod, s.PaymentDate,
        s.Notes, s.CreatedAtUtc, s.PostedAtUtc,
        s.Lines.OrderBy(l => l.VendorBill?.DueDate).Select(l => l.ToDto(today)).ToList());

    public static ApCreditNoteApplicationDto ToDto(this ApCreditNoteApplication a) => new(
        a.Id, a.CreditNoteId, a.VendorBillId,
        a.VendorBill?.BillNumber ?? string.Empty,
        a.AppliedAmount, a.ApplicationDate, a.Notes, a.CreatedAtUtc);

    public static ApCreditNoteDto ToDto(this ApCreditNote cn) => new(
        cn.Id, cn.BusinessId, cn.VendorId,
        cn.Vendor?.Name ?? string.Empty,
        cn.OriginalVendorBillId,
        cn.OriginalVendorBill?.BillNumber,
        cn.Type.ToString(), cn.Status.ToString(),
        cn.CreditNoteNumber, cn.VendorReference,
        cn.IssueDate, cn.CurrencyCode, cn.ExchangeRate,
        cn.CreditAmount, cn.CreditAmountBase,
        cn.AmountApplied, cn.AmountRemaining,
        cn.Reason, cn.Notes, cn.CreatedAtUtc,
        cn.Applications.Select(a => a.ToDto()).ToList());

    public static ApPaymentScheduleDto ToDto(this ApPaymentSchedule s) => new(
        s.Id, s.BusinessId, s.VendorId,
        s.Vendor?.Name ?? string.Empty,
        s.VendorBillId,
        s.VendorBill?.BillNumber ?? string.Empty,
        s.Status.ToString(),
        s.ScheduledDate, s.ScheduledAmount, s.CurrencyCode,
        s.BankAccountId, s.PaymentMethod, s.Notes,
        s.ExecutedSessionId, s.ExecutedAtUtc, s.CreatedAtUtc);

    // Computes effective due date from a bill date and credit term
    public static DateOnly ComputeDueDate(DateOnly billDate, VendorCreditTerm? creditTerm, int? overrideDays)
    {
        var netDays = overrideDays ?? creditTerm?.NetDays ?? 30;
        return billDate.AddDays(netDays);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VendorCreditTermService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class VendorCreditTermService(ApplicationDbContext db) : IVendorCreditTermService
{
    public async Task<IReadOnlyList<VendorCreditTermDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default)
        => await db.VendorCreditTerms
            .AsNoTracking()
            .Where(t => t.BusinessId == businessId)
            .OrderBy(t => t.NetDays)
            .Select(t => t.ToDto())
            .ToListAsync(ct);

    public async Task<VendorCreditTermDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.VendorCreditTerms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t?.ToDto();
    }

    public async Task<VendorCreditTermDto> CreateAsync(CreateVendorCreditTermRequest req, CancellationToken ct = default)
    {
        var term = new VendorCreditTerm
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            Name = req.Name.Trim(),
            NetDays = req.NetDays,
            EarlyPayDiscountPercent = req.EarlyPayDiscountPercent,
            EarlyPayDiscountDays = req.EarlyPayDiscountDays,
            IsCod = req.IsCod,
            IsDefault = req.IsDefault,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (req.IsDefault)
            await ClearDefaultFlagAsync(req.BusinessId, ct);

        db.VendorCreditTerms.Add(term);
        await db.SaveChangesAsync(ct);
        return term.ToDto();
    }

    public async Task<VendorCreditTermDto> UpdateAsync(Guid id, UpdateVendorCreditTermRequest req, CancellationToken ct = default)
    {
        var term = await db.VendorCreditTerms.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Credit term not found.");

        if (req.IsDefault)
            await ClearDefaultFlagAsync(term.BusinessId, ct);

        term.Name = req.Name.Trim();
        term.NetDays = req.NetDays;
        term.EarlyPayDiscountPercent = req.EarlyPayDiscountPercent;
        term.EarlyPayDiscountDays = req.EarlyPayDiscountDays;
        term.IsCod = req.IsCod;
        term.IsDefault = req.IsDefault;
        term.IsActive = req.IsActive;
        term.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return term.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var term = await db.VendorCreditTerms.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Credit term not found.");
        var inUse = await db.VendorProfiles.AnyAsync(p => p.CreditTermId == id, ct);
        if (inUse) throw new InvalidOperationException("Credit term is assigned to one or more vendors. Remove the assignment first.");
        db.VendorCreditTerms.Remove(term);
        await db.SaveChangesAsync(ct);
    }

    private async Task ClearDefaultFlagAsync(Guid businessId, CancellationToken ct)
    {
        var currentDefault = await db.VendorCreditTerms.FirstOrDefaultAsync(t => t.BusinessId == businessId && t.IsDefault, ct);
        if (currentDefault is not null)
        {
            currentDefault.IsDefault = false;
            currentDefault.ModifiedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VendorClassService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class VendorClassService(ApplicationDbContext db) : IVendorClassService
{
    public async Task<IReadOnlyList<VendorClassDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default)
        => await db.VendorClasses
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(ct);

    public async Task<VendorClassDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.VendorClasses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c?.ToDto();
    }

    public async Task<VendorClassDto> CreateAsync(CreateVendorClassRequest req, CancellationToken ct = default)
    {
        var cls = new VendorClass
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            Name = req.Name.Trim(),
            Code = req.Code.Trim().ToUpperInvariant(),
            Description = req.Description.Trim(),
            ColorHex = req.ColorHex,
            SortOrder = req.SortOrder,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.VendorClasses.Add(cls);
        await db.SaveChangesAsync(ct);
        return cls.ToDto();
    }

    public async Task<VendorClassDto> UpdateAsync(Guid id, UpdateVendorClassRequest req, CancellationToken ct = default)
    {
        var cls = await db.VendorClasses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Vendor class not found.");

        cls.Name = req.Name.Trim();
        cls.Code = req.Code.Trim().ToUpperInvariant();
        cls.Description = req.Description.Trim();
        cls.ColorHex = req.ColorHex;
        cls.SortOrder = req.SortOrder;
        cls.IsActive = req.IsActive;
        cls.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return cls.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cls = await db.VendorClasses.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Vendor class not found.");
        var inUse = await db.VendorProfiles.AnyAsync(p => p.VendorClassId == id, ct);
        if (inUse) throw new InvalidOperationException("Vendor class is assigned to one or more vendors.");
        db.VendorClasses.Remove(cls);
        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// VendorProfileService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class VendorProfileService(ApplicationDbContext db) : IVendorProfileService
{
    private static IQueryable<VendorProfile> ProfileWithIncludes(ApplicationDbContext db, Guid vendorId)
        => db.VendorProfiles
            .Include(p => p.CreditTerm)
            .Include(p => p.VendorClass)
            .Where(p => p.VendorId == vendorId);

    public async Task<VendorProfileDto?> GetByVendorAsync(Guid vendorId, CancellationToken ct = default)
    {
        var p = await ProfileWithIncludes(db, vendorId).AsNoTracking().FirstOrDefaultAsync(ct);
        return p?.ToDto();
    }

    public async Task<VendorProfileDto> UpsertAsync(Guid vendorId, UpsertVendorProfileRequest req, CancellationToken ct = default)
    {
        var profile = await ProfileWithIncludes(db, vendorId).FirstOrDefaultAsync(ct);
        var now = DateTimeOffset.UtcNow;

        if (req.PerformanceRating is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(req.PerformanceRating), "Rating must be between 0 and 5.");

        if (profile is null)
        {
            // Validate vendor exists
            if (!await db.Vendors.AnyAsync(v => v.Id == vendorId, ct))
                throw new KeyNotFoundException("Vendor not found.");

            profile = new VendorProfile { VendorId = vendorId, CreatedAtUtc = now };
            db.VendorProfiles.Add(profile);
        }
        else
        {
            profile.ModifiedAtUtc = now;
        }

        profile.DefaultVatPercent = req.DefaultVatPercent;
        profile.VatRegistrationNumber = req.VatRegistrationNumber;
        profile.IsVatRegistered = req.IsVatRegistered;
        profile.CreditTermId = req.CreditTermId;
        profile.CreditTermDaysOverride = req.CreditTermDaysOverride;
        profile.CreditLimitBase = req.CreditLimitBase;
        profile.PreferredPaymentMethod = req.PreferredPaymentMethod;
        profile.PreferredBankAccountId = req.PreferredBankAccountId;
        profile.MinimumPaymentAmount = req.MinimumPaymentAmount;
        profile.VendorClassId = req.VendorClassId;
        profile.PerformanceRating = req.PerformanceRating;
        profile.RelationshipNotes = req.RelationshipNotes;
        profile.IsOnHold = req.IsOnHold;
        profile.HoldReason = req.HoldReason;
        profile.IsBlacklisted = req.IsBlacklisted;
        profile.BlacklistReason = req.BlacklistReason;

        await db.SaveChangesAsync(ct);

        // Reload with nav for full DTO
        await db.Entry(profile).Reference(p => p.CreditTerm).LoadAsync(ct);
        await db.Entry(profile).Reference(p => p.VendorClass).LoadAsync(ct);
        return profile.ToDto();
    }

    public async Task SetHoldAsync(Guid vendorId, bool onHold, string reason, CancellationToken ct = default)
    {
        var profile = await db.VendorProfiles.FirstOrDefaultAsync(p => p.VendorId == vendorId, ct)
            ?? throw new KeyNotFoundException("Vendor profile not found.");
        profile.IsOnHold = onHold;
        profile.HoldReason = onHold ? reason : string.Empty;
        profile.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetBlacklistAsync(Guid vendorId, bool blacklisted, string reason, CancellationToken ct = default)
    {
        var profile = await db.VendorProfiles.FirstOrDefaultAsync(p => p.VendorId == vendorId, ct)
            ?? throw new KeyNotFoundException("Vendor profile not found.");
        profile.IsBlacklisted = blacklisted;
        profile.BlacklistReason = blacklisted ? reason : string.Empty;
        profile.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRatingAsync(Guid vendorId, decimal rating, CancellationToken ct = default)
    {
        if (rating is < 0 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be 0–5.");
        var profile = await db.VendorProfiles.FirstOrDefaultAsync(p => p.VendorId == vendorId, ct)
            ?? throw new KeyNotFoundException("Vendor profile not found.");
        profile.PerformanceRating = rating;
        profile.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ApDashboardService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ApDashboardService(ApplicationDbContext db) : IApDashboardService
{
    public async Task<ApDashboardDto> GetDashboardAsync(Guid businessId, CancellationToken ct = default)
    {
        var business = await db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Single query to get open bills with vendor and profile
        var openBills = await db.VendorBills
            .AsNoTracking()
            .Where(b => b.BusinessId == businessId
                && b.AmountDue > 0
                && (b.Status == VendorBillStatus.Open
                    || b.Status == VendorBillStatus.PartiallyPaid
                    || b.Status == VendorBillStatus.Overdue))
            .Select(b => new
            {
                b.VendorId, b.AmountDue, b.DueDate, b.Status,
                VendorName = b.Vendor.Name
            })
            .ToListAsync(ct);

        var pendingSchedules = await db.ApPaymentSchedules
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.Status == ApPaymentScheduleStatus.Scheduled)
            .Select(s => new { s.VendorId, s.ScheduledAmount })
            .ToListAsync(ct);

        var profiles = await db.VendorProfiles
            .AsNoTracking()
            .Include(p => p.VendorClass)
            .Where(p => db.Vendors.Any(v => v.Id == p.VendorId && v.BusinessId == businessId))
            .ToListAsync(ct);

        var profileByVendor = profiles.ToDictionary(p => p.VendorId);
        var scheduleByVendor = pendingSchedules
            .GroupBy(s => s.VendorId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.ScheduledAmount));

        var vendorSummaries = openBills
            .GroupBy(b => b.VendorId)
            .Select(g =>
            {
                var overdueBills = g.Where(b => b.DueDate < today).ToList();
                profileByVendor.TryGetValue(g.Key, out var profile);

                return new VendorApSummaryDto(
                    VendorId: g.Key,
                    VendorName: g.First().VendorName,
                    VendorClassName: profile?.VendorClass?.Name,
                    VendorClassColorHex: profile?.VendorClass?.ColorHex,
                    PerformanceRating: profile?.PerformanceRating ?? 0,
                    IsOnHold: profile?.IsOnHold ?? false,
                    IsBlacklisted: profile?.IsBlacklisted ?? false,
                    OpenBillCount: g.Count(),
                    OverdueBillCount: overdueBills.Count,
                    TotalOwed: g.Sum(b => b.AmountDue),
                    TotalOverdue: overdueBills.Sum(b => b.AmountDue),
                    TotalPendingScheduled: scheduleByVendor.GetValueOrDefault(g.Key, 0),
                    MaxDaysOverdue: overdueBills.Any()
                        ? overdueBills.Max(b => (today.ToDateTime(TimeOnly.MinValue) - b.DueDate.ToDateTime(TimeOnly.MinValue)).Days)
                        : 0,
                    CurrencyCode: business?.BaseCurrencyCode ?? "USD",
                    EarliestDueDate: g.Min(b => (DateTimeOffset?)b.DueDate.ToDateTime(TimeOnly.MinValue))
                );
            })
            .OrderByDescending(s => s.TotalOverdue)
            .ThenByDescending(s => s.TotalOwed)
            .ToList();

        return new ApDashboardDto(
            BusinessId: businessId,
            BaseCurrencyCode: business?.BaseCurrencyCode ?? "USD",
            TotalOwed: vendorSummaries.Sum(v => v.TotalOwed),
            TotalOverdue: vendorSummaries.Sum(v => v.TotalOverdue),
            TotalPendingScheduled: vendorSummaries.Sum(v => v.TotalPendingScheduled),
            TotalVendorsWithOpenBills: vendorSummaries.Count,
            TotalOverdueBills: vendorSummaries.Sum(v => v.OverdueBillCount),
            Vendors: vendorSummaries
        );
    }

    public async Task<VendorApSummaryDto?> GetVendorSummaryAsync(Guid businessId, Guid vendorId, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(businessId, ct);
        return dashboard.Vendors.FirstOrDefault(v => v.VendorId == vendorId);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ApPaymentSessionService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ApPaymentSessionService(ApplicationDbContext db) : IApPaymentSessionService
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.Date);

    public async Task<ApPaymentSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct);
        return session?.ToDto(Today);
    }

    public async Task<IReadOnlyList<ApPaymentSessionDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default)
        => (await db.ApPaymentSessions
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.VendorId == vendorId)
            .Include(s => s.Vendor)
            .Include(s => s.Lines).ThenInclude(l => l.VendorBill)
            .OrderByDescending(s => s.PaymentDate)
            .ToListAsync(ct))
            .Select(s => s.ToDto(Today))
            .ToList();

    /// <summary>
    /// Prepares a draft payment session without persisting to DB yet.
    /// Validates bills, calculates allocations, and saves as Draft.
    /// </summary>
    public async Task<ApPaymentSessionDto> PrepareAsync(
        CreateApPaymentSessionRequest req,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        // Guard: vendor AP profile
        var profile = await db.VendorProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.VendorId == req.VendorId, ct);
        if (profile?.IsOnHold == true)
            throw new InvalidOperationException($"Vendor is on payment hold: {profile.HoldReason}");
        if (profile?.IsBlacklisted == true)
            throw new InvalidOperationException("Vendor is blacklisted — payments blocked.");

        // Load eligible bills for this vendor (oldest DueDate first)
        var eligibleBills = await db.VendorBills
            .Where(b => b.BusinessId == req.BusinessId
                && b.VendorId == req.VendorId
                && b.AmountDue > 0
                && (b.Status == VendorBillStatus.Open
                    || b.Status == VendorBillStatus.PartiallyPaid
                    || b.Status == VendorBillStatus.Overdue))
            .OrderBy(b => b.DueDate)   // oldest first
            .ToListAsync(ct);

        if (!eligibleBills.Any())
            throw new InvalidOperationException("No outstanding bills found for this vendor.");

        List<ApPaymentSessionLine> lines;

        if (req.PaymentMode == ApPaymentMode.BulkBillPayment)
        {
            lines = AllocateBulk(req.TotalAmount, eligibleBills);
        }
        else
        {
            // SelectedBillPayment: validate caller-supplied lines
            if (req.Bills is null || !req.Bills.Any())
                throw new ArgumentException("SelectedBillPayment requires at least one bill line.");

            var requestedTotal = req.Bills.Sum(l => l.AllocatedAmount);
            if (Math.Abs(requestedTotal - req.TotalAmount) > 0.01m)
                throw new InvalidOperationException($"Sum of bill allocations ({requestedTotal}) must equal TotalAmount ({req.TotalAmount}).");

            var billById = eligibleBills.ToDictionary(b => b.Id);
            lines = [];

            foreach (var line in req.Bills)
            {
                if (!billById.TryGetValue(line.VendorBillId, out var bill))
                    throw new KeyNotFoundException($"Bill {line.VendorBillId} not found or not eligible for payment.");

                if (line.AllocatedAmount <= 0)
                    throw new ArgumentException($"Allocation amount for bill {bill.BillNumber} must be positive.");

                if (line.AllocatedAmount > bill.AmountDue + 0.01m)
                    throw new InvalidOperationException(
                        $"Allocation ({line.AllocatedAmount}) exceeds Amount Due on bill {bill.BillNumber} ({bill.AmountDue}).");

                lines.Add(new ApPaymentSessionLine
                {
                    Id = Guid.NewGuid(),
                    VendorBillId = bill.Id,
                    AllocatedAmount = line.AllocatedAmount,
                    BillAmountDueBefore = bill.AmountDue,
                    BillAmountDueAfter = decimal.Round(bill.AmountDue - line.AllocatedAmount, 2),
                    IsPartialPayment = line.AllocatedAmount < bill.AmountDue - 0.005m
                });
            }
        }

        var session = new ApPaymentSession
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            VendorId = req.VendorId,
            PaymentMode = req.PaymentMode,
            Status = ApPaymentSessionStatus.Draft,
            Reference = req.Reference,
            BankAccountId = req.BankAccountId,
            TotalAmount = req.TotalAmount,
            TotalAmountBase = decimal.Round(req.TotalAmount * req.ExchangeRate, 2),
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            PaymentMethod = req.PaymentMethod,
            PaymentDate = req.PaymentDate,
            Notes = req.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        foreach (var line in lines)
            line.SessionId = session.Id;

        db.ApPaymentSessions.Add(session);
        db.ApPaymentSessionLines.AddRange(lines);
        await db.SaveChangesAsync(ct);

        return (await LoadSessionAsync(session.Id, ct))!.ToDto(Today);
    }

    public async Task<ApPaymentSessionDto> PostAsync(Guid sessionId, Guid postedByUserId, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Payment session not found.");

        if (session.Status == ApPaymentSessionStatus.Posted)
            return session.ToDto(Today); // Idempotent

        if (session.Status != ApPaymentSessionStatus.Draft)
            throw new InvalidOperationException($"Cannot post a session with status {session.Status}.");

        // Apply each line: create VendorBillPayment and update bill
        foreach (var line in session.Lines)
        {
            var bill = await db.VendorBills.FirstOrDefaultAsync(b => b.Id == line.VendorBillId, ct)
                ?? throw new KeyNotFoundException($"Bill {line.VendorBillId} not found.");

            var payment = new VendorBillPayment
            {
                Id = Guid.NewGuid(),
                VendorBillId = bill.Id,
                BankAccountId = session.BankAccountId,
                Amount = line.AllocatedAmount,
                AmountBase = decimal.Round(line.AllocatedAmount * session.ExchangeRate, 2),
                CurrencyCode = session.CurrencyCode,
                ExchangeRate = session.ExchangeRate,
                PaymentDate = session.PaymentDate,
                PaymentMethod = session.PaymentMethod,
                Reference = session.Reference,
                Notes = $"AP Session {session.Id}",
                CreatedByUserId = postedByUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            bill.AmountPaid = decimal.Round(bill.AmountPaid + line.AllocatedAmount, 2);
            bill.AmountDue = decimal.Round(bill.TotalAmount - bill.AmountPaid, 2);
            bill.Status = bill.AmountDue <= 0
                ? VendorBillStatus.Paid
                : (bill.DueDate < Today ? VendorBillStatus.Overdue : VendorBillStatus.PartiallyPaid);
            bill.ModifiedAtUtc = DateTimeOffset.UtcNow;

            db.VendorBillPayments.Add(payment);
            line.VendorBillPaymentId = payment.Id;
        }

        session.Status = ApPaymentSessionStatus.Posted;
        session.PostedByUserId = postedByUserId;
        session.PostedAtUtc = DateTimeOffset.UtcNow;
        session.ModifiedAtUtc = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return session.ToDto(Today);
    }

    public async Task ReverseAsync(Guid sessionId, Guid reversedByUserId, CancellationToken ct = default)
    {
        var session = await LoadSessionAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Payment session not found.");

        if (session.Status != ApPaymentSessionStatus.Posted)
            throw new InvalidOperationException("Only posted sessions can be reversed.");

        foreach (var line in session.Lines)
        {
            if (line.VendorBillPaymentId.HasValue)
            {
                var payment = await db.VendorBillPayments
                    .FirstOrDefaultAsync(p => p.Id == line.VendorBillPaymentId.Value, ct);

                if (payment is not null)
                {
                    var bill = await db.VendorBills.FirstOrDefaultAsync(b => b.Id == line.VendorBillId, ct);
                    if (bill is not null)
                    {
                        bill.AmountPaid = decimal.Round(bill.AmountPaid - payment.Amount, 2);
                        bill.AmountDue = decimal.Round(bill.TotalAmount - bill.AmountPaid, 2);
                        bill.Status = bill.DueDate < Today && bill.AmountDue > 0
                            ? VendorBillStatus.Overdue
                            : bill.AmountDue > 0 ? VendorBillStatus.Open : VendorBillStatus.Paid;
                        bill.ModifiedAtUtc = DateTimeOffset.UtcNow;
                    }
                    db.VendorBillPayments.Remove(payment);
                    line.VendorBillPaymentId = null;
                }
            }
        }

        session.Status = ApPaymentSessionStatus.Reversed;
        session.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Allocates totalAmount to bills ordered oldest DueDate first.
    /// The last bill may receive partial allocation.
    /// </summary>
    private static List<ApPaymentSessionLine> AllocateBulk(decimal totalAmount, List<VendorBill> bills)
    {
        var lines = new List<ApPaymentSessionLine>();
        var remaining = totalAmount;

        foreach (var bill in bills)
        {
            if (remaining <= 0) break;

            var allocated = Math.Min(remaining, bill.AmountDue);
            allocated = decimal.Round(allocated, 2);

            lines.Add(new ApPaymentSessionLine
            {
                Id = Guid.NewGuid(),
                VendorBillId = bill.Id,
                AllocatedAmount = allocated,
                BillAmountDueBefore = bill.AmountDue,
                BillAmountDueAfter = decimal.Round(bill.AmountDue - allocated, 2),
                IsPartialPayment = allocated < bill.AmountDue - 0.005m
            });

            remaining -= allocated;
        }

        return lines;
    }

    private async Task<ApPaymentSession?> LoadSessionAsync(Guid id, CancellationToken ct)
        => await db.ApPaymentSessions
            .Include(s => s.Vendor)
            .Include(s => s.Lines).ThenInclude(l => l.VendorBill)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}

// ═══════════════════════════════════════════════════════════════════════════════
// ApCreditNoteService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ApCreditNoteService(ApplicationDbContext db) : IApCreditNoteService
{
    public async Task<IReadOnlyList<ApCreditNoteDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default)
        => (await db.ApCreditNotes
            .AsNoTracking()
            .Where(c => c.BusinessId == businessId && c.VendorId == vendorId)
            .Include(c => c.Vendor)
            .Include(c => c.OriginalVendorBill)
            .Include(c => c.Applications).ThenInclude(a => a.VendorBill)
            .OrderByDescending(c => c.IssueDate)
            .ToListAsync(ct))
            .Select(c => c.ToDto())
            .ToList();

    public async Task<ApCreditNoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await LoadCreditNoteAsync(id, ct);
        return cn?.ToDto();
    }

    public async Task<ApCreditNoteDto> CreateAsync(
        CreateApCreditNoteRequest req,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        if (req.CreditAmount <= 0)
            throw new ArgumentException("Credit amount must be positive.");

        var cn = new ApCreditNote
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            VendorId = req.VendorId,
            OriginalVendorBillId = req.OriginalVendorBillId,
            Type = req.Type,
            Status = ApCreditNoteStatus.Draft,
            CreditNoteNumber = await GenerateCreditNoteNumberAsync(req.BusinessId, ct),
            VendorReference = req.VendorReference,
            IssueDate = req.IssueDate,
            CurrencyCode = req.CurrencyCode,
            ExchangeRate = req.ExchangeRate,
            CreditAmount = req.CreditAmount,
            CreditAmountBase = decimal.Round(req.CreditAmount * req.ExchangeRate, 2),
            AmountApplied = 0m,
            AmountRemaining = req.CreditAmount,
            Reason = req.Reason,
            Notes = req.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.ApCreditNotes.Add(cn);
        await db.SaveChangesAsync(ct);
        return (await LoadCreditNoteAsync(cn.Id, ct))!.ToDto();
    }

    public async Task<ApCreditNoteDto> ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default)
    {
        var cn = await db.ApCreditNotes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Credit note not found.");

        if (cn.Status != ApCreditNoteStatus.Draft)
            throw new InvalidOperationException("Only Draft credit notes can be confirmed.");

        cn.Status = ApCreditNoteStatus.Confirmed;
        cn.ApprovedByUserId = confirmedByUserId;
        cn.ApprovedAtUtc = DateTimeOffset.UtcNow;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return (await LoadCreditNoteAsync(id, ct))!.ToDto();
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var cn = await db.ApCreditNotes.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException("Credit note not found.");

        if (cn.Status == ApCreditNoteStatus.Applied)
            throw new InvalidOperationException("Cannot cancel a fully applied credit note.");

        cn.Status = ApCreditNoteStatus.Cancelled;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ApCreditNoteDto> ApplyToBillAsync(
        Guid creditNoteId,
        ApplyCreditNoteRequest req,
        Guid appliedByUserId,
        CancellationToken ct = default)
    {
        var cn = await db.ApCreditNotes
            .Include(c => c.Applications)
            .FirstOrDefaultAsync(c => c.Id == creditNoteId, ct)
            ?? throw new KeyNotFoundException("Credit note not found.");

        if (cn.Status != ApCreditNoteStatus.Confirmed)
            throw new InvalidOperationException("Credit note must be Confirmed before applying to a bill.");

        if (req.AppliedAmount <= 0)
            throw new ArgumentException("Applied amount must be positive.");

        if (req.AppliedAmount > cn.AmountRemaining + 0.01m)
            throw new InvalidOperationException($"Applied amount ({req.AppliedAmount}) exceeds remaining credit ({cn.AmountRemaining}).");

        var bill = await db.VendorBills.FirstOrDefaultAsync(b => b.Id == req.VendorBillId, ct)
            ?? throw new KeyNotFoundException("Vendor bill not found.");

        if (bill.AmountDue <= 0)
            throw new InvalidOperationException("Bill is already fully paid.");

        var applied = Math.Min(req.AppliedAmount, bill.AmountDue);
        applied = decimal.Round(applied, 2);

        var application = new ApCreditNoteApplication
        {
            Id = Guid.NewGuid(),
            CreditNoteId = creditNoteId,
            VendorBillId = bill.Id,
            AppliedAmount = applied,
            ApplicationDate = req.ApplicationDate,
            Notes = req.Notes,
            CreatedByUserId = appliedByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Update bill
        bill.AmountPaid = decimal.Round(bill.AmountPaid + applied, 2);
        bill.AmountDue = decimal.Round(bill.TotalAmount - bill.AmountPaid, 2);
        bill.Status = bill.AmountDue <= 0
            ? VendorBillStatus.Paid
            : bill.Status == VendorBillStatus.Open ? VendorBillStatus.PartiallyPaid : bill.Status;
        bill.ModifiedAtUtc = DateTimeOffset.UtcNow;

        // Update credit note
        cn.AmountApplied = decimal.Round(cn.AmountApplied + applied, 2);
        cn.AmountRemaining = decimal.Round(cn.CreditAmount - cn.AmountApplied, 2);
        cn.Status = cn.AmountRemaining <= 0 ? ApCreditNoteStatus.Applied : ApCreditNoteStatus.Confirmed;
        cn.ModifiedAtUtc = DateTimeOffset.UtcNow;

        db.ApCreditNoteApplications.Add(application);
        await db.SaveChangesAsync(ct);
        return (await LoadCreditNoteAsync(creditNoteId, ct))!.ToDto();
    }

    private async Task<ApCreditNote?> LoadCreditNoteAsync(Guid id, CancellationToken ct)
        => await db.ApCreditNotes
            .Include(c => c.Vendor)
            .Include(c => c.OriginalVendorBill)
            .Include(c => c.Applications).ThenInclude(a => a.VendorBill)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    private async Task<string> GenerateCreditNoteNumberAsync(Guid businessId, CancellationToken ct)
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"CN-{datePart}";
        var count = await db.ApCreditNotes.CountAsync(c => c.BusinessId == businessId && c.CreditNoteNumber.StartsWith(prefix), ct);
        return $"{prefix}-{count + 1:0000}";
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ApPaymentScheduleService
// ═══════════════════════════════════════════════════════════════════════════════
public sealed class ApPaymentScheduleService(
    ApplicationDbContext db,
    IApPaymentSessionService sessionService) : IApPaymentScheduleService
{
    public async Task<IReadOnlyList<ApPaymentScheduleDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default)
        => (await db.ApPaymentSchedules
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.VendorId == vendorId)
            .Include(s => s.Vendor)
            .Include(s => s.VendorBill)
            .OrderBy(s => s.ScheduledDate)
            .ToListAsync(ct))
            .Select(s => s.ToDto())
            .ToList();

    public async Task<IReadOnlyList<ApPaymentScheduleDto>> GetDueAsync(Guid businessId, DateOnly? asOfDate = null, CancellationToken ct = default)
    {
        var cutoff = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return (await db.ApPaymentSchedules
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId
                && s.Status == ApPaymentScheduleStatus.Scheduled
                && s.ScheduledDate <= cutoff)
            .Include(s => s.Vendor)
            .Include(s => s.VendorBill)
            .OrderBy(s => s.ScheduledDate)
            .ToListAsync(ct))
            .Select(s => s.ToDto())
            .ToList();
    }

    public async Task<ApPaymentScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.ApPaymentSchedules
            .AsNoTracking()
            .Include(x => x.Vendor)
            .Include(x => x.VendorBill)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return s?.ToDto();
    }

    public async Task<ApPaymentScheduleDto> CreateAsync(CreateApPaymentScheduleRequest req, Guid createdByUserId, CancellationToken ct = default)
    {
        if (req.ScheduledAmount <= 0)
            throw new ArgumentException("Scheduled amount must be positive.");

        var bill = await db.VendorBills.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == req.VendorBillId, ct)
            ?? throw new KeyNotFoundException("Vendor bill not found.");

        if (bill.AmountDue <= 0)
            throw new InvalidOperationException("Bill has no outstanding balance to schedule.");

        var schedule = new ApPaymentSchedule
        {
            Id = Guid.NewGuid(),
            BusinessId = req.BusinessId,
            VendorId = req.VendorId,
            VendorBillId = req.VendorBillId,
            Status = ApPaymentScheduleStatus.Scheduled,
            ScheduledDate = req.ScheduledDate,
            ScheduledAmount = req.ScheduledAmount,
            CurrencyCode = req.CurrencyCode,
            BankAccountId = req.BankAccountId,
            PaymentMethod = req.PaymentMethod,
            Notes = req.Notes,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.ApPaymentSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);
        await db.Entry(schedule).Reference(s => s.Vendor).LoadAsync(ct);
        await db.Entry(schedule).Reference(s => s.VendorBill).LoadAsync(ct);
        return schedule.ToDto();
    }

    public async Task<ApPaymentScheduleDto> UpdateAsync(Guid id, UpdateApPaymentScheduleRequest req, CancellationToken ct = default)
    {
        var schedule = await db.ApPaymentSchedules.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException("Payment schedule not found.");

        if (schedule.Status != ApPaymentScheduleStatus.Scheduled)
            throw new InvalidOperationException("Only Scheduled payments can be updated.");

        schedule.ScheduledDate = req.ScheduledDate;
        schedule.ScheduledAmount = req.ScheduledAmount;
        schedule.BankAccountId = req.BankAccountId;
        schedule.PaymentMethod = req.PaymentMethod;
        schedule.Notes = req.Notes;
        schedule.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await db.Entry(schedule).Reference(s => s.Vendor).LoadAsync(ct);
        await db.Entry(schedule).Reference(s => s.VendorBill).LoadAsync(ct);
        return schedule.ToDto();
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await db.ApPaymentSchedules.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new KeyNotFoundException("Payment schedule not found.");
        if (schedule.Status == ApPaymentScheduleStatus.Executed)
            throw new InvalidOperationException("Executed schedules cannot be cancelled.");
        schedule.Status = ApPaymentScheduleStatus.Cancelled;
        schedule.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ApPaymentSessionDto> ExecuteAsync(Guid scheduleId, Guid executedByUserId, CancellationToken ct = default)
    {
        var schedule = await db.ApPaymentSchedules
            .Include(s => s.VendorBill)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            ?? throw new KeyNotFoundException("Payment schedule not found.");

        if (schedule.Status != ApPaymentScheduleStatus.Scheduled)
            throw new InvalidOperationException("Only Scheduled payments can be executed.");

        // Create and immediately post a payment session for this scheduled payment
        var sessionReq = new CreateApPaymentSessionRequest(
            BusinessId: schedule.BusinessId,
            VendorId: schedule.VendorId,
            PaymentMode: ApPaymentMode.SelectedBillPayment,
            TotalAmount: schedule.ScheduledAmount,
            CurrencyCode: schedule.CurrencyCode,
            ExchangeRate: 1m,
            PaymentDate: DateOnly.FromDateTime(DateTime.UtcNow.Date),
            PaymentMethod: schedule.PaymentMethod,
            BankAccountId: schedule.BankAccountId,
            Reference: $"Schedule {schedule.Id}",
            Notes: schedule.Notes,
            Bills: [new ApPaymentLineRequest(schedule.VendorBillId, schedule.ScheduledAmount)]
        );

        var session = await sessionService.PrepareAsync(sessionReq, executedByUserId, ct);
        var posted = await sessionService.PostAsync(session.Id, executedByUserId, ct);

        schedule.Status = ApPaymentScheduleStatus.Executed;
        schedule.ExecutedSessionId = posted.Id;
        schedule.ExecutedAtUtc = DateTimeOffset.UtcNow;
        schedule.ModifiedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return posted;
    }
}
