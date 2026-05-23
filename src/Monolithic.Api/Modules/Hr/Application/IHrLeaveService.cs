using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public interface IHrLeaveService
{
    Task<IReadOnlyList<LeaveRequestDto>> GetByBusinessAsync(
        Guid businessId,
        string? status,
        CancellationToken ct = default);

    Task<LeaveRequestDto> CreateAsync(
        Guid businessId,
        CreateLeaveRequest request,
        CancellationToken ct = default);

    Task<LeaveRequestDto> ApproveAsync(
        Guid businessId,
        Guid leaveRequestId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default);

    Task<LeaveRequestDto> RejectAsync(
        Guid businessId,
        Guid leaveRequestId,
        Guid reviewerUserId,
        string? comment,
        CancellationToken ct = default);
}
