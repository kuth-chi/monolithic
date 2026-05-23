using Monolithic.Api.Modules.Hr.Contracts;

namespace Monolithic.Api.Modules.Hr.Application;

public interface IHrAttendanceService
{
    Task<IReadOnlyList<AttendanceRecordDto>> GetByBusinessAsync(
        Guid businessId,
        DateOnly? workDate,
        Guid? employeeId,
        CancellationToken ct = default);

    Task<AttendanceRecordDto> CheckInAsync(
        Guid businessId,
        CheckInAttendanceRequest request,
        CancellationToken ct = default);

    Task<AttendanceRecordDto> CheckOutAsync(
        Guid businessId,
        Guid attendanceId,
        CheckOutAttendanceRequest request,
        CancellationToken ct = default);
}
