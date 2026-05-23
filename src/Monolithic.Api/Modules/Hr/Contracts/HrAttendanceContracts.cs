namespace Monolithic.Api.Modules.Hr.Contracts;

public sealed record AttendanceRecordDto(
    Guid Id,
    Guid BusinessId,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly WorkDate,
    DateTime CheckInAtUtc,
    DateTime? CheckOutAtUtc,
    string Status,
    string? Note,
    string? LocationTag,
    string? ReviewerComment);

public sealed record CheckInAttendanceRequest(
    Guid EmployeeId,
    DateOnly? WorkDate,
    string? Note,
    string? LocationTag);

public sealed record CheckOutAttendanceRequest(
    string? Note,
    string? ReviewerComment);
