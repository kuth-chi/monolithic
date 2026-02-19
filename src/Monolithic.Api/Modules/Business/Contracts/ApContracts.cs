using Monolithic.Api.Modules.Business.Domain;

namespace Monolithic.Api.Modules.Business.Contracts;

// ═══════════════════════════════════════════════════════════════════════════════
// Vendor Credit Term Contracts
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record VendorCreditTermDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    int NetDays,
    decimal EarlyPayDiscountPercent,
    int EarlyPayDiscountDays,
    bool IsCod,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);

public sealed record CreateVendorCreditTermRequest(
    Guid BusinessId,
    string Name,
    int NetDays = 30,
    decimal EarlyPayDiscountPercent = 0,
    int EarlyPayDiscountDays = 0,
    bool IsCod = false,
    bool IsDefault = false
);

public sealed record UpdateVendorCreditTermRequest(
    string Name,
    int NetDays,
    decimal EarlyPayDiscountPercent,
    int EarlyPayDiscountDays,
    bool IsCod,
    bool IsDefault,
    bool IsActive
);

// ═══════════════════════════════════════════════════════════════════════════════
// Vendor Class Contracts
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record VendorClassDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string Code,
    string Description,
    string ColorHex,
    int SortOrder,
    bool IsActive,
    DateTimeOffset CreatedAtUtc
);

public sealed record CreateVendorClassRequest(
    Guid BusinessId,
    string Name,
    string Code,
    string Description = "",
    string ColorHex = "#6B7280",
    int SortOrder = 100
);

public sealed record UpdateVendorClassRequest(
    string Name,
    string Code,
    string Description,
    string ColorHex,
    int SortOrder,
    bool IsActive
);

// ═══════════════════════════════════════════════════════════════════════════════
// Vendor Profile Contracts
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record VendorProfileDto(
    Guid VendorId,
    decimal DefaultVatPercent,
    string VatRegistrationNumber,
    bool IsVatRegistered,
    Guid? CreditTermId,
    string? CreditTermName,
    int? CreditTermDaysOverride,
    int EffectiveCreditDays,            // resolved: override ?? creditTerm.NetDays ?? 30
    decimal? CreditLimitBase,
    string PreferredPaymentMethod,
    Guid? PreferredBankAccountId,
    decimal? MinimumPaymentAmount,
    Guid? VendorClassId,
    string? VendorClassName,
    string? VendorClassColorHex,
    decimal PerformanceRating,
    string RelationshipNotes,
    bool IsOnHold,
    string HoldReason,
    bool IsBlacklisted,
    string BlacklistReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ModifiedAtUtc
);

public sealed record UpsertVendorProfileRequest(
    decimal DefaultVatPercent = 0,
    string VatRegistrationNumber = "",
    bool IsVatRegistered = false,
    Guid? CreditTermId = null,
    int? CreditTermDaysOverride = null,
    decimal? CreditLimitBase = null,
    string PreferredPaymentMethod = "BankTransfer",
    Guid? PreferredBankAccountId = null,
    decimal? MinimumPaymentAmount = null,
    Guid? VendorClassId = null,
    decimal PerformanceRating = 0,
    string RelationshipNotes = "",
    bool IsOnHold = false,
    string HoldReason = "",
    bool IsBlacklisted = false,
    string BlacklistReason = ""
);

// ═══════════════════════════════════════════════════════════════════════════════
// Vendor AP Summary (dashboard list — owed / pending / overdue per vendor)
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record VendorApSummaryDto(
    Guid VendorId,
    string VendorName,
    string? VendorClassName,
    string? VendorClassColorHex,
    decimal PerformanceRating,
    bool IsOnHold,
    bool IsBlacklisted,
    int OpenBillCount,
    int OverdueBillCount,
    decimal TotalOwed,           // sum of AmountDue on all open/partial/overdue bills
    decimal TotalOverdue,        // sum of AmountDue on overdue bills only
    decimal TotalPendingScheduled, // sum of ScheduledAmount on pending schedules
    int MaxDaysOverdue,
    string CurrencyCode,
    DateTimeOffset? EarliestDueDate
);

