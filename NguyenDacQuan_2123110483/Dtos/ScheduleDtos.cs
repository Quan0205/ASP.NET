namespace CoffeeHRM.Dtos;

public sealed record ScheduleEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    int BranchId,
    bool IsActive);

public sealed record ScheduleShiftDto(
    int Id,
    string ShiftCode,
    string ShiftName,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int GraceMinutes,
    bool IsActive);

public sealed record ScheduleAttendanceDto(
    int Id,
    int Status,
    DateTime? CheckInAt,
    DateTime? CheckOutAt);

public sealed record ScheduleResponseDto(
    int Id,
    int EmployeeId,
    int ShiftId,
    DateTime ScheduleDate,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ScheduleEmployeeDto? Employee,
    ScheduleShiftDto? Shift,
    ScheduleAttendanceDto? Attendance);

public sealed record ScheduleRequestDto(
    int EmployeeId,
    int ShiftId,
    DateTime ScheduleDate,
    string? Note);

public sealed record ScheduleValidationResultDto(bool IsValid, string Message);
