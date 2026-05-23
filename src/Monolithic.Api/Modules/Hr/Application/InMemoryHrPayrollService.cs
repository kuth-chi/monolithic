using System.Collections.Concurrent;
using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public sealed class InMemoryHrPayrollService : IHrPayrollService
{
    private static readonly ConcurrentDictionary<Guid, List<PayrollRunDto>> PayrollByBusiness = new();

    public Task<IReadOnlyList<PayrollRunDto>> GetByBusinessAsync(
        Guid businessId,
        string? status,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rows = PayrollByBusiness.GetValueOrDefault(businessId) ?? [];
        IEnumerable<PayrollRunDto> query = rows;

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var result = query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<PayrollRunDto>>(result);
    }

    public Task<PayrollRunDto> CreateAsync(
        Guid businessId,
        CreatePayrollRunRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ValidateDates(request.PeriodStart, request.PeriodEnd, request.PayDate);

        var created = new PayrollRunDto(
            Id: Guid.NewGuid(),
            BusinessId: businessId,
            PayrollName: string.IsNullOrWhiteSpace(request.PayrollName) ? $"Payroll {DateTime.UtcNow:yyyy-MM}" : request.PayrollName.Trim(),
            PeriodStart: request.PeriodStart,
            PeriodEnd: request.PeriodEnd,
            PayDate: request.PayDate,
            Status: "Draft",
            EmployeeCount: 0,
            GrossAmount: 0m,
            NetAmount: 0m,
            CreatedAtUtc: DateTime.UtcNow,
            SubmittedAtUtc: null,
            ApprovedAtUtc: null,
            FinalizedAtUtc: null,
            Notes: string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            ReviewerComment: null);

        var list = PayrollByBusiness.GetOrAdd(businessId, _ => []);
        lock (list)
        {
            list.Add(created);
        }

        return Task.FromResult(created);
    }

    public Task<PayrollRunDto> SubmitAsync(
        Guid businessId,
        Guid payrollRunId,
        CancellationToken ct = default)
        => TransitionAsync(businessId, payrollRunId, "Submitted", null, ct, requireDraft: true, submitted: true);

    public Task<PayrollRunDto> ApproveAsync(
        Guid businessId,
        Guid payrollRunId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default)
        => TransitionAsync(businessId, payrollRunId, "Approved", reviewerUserId, ct, requireDraft: false, submitted: false, comment: comment, requireSubmitted: true);

    public Task<PayrollRunDto> FinalizeAsync(
        Guid businessId,
        Guid payrollRunId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default)
        => TransitionAsync(businessId, payrollRunId, "Finalized", reviewerUserId, ct, requireDraft: false, submitted: false, comment: comment, requireApproved: true, finalized: true);

    private Task<PayrollRunDto> TransitionAsync(
        Guid businessId,
        Guid payrollRunId,
        string targetStatus,
        Guid? reviewerUserId,
        CancellationToken ct,
        bool requireDraft,
        bool submitted,
        string? comment = null,
        bool requireSubmitted = false,
        bool requireApproved = false,
        bool finalized = false)
    {
        ct.ThrowIfCancellationRequested();

        var list = PayrollByBusiness.GetValueOrDefault(businessId)
            ?? throw new KeyNotFoundException("Payroll run not found.");

        lock (list)
        {
            var idx = list.FindIndex(x => x.Id == payrollRunId);
            if (idx < 0)
                throw new KeyNotFoundException("Payroll run not found.");

            var current = list[idx];
            if (requireDraft && !current.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only draft payroll runs can be submitted.");
            if (requireSubmitted && !current.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only submitted payroll runs can be approved.");
            if (requireApproved && !current.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only approved payroll runs can be finalized.");

            var updated = current with
            {
                Status = targetStatus,
                SubmittedAtUtc = submitted ? DateTime.UtcNow : current.SubmittedAtUtc,
                ApprovedAtUtc = targetStatus == "Approved" ? DateTime.UtcNow : current.ApprovedAtUtc,
                FinalizedAtUtc = finalized ? DateTime.UtcNow : current.FinalizedAtUtc,
                ReviewerComment = string.IsNullOrWhiteSpace(comment) ? current.ReviewerComment : comment.Trim(),
                GrossAmount = current.GrossAmount <= 0 ? 15000m : current.GrossAmount,
                NetAmount = current.NetAmount <= 0 ? 12000m : current.NetAmount,
                EmployeeCount = current.EmployeeCount <= 0 ? 1 : current.EmployeeCount,
            };

            if (reviewerUserId is not null && reviewerUserId != Guid.Empty)
            {
                updated = updated with { ReviewerComment = string.IsNullOrWhiteSpace(comment) ? current.ReviewerComment : comment.Trim() };
            }

            list[idx] = updated;
            return Task.FromResult(updated);
        }
    }

    private static void ValidateDates(DateOnly start, DateOnly end, DateOnly payDate)
    {
        if (start > end)
            throw new ArgumentException("PeriodStart must be earlier than or equal to PeriodEnd.");
        if (payDate < end)
            throw new ArgumentException("PayDate must be on or after the period end.");
    }
}
