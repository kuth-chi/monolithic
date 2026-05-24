using Microsoft.AspNetCore.Mvc;
using Monolithic.Api.Controllers.V1;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Xunit;

namespace Monolithic.Api.Tests;

public sealed class HrLeaveRequestsControllerTests
{
    [Fact]
    public async Task Create_WhenArgumentException_ReturnsBadRequestProblemDetails()
    {
        var controller = new HrLeaveRequestsController(
            new FakeLeaveService(
                onCreate: (_, _, _) => throw new ArgumentException("EmployeeId is required.")));

        var result = await controller.Create(
            Guid.NewGuid(),
            new CreateLeaveRequest(Guid.Empty, "Annual", new DateOnly(2026, 5, 24), new DateOnly(2026, 5, 24), "x"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Invalid leave request", problem.Title);
        Assert.Equal("EmployeeId is required.", problem.Detail);
    }

    [Fact]
    public async Task Create_WhenInvalidOperationException_ReturnsBadRequestProblemDetails()
    {
        var controller = new HrLeaveRequestsController(
            new FakeLeaveService(
                onCreate: (_, _, _) => throw new InvalidOperationException("Compensation Leave leave quota exceeded. Remaining days: 0.")));

        var result = await controller.Create(
            Guid.NewGuid(),
            new CreateLeaveRequest(Guid.NewGuid(), "Compensation Leave", new DateOnly(2026, 5, 24), new DateOnly(2026, 5, 24), "x"),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, badRequest.StatusCode);
        Assert.Equal("Leave request validation failed", problem.Title);
        Assert.Equal("Compensation Leave leave quota exceeded. Remaining days: 0.", problem.Detail);
    }

    private sealed class FakeLeaveService(
        Func<Guid, CreateLeaveRequest, CancellationToken, LeaveRequestDto> onCreate) : IHrLeaveService
    {
        public Task<IReadOnlyList<LeaveRequestDto>> GetByBusinessAsync(
            Guid businessId,
            string? status,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<LeaveRequestDto>>([]);

        public Task<LeaveRequestDto> CreateAsync(
            Guid businessId,
            CreateLeaveRequest request,
            CancellationToken ct = default)
            => Task.FromResult(onCreate(businessId, request, ct));

        public Task<LeaveRequestDto> ApproveAsync(
            Guid businessId,
            Guid leaveRequestId,
            Guid reviewerUserId,
            string? comment,
            CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<LeaveRequestDto> RejectAsync(
            Guid businessId,
            Guid leaveRequestId,
            Guid reviewerUserId,
            string? comment,
            CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}
