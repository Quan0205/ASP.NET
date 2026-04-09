namespace CoffeeHRM.Dtos;

public sealed record LeaveRequestEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record LeaveRequestReviewDto(
    int Id,
    string Username);

public sealed record LeaveRequestResponseDto(
    int Id,
    int EmployeeId,
    DateTime StartDate,
    DateTime EndDate,
    string LeaveType,
    string? Reason,
    decimal TotalDays,
    int Status,
    int? ReviewedByUserAccountId,
    DateTime? ReviewedAt,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LeaveRequestEmployeeDto? Employee,
    LeaveRequestReviewDto? ReviewedByUserAccount);

public sealed record LeaveRequestUpsertDto(
    int EmployeeId,
    DateTime StartDate,
    DateTime EndDate,
    string LeaveType,
    string? Reason);

public sealed record DecisionNoteDto(string? Note);

public sealed record ShiftSwapEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record ShiftSwapScheduleDto(
    int Id,
    DateTime ScheduleDate,
    int ShiftId,
    string? ShiftCode,
    string? ShiftName);

public sealed record ShiftSwapResponseDto(
    int Id,
    int RequestEmployeeId,
    int TargetEmployeeId,
    int RequestScheduleId,
    int TargetScheduleId,
    string? Reason,
    int Status,
    int? ReviewedByUserAccountId,
    DateTime? ReviewedAt,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ShiftSwapEmployeeDto? RequestEmployee,
    ShiftSwapEmployeeDto? TargetEmployee,
    ShiftSwapScheduleDto? RequestSchedule,
    ShiftSwapScheduleDto? TargetSchedule);

public sealed record ShiftSwapUpsertDto(
    int RequestEmployeeId,
    int TargetEmployeeId,
    int RequestScheduleId,
    int TargetScheduleId,
    string? Reason);

public sealed record AttendanceAdjustmentAttendanceDto(
    int Id,
    DateTime AttendanceDate,
    int Status,
    DateTime? CheckInAt,
    DateTime? CheckOutAt);

public sealed record AttendanceAdjustmentResponseDto(
    int Id,
    int AttendanceId,
    int EmployeeId,
    DateTime? RequestedCheckInAt,
    DateTime? RequestedCheckOutAt,
    int? RequestedStatus,
    string? Reason,
    int Status,
    int? ReviewedByUserAccountId,
    DateTime? ReviewedAt,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    LeaveRequestEmployeeDto? Employee,
    AttendanceAdjustmentAttendanceDto? Attendance,
    LeaveRequestReviewDto? ReviewedByUserAccount);

public sealed record AttendanceAdjustmentUpsertDto(
    int AttendanceId,
    int EmployeeId,
    DateTime? RequestedCheckInAt,
    DateTime? RequestedCheckOutAt,
    int? RequestedStatus,
    string? Reason);

public sealed record ReportSummaryDto(
    int Month,
    int Year,
    int? BranchId,
    int ActiveEmployees,
    int AttendanceCount,
    int LateCount,
    int AbsentCount,
    int OvertimeMinutes,
    int PayrollCount,
    decimal PayrollTotal,
    int ApprovedPayrollCount,
    int PaidPayrollCount,
    int OpenRecruitments,
    int PendingLeaveRequests,
    int PendingShiftSwaps,
    int PendingAdjustments);
