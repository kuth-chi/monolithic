namespace Monolithic.Api.Modules.Hr.Contracts;

public sealed record LeaveRequestDto(
    Guid Id,
    Guid BusinessId,
    Guid EmployeeId,
    string EmployeeName,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    string Reason,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    string? ReviewerComment);

public sealed record CreateLeaveRequest(
    Guid EmployeeId,
    string LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);

public sealed record ReviewLeaveRequest(
    string? Comment);
