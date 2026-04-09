namespace CoffeeHRM.Dtos;

public sealed record AttendanceEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    int BranchId,
    bool IsActive);

public sealed record AttendanceShiftDto(
    int Id,
    string ShiftCode,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    bool IsActive);

public sealed record AttendanceScheduleDto(
    int Id,
    DateTime ScheduleDate,
    string? Note);

public sealed record AttendanceResponseDto(
    int Id,
    int EmployeeId,
    int? ShiftId,
    int? ScheduleId,
    DateTime AttendanceDate,
    DateTime? CheckInAt,
    DateTime? CheckOutAt,
    int LateMinutes,
    int WorkingMinutes,
    int OvertimeMinutes,
    int EarlyLeaveMinutes,
    int Status,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AttendanceEmployeeDto? Employee,
    AttendanceShiftDto? Shift,
    AttendanceScheduleDto? Schedule);

public sealed record AttendanceSummaryDto(
    int Month,
    int Year,
    int? EmployeeId,
    int PresentCount,
    int LateCount,
    int EarlyLeaveCount,
    int OvertimeCount,
    int AbsentCount,
    int WorkingMinutes,
    IReadOnlyList<AttendanceResponseDto> Records);

public sealed record CheckInRequestDto(int EmployeeId, DateTime? AttendanceDate, string? Note);
public sealed record CheckOutRequestDto(int EmployeeId, DateTime? AttendanceDate);
public sealed record MarkAbsentRequestDto(DateTime AttendanceDate, string? Note);
public sealed record MarkAbsentResultDto(int Created, DateTime Date);
