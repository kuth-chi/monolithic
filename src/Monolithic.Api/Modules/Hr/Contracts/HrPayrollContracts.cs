namespace Monolithic.Api.Modules.Hr.Contracts;

public sealed record PayrollRunDto(
    Guid Id,
    Guid BusinessId,
    string PayrollName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly PayDate,
    string Status,
    int EmployeeCount,
    decimal GrossAmount,
    decimal NetAmount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? FinalizedAtUtc,
    string? Notes,
    string? ReviewerComment);

public sealed record CreatePayrollRunRequest(
    string PayrollName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateOnly PayDate,
    string? Notes);

public sealed record ReviewPayrollRunRequest(
    string? Comment);
