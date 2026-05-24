using Microsoft.EntityFrameworkCore;
using Monolithic.Api.Modules.Business.Domain;
using Monolithic.Api.Modules.Hr.Application;
using Monolithic.Api.Modules.Hr.Contracts;
using Monolithic.Api.Modules.Identity.Infrastructure.Data;
using Xunit;

namespace Monolithic.Api.Tests;

public sealed class InMemoryHrLeaveServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"leave-tests-{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_UnassignedCompensationLeave_ThrowsQuotaExceeded()
    {
        await using var db = CreateDbContext();
        var businessId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        db.Businesses.Add(new Business
        {
            Id = businessId,
            Name = "Test Business",
        });

        db.BusinessSettings.Add(new BusinessSetting
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CompensationLeaveByEmployeeJson = "{}",
        });
        await db.SaveChangesAsync();

        var service = new InMemoryHrLeaveService(db);

        var request = new CreateLeaveRequest(
            EmployeeId: employeeId,
            LeaveType: "Compensation Leave",
            StartDate: new DateOnly(2026, 5, 24),
            EndDate: new DateOnly(2026, 5, 24),
            Reason: "Quota guard");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(businessId, request));

        Assert.Contains("leave quota exceeded", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_AssignedCompensationLeave_AllowsFirstThenBlocksSecond()
    {
        await using var db = CreateDbContext();
        var businessId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        db.Businesses.Add(new Business
        {
            Id = businessId,
            Name = "Test Business",
        });

        db.BusinessSettings.Add(new BusinessSetting
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId,
            CompensationLeaveByEmployeeJson = $"{{\"{employeeId}\":1}}",
        });
        await db.SaveChangesAsync();

        var service = new InMemoryHrLeaveService(db);

        var firstRequest = new CreateLeaveRequest(
            EmployeeId: employeeId,
            LeaveType: "Compensation Leave",
            StartDate: new DateOnly(2026, 5, 24),
            EndDate: new DateOnly(2026, 5, 24),
            Reason: "First day");

        var secondRequest = new CreateLeaveRequest(
            EmployeeId: employeeId,
            LeaveType: "Compensation Leave",
            StartDate: new DateOnly(2026, 5, 25),
            EndDate: new DateOnly(2026, 5, 25),
            Reason: "Second day");

        var first = await service.CreateAsync(businessId, firstRequest);
        Assert.Equal("Pending", first.Status);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(businessId, secondRequest));

        Assert.Contains("Remaining days: 0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
