using System.Collections.Concurrent;
using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public sealed class InMemoryHrAttendanceService : IHrAttendanceService
{
    private static readonly ConcurrentDictionary<Guid, List<AttendanceRecordDto>> AttendanceByBusiness = new();

    public Task<IReadOnlyList<AttendanceRecordDto>> GetByBusinessAsync(
        Guid businessId,
        DateOnly? workDate,
        Guid? employeeId,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var rows = AttendanceByBusiness.GetValueOrDefault(businessId) ?? [];
        IEnumerable<AttendanceRecordDto> query = rows;

        if (workDate is not null)
            query = query.Where(x => x.WorkDate == workDate.Value);
        if (employeeId is not null && employeeId != Guid.Empty)
            query = query.Where(x => x.EmployeeId == employeeId.Value);

        var result = query
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.CheckInAtUtc)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<AttendanceRecordDto>>(result);
    }

    public Task<AttendanceRecordDto> CheckInAsync(
        Guid businessId,
        CheckInAttendanceRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (request.EmployeeId == Guid.Empty)
            throw new ArgumentException("EmployeeId is required.", nameof(request));

        var workDate = request.WorkDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var list = AttendanceByBusiness.GetOrAdd(businessId, _ => []);

        lock (list)
        {
            var hasOpenRecord = list.Any(x =>
                x.EmployeeId == request.EmployeeId
                && x.WorkDate == workDate
                && x.CheckOutAtUtc is null);

            if (hasOpenRecord)
                throw new InvalidOperationException("Employee already has an open attendance record for this date.");

            var created = new AttendanceRecordDto(
                Id: Guid.NewGuid(),
                BusinessId: businessId,
                EmployeeId: request.EmployeeId,
                EmployeeName: $"Employee {request.EmployeeId.ToString()[..8]}",
                WorkDate: workDate,
                CheckInAtUtc: DateTime.UtcNow,
                CheckOutAtUtc: null,
                Status: "Present",
                Note: string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                LocationTag: string.IsNullOrWhiteSpace(request.LocationTag) ? null : request.LocationTag.Trim(),
                ReviewerComment: null);

            list.Add(created);
            return Task.FromResult(created);
        }
    }

    public Task<AttendanceRecordDto> CheckOutAsync(
        Guid businessId,
        Guid attendanceId,
        CheckOutAttendanceRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var list = AttendanceByBusiness.GetValueOrDefault(businessId)
            ?? throw new KeyNotFoundException("Attendance record not found.");

        lock (list)
        {
            var idx = list.FindIndex(x => x.Id == attendanceId);
            if (idx < 0)
                throw new KeyNotFoundException("Attendance record not found.");

            var current = list[idx];
            if (current.CheckOutAtUtc is not null)
                throw new InvalidOperationException("Attendance record is already checked out.");

            var mergedNote = current.Note;
            if (!string.IsNullOrWhiteSpace(request.Note))
                mergedNote = string.IsNullOrWhiteSpace(current.Note)
                    ? request.Note.Trim()
                    : $"{current.Note} | {request.Note.Trim()}";

            var updated = current with
            {
                CheckOutAtUtc = DateTime.UtcNow,
                Note = mergedNote,
                ReviewerComment = string.IsNullOrWhiteSpace(request.ReviewerComment)
                    ? current.ReviewerComment
                    : request.ReviewerComment.Trim(),
            };

            list[idx] = updated;
            return Task.FromResult(updated);
        }
    }
}
