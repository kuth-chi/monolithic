using System.Collections.Concurrent;
using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public sealed class InMemoryHrLeaveService : IHrLeaveService
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

    public Task<LeaveRequestDto> CreateAsync(
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

        var totalDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;

        var created = new LeaveRequestDto(
            Id: Guid.NewGuid(),
            BusinessId: businessId,
            EmployeeId: request.EmployeeId,
            EmployeeName: $"Employee {request.EmployeeId.ToString()[..8]}",
            LeaveType: request.LeaveType.Trim(),
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
            list.Add(created);
        }

        return Task.FromResult(created);
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
}
