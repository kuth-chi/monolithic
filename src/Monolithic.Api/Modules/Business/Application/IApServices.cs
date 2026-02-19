using Monolithic.Api.Modules.Business.Contracts;
using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Application;

// ── Vendor Credit Terms ───────────────────────────────────────────────────────
public interface IVendorCreditTermService
{
    Task<IReadOnlyList<VendorCreditTermDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default);
    Task<VendorCreditTermDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VendorCreditTermDto> CreateAsync(CreateVendorCreditTermRequest request, CancellationToken ct = default);
    Task<VendorCreditTermDto> UpdateAsync(Guid id, UpdateVendorCreditTermRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// ── Vendor Classes ────────────────────────────────────────────────────────────
public interface IVendorClassService
{
    Task<IReadOnlyList<VendorClassDto>> GetByBusinessAsync(Guid businessId, CancellationToken ct = default);
    Task<VendorClassDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VendorClassDto> CreateAsync(CreateVendorClassRequest request, CancellationToken ct = default);
    Task<VendorClassDto> UpdateAsync(Guid id, UpdateVendorClassRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

// ── Vendor Profile (AP extended data) ────────────────────────────────────────
public interface IVendorProfileService
{
    Task<VendorProfileDto?> GetByVendorAsync(Guid vendorId, CancellationToken ct = default);

    /// <summary>Creates or updates the AP profile for a vendor (upsert).</summary>
    Task<VendorProfileDto> UpsertAsync(Guid vendorId, UpsertVendorProfileRequest request, CancellationToken ct = default);

    Task SetHoldAsync(Guid vendorId, bool onHold, string reason, CancellationToken ct = default);
    Task SetBlacklistAsync(Guid vendorId, bool blacklisted, string reason, CancellationToken ct = default);
    Task UpdateRatingAsync(Guid vendorId, decimal rating, CancellationToken ct = default);
}

// ── AP Dashboard (Vendor Outstanding / Overdue List) ─────────────────────────
public interface IApDashboardService
{
    /// <summary>
    /// Main AP manager view: all vendors with open/overdue bills for the business.
    /// </summary>
    Task<ApDashboardDto> GetDashboardAsync(Guid businessId, CancellationToken ct = default);

    /// <summary>Per-vendor outstanding bill list, sorted oldest-due-date first.</summary>
    Task<VendorApSummaryDto?> GetVendorSummaryAsync(Guid businessId, Guid vendorId, CancellationToken ct = default);
}

// ── AP Payment Session ────────────────────────────────────────────────────────
public interface IApPaymentSessionService
{
    Task<ApPaymentSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<ApPaymentSessionDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default);

    /// <summary>
    /// Prepares a draft payment session.
    /// BulkBillPayment: auto-allocates to oldest bills first.
    /// SelectedBillPayment: validates caller-supplied allocation lines.
    /// Does NOT post — call PostAsync to apply payments.
    /// </summary>
    Task<ApPaymentSessionDto> PrepareAsync(CreateApPaymentSessionRequest request, Guid createdByUserId, CancellationToken ct = default);

    /// <summary>
    /// Posts the session: creates VendorBillPayment records, updates bill AmountDue/Status.
    /// Idempotent: no-op if already posted.
    /// </summary>
    Task<ApPaymentSessionDto> PostAsync(Guid sessionId, Guid postedByUserId, CancellationToken ct = default);

    /// <summary>Reverses a posted session. Voids the child VendorBillPayments and restores bill amounts.</summary>
    Task ReverseAsync(Guid sessionId, Guid reversedByUserId, CancellationToken ct = default);
}

// ── AP Credit Notes ───────────────────────────────────────────────────────────
public interface IApCreditNoteService
{
    Task<IReadOnlyList<ApCreditNoteDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default);
    Task<ApCreditNoteDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApCreditNoteDto> CreateAsync(CreateApCreditNoteRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<ApCreditNoteDto> ConfirmAsync(Guid id, Guid confirmedByUserId, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);

    /// <summary>Applies credit note balance to a specific bill (reduces bill AmountDue).</summary>
    Task<ApCreditNoteDto> ApplyToBillAsync(Guid creditNoteId, ApplyCreditNoteRequest request, Guid appliedByUserId, CancellationToken ct = default);
}

// ── AP Payment Schedule (Pay Later) ──────────────────────────────────────────
public interface IApPaymentScheduleService
{
    Task<IReadOnlyList<ApPaymentScheduleDto>> GetByVendorAsync(Guid businessId, Guid vendorId, CancellationToken ct = default);
    Task<IReadOnlyList<ApPaymentScheduleDto>> GetDueAsync(Guid businessId, DateOnly? asOfDate = null, CancellationToken ct = default);
    Task<ApPaymentScheduleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApPaymentScheduleDto> CreateAsync(CreateApPaymentScheduleRequest request, Guid createdByUserId, CancellationToken ct = default);
    Task<ApPaymentScheduleDto> UpdateAsync(Guid id, UpdateApPaymentScheduleRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);

    /// <summary>Executes a scheduled payment by creating and posting an ApPaymentSession for it.</summary>
    Task<ApPaymentSessionDto> ExecuteAsync(Guid scheduleId, Guid executedByUserId, CancellationToken ct = default);
}
