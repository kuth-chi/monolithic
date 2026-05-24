using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Monolithic.Api.Controllers.V1;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Xunit;

namespace Monolithic.Api.Tests;

public sealed class HrLeaveRequestsEndpointContractTests
{
    [Fact]
    public async Task PostLeaveRequest_WhenServiceThrowsArgumentException_Returns400ProblemDetails()
    {
        await using var app = await BuildAppAsync(
            new StubLeaveService(
                (_, _, _) => throw new ArgumentException("EmployeeId is required.")));

        var client = app.GetTestClient();
        var businessId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId = Guid.Empty,
                leaveType = "Annual",
                startDate = "2026-05-24",
                endDate = "2026-05-24",
                reason = "Test",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Invalid leave request", problem!.Title);
        Assert.Equal("EmployeeId is required.", problem.Detail);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task PostLeaveRequest_WhenServiceThrowsInvalidOperation_Returns400ProblemDetails()
    {
        await using var app = await BuildAppAsync(
            new StubLeaveService(
                (_, _, _) => throw new InvalidOperationException(
                    "Compensation Leave leave quota exceeded. Remaining days: 0.")));

        var client = app.GetTestClient();
        var businessId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId = Guid.NewGuid(),
                leaveType = "Compensation Leave",
                startDate = "2026-05-24",
                endDate = "2026-05-24",
                reason = "Test",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Leave request validation failed", problem!.Title);
        Assert.Equal("Compensation Leave leave quota exceeded. Remaining days: 0.", problem.Detail);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task PostLeaveRequest_WhenServiceSucceeds_Returns200Payload()
    {
        var leaveId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        await using var app = await BuildAppAsync(
            new StubLeaveService(
                (_, request, _) => new LeaveRequestDto(
                    Id: leaveId,
                    BusinessId: businessId,
                    EmployeeId: request.EmployeeId,
                    EmployeeName: "Demo Employee",
                    LeaveType: request.LeaveType,
                    StartDate: request.StartDate,
                    EndDate: request.EndDate,
                    TotalDays: 1,
                    Reason: request.Reason,
                    Status: "Pending",
                    CreatedAtUtc: DateTime.UtcNow,
                    ReviewedAtUtc: null,
                    ReviewedByUserId: null,
                    ReviewerComment: null)));

        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId,
                leaveType = "Annual",
                startDate = "2026-05-24",
                endDate = "2026-05-24",
                reason = "Test",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.NotNull(created);
        Assert.Equal(leaveId, created!.Id);
        Assert.Equal("Pending", created.Status);
        Assert.Equal("Annual", created.LeaveType);
    }

    [Fact]
    public async Task PostLeaveRequest_WithRealService_EnforcesCompensationQuotaAcrossRequests()
    {
        var businessId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var dbName = $"endpoint-real-{Guid.NewGuid()}";

        await using var app = await BuildAppWithRealServiceAsync(dbName, businessId, employeeId);
        var client = app.GetTestClient();

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId,
                leaveType = "Compensation Leave",
                startDate = "2026-05-24",
                endDate = "2026-05-24",
                reason = "First",
            });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId,
                leaveType = "Compensation Leave",
                startDate = "2026-05-25",
                endDate = "2026-05-25",
                reason = "Second",
            });

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var problem = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Leave request validation failed", problem!.Title);
        Assert.Equal("Compensation Leave leave quota exceeded. Remaining days: 0.", problem.Detail);
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public async Task PostLeaveRequest_WithRealService_UnassignedCompensationLeaveReturns400()
    {
        var businessId = Guid.NewGuid();
        var assignedEmployeeId = Guid.NewGuid();
        var unassignedEmployeeId = Guid.NewGuid();
        var dbName = $"endpoint-real-unassigned-{Guid.NewGuid()}";

        await using var app = await BuildAppWithRealServiceAsync(dbName, businessId, assignedEmployeeId);
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/businesses/{businessId}/hr/leave-requests",
            new
            {
                employeeId = unassignedEmployeeId,
                leaveType = "Compensation Leave",
                startDate = "2026-05-24",
                endDate = "2026-05-24",
                reason = "Unassigned employee test",
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Leave request validation failed", problem!.Title);
        Assert.Equal("Compensation Leave leave quota exceeded. Remaining days: 0.", problem.Detail);
        Assert.Equal(400, problem.Status);
    }

    private static async Task<WebApplication> BuildAppAsync(IHrLeaveService leaveService)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(leaveService);
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(HrLeaveRequestsController).Assembly);

        var app = builder.Build();
        app.MapControllers();

        await app.StartAsync();
        return app;
    }

    private static async Task<WebApplication> BuildAppWithRealServiceAsync(
        string dbName,
        Guid businessId,
        Guid employeeId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        builder.Services.AddScoped<IHrLeaveService, InMemoryHrLeaveService>();
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(HrLeaveRequestsController).Assembly);

        var app = builder.Build();
        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Businesses.Add(new Business
            {
                Id = businessId,
                Name = "Contract Test Business",
            });
            db.BusinessSettings.Add(new BusinessSetting
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                CompensationLeaveByEmployeeJson = $"{{\"{employeeId}\":1}}",
            });
            await db.SaveChangesAsync();
        }

        await app.StartAsync();
        return app;
    }

    private sealed class StubLeaveService(
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