// ═══════════════════════════════════════════════════════════════════════════════
// AP Payment Session Contracts
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Request to prepare (draft) a bulk payment session.
/// Mode 1 (BulkBillPayment): provide TotalAmount only — system auto-distributes oldest first.
/// Mode 2 (SelectedBillPayment): provide Bills list with explicit amounts.
/// </summary>
public sealed record CreateApPaymentSessionRequest(
    Guid BusinessId,
    Guid VendorId,
    ApPaymentMode PaymentMode,
    decimal TotalAmount,
    string CurrencyCode,
    decimal ExchangeRate,
    DateOnly PaymentDate,
    string PaymentMethod,
    Guid? BankAccountId,
    string Reference = "",
    string Notes = "",
    /// <summary>Required when PaymentMode = SelectedBillPayment.</summary>
    IList<ApPaymentLineRequest>? Bills = null
);

public sealed record ApPaymentLineRequest(
    Guid VendorBillId,
    decimal AllocatedAmount
);

public sealed record ApPaymentSessionDto(
    Guid Id,
    Guid BusinessId,
    Guid VendorId,
    string VendorName,
    string PaymentMode,
    string Status,
    string Reference,
    Guid? BankAccountId,
    decimal TotalAmount,
    decimal TotalAmountBase,
    string CurrencyCode,
    decimal ExchangeRate,
    string PaymentMethod,
    DateOnly PaymentDate,
    string Notes,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PostedAtUtc,
    IReadOnlyList<ApPaymentSessionLineDto> Lines
);

public sealed record ApPaymentSessionLineDto(
    Guid Id,
    Guid VendorBillId,
    string BillNumber,
    DateOnly BillDueDate,
    int DaysOverdue,
    decimal AllocatedAmount,
    decimal BillAmountDueBefore,
    decimal BillAmountDueAfter,
    bool IsPartialPayment,
    Guid? VendorBillPaymentId
);

// ═══════════════════════════════════════════════════════════════════════════════
// AP Credit Note Contracts
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ApCreditNoteDto(
    Guid Id,
    Guid BusinessId,
    Guid VendorId,
    string VendorName,
    Guid? OriginalVendorBillId,
    string? OriginalBillNumber,
    string Type,
    string Status,
    string CreditNoteNumber,
    string VendorReference,
    DateOnly IssueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CreditAmount,
    decimal CreditAmountBase,
    decimal AmountApplied,
    decimal AmountRemaining,
    string Reason,
    string Notes,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ApCreditNoteApplicationDto> Applications
);

public sealed record ApCreditNoteApplicationDto(
    Guid Id,
    Guid CreditNoteId,
    Guid VendorBillId,
    string BillNumber,
    decimal AppliedAmount,
    DateOnly ApplicationDate,
    string Notes,
    DateTimeOffset CreatedAtUtc
);

public sealed record CreateApCreditNoteRequest(
    Guid BusinessId,
    Guid VendorId,
    Guid? OriginalVendorBillId,
    ApCreditNoteType Type,
    DateOnly IssueDate,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal CreditAmount,
    string Reason,
    string Notes = "",
    string VendorReference = ""
);

public sealed record ApplyCreditNoteRequest(
    Guid VendorBillId,
    decimal AppliedAmount,
    DateOnly ApplicationDate,
    string Notes = ""
);

// ═══════════════════════════════════════════════════════════════════════════════
// AP Payment Schedule (Pay Later) Contracts
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ApPaymentScheduleDto(
    Guid Id,
    Guid BusinessId,
    Guid VendorId,
    string VendorName,
    Guid VendorBillId,
    string BillNumber,
    string Status,
    DateOnly ScheduledDate,
    decimal ScheduledAmount,
    string CurrencyCode,
    Guid? BankAccountId,
    string PaymentMethod,
    string Notes,
    Guid? ExecutedSessionId,
    DateTimeOffset? ExecutedAtUtc,
    DateTimeOffset CreatedAtUtc
);

public sealed record CreateApPaymentScheduleRequest(
    Guid BusinessId,
    Guid VendorId,
    Guid VendorBillId,
    DateOnly ScheduledDate,
    decimal ScheduledAmount,
    string CurrencyCode,
    Guid? BankAccountId = null,
    string PaymentMethod = "BankTransfer",
    string Notes = ""
);

public sealed record UpdateApPaymentScheduleRequest(
    DateOnly ScheduledDate,
    decimal ScheduledAmount,
    Guid? BankAccountId,
    string PaymentMethod,
    string Notes
);

// ═══════════════════════════════════════════════════════════════════════════════
// AP Dashboard
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ApDashboardDto(
    Guid BusinessId,
    string BaseCurrencyCode,
    decimal TotalOwed,
    decimal TotalOverdue,
    decimal TotalPendingScheduled,
    int TotalVendorsWithOpenBills,
    int TotalOverdueBills,
    IReadOnlyList<VendorApSummaryDto> Vendors
);
