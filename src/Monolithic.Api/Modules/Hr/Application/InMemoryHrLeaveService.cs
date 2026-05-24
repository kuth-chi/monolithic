using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public sealed class InMemoryHrLeaveService(ApplicationDbContext db) : IHrLeaveService
{
    private static readonly ConcurrentDictionary<Guid, List<LeaveRequestDto>> LeaveByBusiness = new();

    public Task<IReadOnlyList<LeaveRequestDto>> GetByBusinessAsync(
        Guid businessId,
        string? status,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rows = LeaveByBusiness.GetValueOrDefault(businessId) ?? [];
        IEnumerable<LeaveRequestDto> query = rows;

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var result = query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<LeaveRequestDto>>(result);
    }

    public async Task<LeaveRequestDto> CreateAsync(
        Guid businessId,
        CreateLeaveRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (request.EmployeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId is required.", nameof(request));
        if (request.StartDate > request.EndDate)
            throw new ArgumentException("StartDate must be earlier than or equal to EndDate.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.LeaveType))
            throw new ArgumentException("LeaveType is required.", nameof(request));

        var leaveType = request.LeaveType.Trim();
        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        var settingsRow = await db.BusinessSettings
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .Select(x => new
            {
                x.LeaveEntitlementByTypeJson,
                x.CompensationLeaveByEmployeeJson,
            })
            .FirstOrDefaultAsync(ct);

        var leaveQuota = ResolveLeaveQuota(
            settingsRow?.LeaveEntitlementByTypeJson,
            settingsRow?.CompensationLeaveByEmployeeJson,
            leaveType,
            request.EmployeeId);

        var created = new LeaveRequestDto(
            Id: Guid.NewGuid(),
            BusinessId: businessId,
            EmployeeId: request.EmployeeId,
            EmployeeName: $"Employee {request.EmployeeId.ToString()[..8]}",
            LeaveType: leaveType,
            StartDate: request.StartDate,
            EndDate: request.EndDate,
            TotalDays: totalDays,
            Reason: request.Reason.Trim(),
            Status: "Pending",
            CreatedAtUtc: DateTime.UtcNow,
            ReviewedAtUtc: null,
            ReviewedByUserId: null,
            ReviewerComment: null);

        var list = LeaveByBusiness.GetOrAdd(businessId, _ => []);
        lock (list)
        {
            if (leaveQuota.HasValue)
            {
                var consumedDays = list
                    .Where(x => x.EmployeeId == request.EmployeeId)
                    .Where(x => x.LeaveType.Equals(leaveType, StringComparison.OrdinalIgnoreCase))
                    .Where(x => x.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                             || x.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalDays);

                var remainingDays = leaveQuota.Value - consumedDays;
                if (totalDays > remainingDays)
                {
                    throw new InvalidOperationException(
                        $"{leaveType} leave quota exceeded. Remaining days: {Math.Max(0, remainingDays)}.");
                }
            }

            list.Add(created);
        }

        return created;
    }

    public Task<LeaveRequestDto> ApproveAsync(
        Guid businessId,
        Guid leaveRequestId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default)
        => ReviewAsync(businessId, leaveRequestId, reviewerUserId, "Approved", comment, ct);

    public Task<LeaveRequestDto> RejectAsync(
        Guid businessId,
        Guid leaveRequestId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default)
        => ReviewAsync(businessId, leaveRequestId, reviewerUserId, "Rejected", comment, ct);

    private Task<LeaveRequestDto> ReviewAsync(
        Guid businessId,
        Guid leaveRequestId,
        Guid reviewerUserId,
        string status,
        string? comment,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (reviewerUserId == Guid.Empty)
            throw new ArgumentException("Reviewer user id is required.", nameof(reviewerUserId));

        var list = LeaveByBusiness.GetValueOrDefault(businessId)
            ?? throw new KeyNotFoundException("Leave request not found.");

        lock (list)
        {
            var idx = list.FindIndex(x => x.Id == leaveRequestId);
            if (idx < 0)
                throw new KeyNotFoundException("Leave request not found.");

            var current = list[idx];
            if (!current.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending leave requests can be reviewed.");

            var updated = current with
            {
                Status = status,
                ReviewedAtUtc = DateTime.UtcNow,
                ReviewedByUserId = reviewerUserId,
                ReviewerComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            };

            list[idx] = updated;
            return Task.FromResult(updated);
        }
    }

    private static bool IsCompensationLeaveType(string leaveType)
        => leaveType.Equals("Compensation Leave", StringComparison.OrdinalIgnoreCase);

    private static int? ResolveLeaveQuota(
        string? leaveByTypeJson,
        string? compensationByEmployeeJson,
        string leaveType,
        Guid employeeId)
    {
        if (IsCompensationLeaveType(leaveType))
        {
            return ResolveCompensationQuota(compensationByEmployeeJson, employeeId) ?? 0;
        }

        return ResolveLeaveTypeQuota(leaveByTypeJson, leaveType);
    }

    private static int? ResolveLeaveTypeQuota(string? rawJson, string leaveType)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(rawJson);
            if (map is null)
                return null;

            foreach (var item in map)
            {
                if (item.Key.Equals(leaveType, StringComparison.OrdinalIgnoreCase))
                    return Math.Max(0, item.Value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static int? ResolveCompensationQuota(string? rawJson, Guid employeeId)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(rawJson);
            if (map is null)
                return null;

            var employeeIdText = employeeId.ToString();
            foreach (var item in map)
            {
                if (item.Key.Equals(employeeIdText, StringComparison.OrdinalIgnoreCase))
                    return Math.Max(0, item.Value);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
