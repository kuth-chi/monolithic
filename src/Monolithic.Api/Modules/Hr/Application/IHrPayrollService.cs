using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public interface IHrPayrollService
{
    Task<IReadOnlyList<PayrollRunDto>> GetByBusinessAsync(
        Guid businessId,
        string? status,
        CancellationToken ct = default);

    Task<PayrollRunDto> CreateAsync(
        Guid businessId,
        CreatePayrollRunRequest request,
        CancellationToken ct = default);

    Task<PayrollRunDto> SubmitAsync(
        Guid businessId,
        Guid payrollRunId,
        CancellationToken ct = default);

    Task<PayrollRunDto> ApproveAsync(
        Guid businessId,
        Guid payrollRunId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default);

    Task<PayrollRunDto> FinalizeAsync(
        Guid businessId,
        Guid payrollRunId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default);
}
