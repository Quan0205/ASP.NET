using System.Text;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface ILeaveRequestService
{
    Task<IReadOnlyList<LeaveRequestResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(LeaveRequestResponseDto? LeaveRequest, string? Error, int? StatusCode)> CreateAsync(LeaveRequestUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, LeaveRequestUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public interface IShiftSwapRequestService
{
    Task<IReadOnlyList<ShiftSwapResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(ShiftSwapResponseDto? ShiftSwap, string? Error, int? StatusCode)> CreateAsync(ShiftSwapUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAttendanceAdjustmentService
{
    Task<IReadOnlyList<AttendanceAdjustmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(AttendanceAdjustmentResponseDto? Adjustment, string? Error, int? StatusCode)> CreateAsync(AttendanceAdjustmentUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<ReportSummaryDto> GetSummaryAsync(int? month, int? year, int? branchId, CancellationToken cancellationToken = default);
    Task<string> ExportEmployeesCsvAsync(CancellationToken cancellationToken = default);
    Task<string> ExportAttendanceCsvAsync(int? month, int? year, CancellationToken cancellationToken = default);
}

public sealed class LeaveRequestService : ILeaveRequestService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public LeaveRequestService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<LeaveRequestResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.LeaveRequests.Include(x => x.Employee).Include(x => x.ReviewedByUserAccount).AsNoTracking().OrderByDescending(x => x.StartDate).ToListAsync(cancellationToken);
        return rows.Select(MapLeaveRequest).ToList();
    }

    public async Task<(LeaveRequestResponseDto? LeaveRequest, string? Error, int? StatusCode)> CreateAsync(LeaveRequestUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);
        var entity = new LeaveRequest
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            LeaveType = dto.LeaveType.Trim(),
            Reason = dto.Reason?.Trim(),
            TotalDays = (decimal)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1
        };
        _context.LeaveRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        var reloaded = await _context.LeaveRequests.Include(x => x.Employee).Include(x => x.ReviewedByUserAccount).AsNoTracking().FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return (MapLeaveRequest(reloaded), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, LeaveRequestUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Leave request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != LeaveRequestStatus.Pending) return (false, "Only pending leave request can be updated.", StatusCodes.Status409Conflict);
        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);
        entity.EmployeeId = dto.EmployeeId;
        entity.StartDate = dto.StartDate.Date;
        entity.EndDate = dto.EndDate.Date;
        entity.LeaveType = dto.LeaveType.Trim();
        entity.Reason = dto.Reason?.Trim();
        entity.TotalDays = (decimal)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
        => await DecideAsync(id, LeaveRequestStatus.Approved, dto.Note, cancellationToken);
    public async Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
        => await DecideAsync(id, LeaveRequestStatus.Rejected, dto.Note, cancellationToken);

    public async Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Leave request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != LeaveRequestStatus.Pending) return (false, "Only pending leave request can be cancelled.", StatusCodes.Status409Conflict);
        entity.Status = LeaveRequestStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(LeaveRequestUpsertDto dto, int? requestId, CancellationToken cancellationToken)
    {
        if (dto.EmployeeId <= 0 || string.IsNullOrWhiteSpace(dto.LeaveType)) return ("EmployeeId and LeaveType are required.", StatusCodes.Status400BadRequest);
        if (dto.EndDate.Date < dto.StartDate.Date) return ("EndDate must be greater than or equal to StartDate.", StatusCodes.Status400BadRequest);
        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId && x.IsActive, cancellationToken)) return ("Active employee not found.", StatusCodes.Status400BadRequest);
        var overlap = await _context.LeaveRequests.AnyAsync(x => x.Id != requestId && x.EmployeeId == dto.EmployeeId && x.Status != LeaveRequestStatus.Cancelled && x.Status != LeaveRequestStatus.Rejected && x.StartDate <= dto.EndDate.Date && x.EndDate >= dto.StartDate.Date, cancellationToken);
        return overlap ? ("Employee already has a leave request in this period.", StatusCodes.Status409Conflict) : null;
    }

    private async Task<(bool Success, string? Error, int? StatusCode)> DecideAsync(int id, LeaveRequestStatus status, string? note, CancellationToken cancellationToken)
    {
        var entity = await _context.LeaveRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Leave request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != LeaveRequestStatus.Pending) return (false, $"Only pending leave request can be {(status == LeaveRequestStatus.Approved ? "approved" : "rejected")}.", StatusCodes.Status409Conflict);
        entity.Status = status;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedByUserAccountId = _currentUserService.UserId;
        entity.DecisionNote = note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private static LeaveRequestResponseDto MapLeaveRequest(LeaveRequest x)
    {
        return new LeaveRequestResponseDto(x.Id, x.EmployeeId, x.StartDate, x.EndDate, x.LeaveType, x.Reason, x.TotalDays, (int)x.Status, x.ReviewedByUserAccountId, x.ReviewedAt, x.DecisionNote, x.CreatedAt, x.UpdatedAt,
            x.Employee == null ? null : new LeaveRequestEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive),
            x.ReviewedByUserAccount == null ? null : new LeaveRequestReviewDto(x.ReviewedByUserAccount.Id, x.ReviewedByUserAccount.Username));
    }
}

public sealed class ShiftSwapRequestService : IShiftSwapRequestService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ShiftSwapRequestService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ShiftSwapResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.ShiftSwapRequests
            .Include(x => x.RequestEmployee)
            .Include(x => x.TargetEmployee)
            .Include(x => x.RequestSchedule).ThenInclude(x => x!.Shift)
            .Include(x => x.TargetSchedule).ThenInclude(x => x!.Shift)
            .AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(MapShiftSwap).ToList();
    }

    public async Task<(ShiftSwapResponseDto? ShiftSwap, string? Error, int? StatusCode)> CreateAsync(ShiftSwapUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);
        var entity = new ShiftSwapRequest { RequestEmployeeId = dto.RequestEmployeeId, TargetEmployeeId = dto.TargetEmployeeId, RequestScheduleId = dto.RequestScheduleId, TargetScheduleId = dto.TargetScheduleId, Reason = dto.Reason?.Trim() };
        _context.ShiftSwapRequests.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        var reloaded = await _context.ShiftSwapRequests
            .Include(x => x.RequestEmployee)
            .Include(x => x.TargetEmployee)
            .Include(x => x.RequestSchedule).ThenInclude(x => x!.Shift)
            .Include(x => x.TargetSchedule).ThenInclude(x => x!.Shift)
            .AsNoTracking().FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return (MapShiftSwap(reloaded), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ShiftSwapRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Shift swap request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != ShiftSwapStatus.Pending) return (false, "Only pending shift swap can be approved.", StatusCodes.Status409Conflict);
        var requestSchedule = await _context.Schedules.Include(x => x.Attendance).FirstOrDefaultAsync(x => x.Id == entity.RequestScheduleId, cancellationToken);
        var targetSchedule = await _context.Schedules.Include(x => x.Attendance).FirstOrDefaultAsync(x => x.Id == entity.TargetScheduleId, cancellationToken);
        if (requestSchedule == null || targetSchedule == null) return (false, "Schedule not found.", StatusCodes.Status400BadRequest);
        if (requestSchedule.Attendance != null || targetSchedule.Attendance != null) return (false, "Cannot swap schedules that already have attendance.", StatusCodes.Status409Conflict);

        var requestEmployeeId = entity.RequestEmployeeId;
        var targetEmployeeId = entity.TargetEmployeeId;
        await _context.Database.ExecuteSqlRawAsync(
            """
            UPDATE [Schedules]
            SET [EmployeeId] = CASE
                WHEN [Id] = {0} THEN {1}
                WHEN [Id] = {2} THEN {3}
                ELSE [EmployeeId]
            END
            WHERE [Id] IN ({0}, {2})
            """,
            requestSchedule.Id,
            targetEmployeeId,
            targetSchedule.Id,
            requestEmployeeId);

        entity.Status = ShiftSwapStatus.Approved;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedByUserAccountId = _currentUserService.UserId;
        entity.DecisionNote = dto.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ShiftSwapRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Shift swap request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != ShiftSwapStatus.Pending) return (false, "Only pending shift swap can be rejected.", StatusCodes.Status409Conflict);
        entity.Status = ShiftSwapStatus.Rejected;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedByUserAccountId = _currentUserService.UserId;
        entity.DecisionNote = dto.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ShiftSwapRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Shift swap request not found.", StatusCodes.Status404NotFound);
        if (entity.Status != ShiftSwapStatus.Pending) return (false, "Only pending shift swap can be cancelled.", StatusCodes.Status409Conflict);
        entity.Status = ShiftSwapStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(ShiftSwapUpsertDto dto, CancellationToken cancellationToken)
    {
        if (dto.RequestEmployeeId <= 0 || dto.TargetEmployeeId <= 0 || dto.RequestScheduleId <= 0 || dto.TargetScheduleId <= 0) return ("Invalid shift swap request.", StatusCodes.Status400BadRequest);
        if (dto.RequestScheduleId == dto.TargetScheduleId) return ("RequestScheduleId and TargetScheduleId must be different.", StatusCodes.Status400BadRequest);
        var requestSchedule = await _context.Schedules.FirstOrDefaultAsync(x => x.Id == dto.RequestScheduleId, cancellationToken);
        var targetSchedule = await _context.Schedules.FirstOrDefaultAsync(x => x.Id == dto.TargetScheduleId, cancellationToken);
        if (requestSchedule == null || targetSchedule == null) return ("Schedule not found.", StatusCodes.Status400BadRequest);
        if (requestSchedule.EmployeeId != dto.RequestEmployeeId || targetSchedule.EmployeeId != dto.TargetEmployeeId) return ("Schedule does not belong to the selected employee.", StatusCodes.Status400BadRequest);
        if (requestSchedule.ScheduleDate != targetSchedule.ScheduleDate) return ("Only schedules on the same date can be swapped.", StatusCodes.Status400BadRequest);
        return null;
    }

    private static ShiftSwapResponseDto MapShiftSwap(ShiftSwapRequest x)
    {
        return new ShiftSwapResponseDto(
            x.Id, x.RequestEmployeeId, x.TargetEmployeeId, x.RequestScheduleId, x.TargetScheduleId, x.Reason, (int)x.Status, x.ReviewedByUserAccountId, x.ReviewedAt, x.DecisionNote, x.CreatedAt, x.UpdatedAt,
            x.RequestEmployee == null ? null : new ShiftSwapEmployeeDto(x.RequestEmployee.Id, x.RequestEmployee.EmployeeCode, x.RequestEmployee.FullName, x.RequestEmployee.IsActive),
            x.TargetEmployee == null ? null : new ShiftSwapEmployeeDto(x.TargetEmployee.Id, x.TargetEmployee.EmployeeCode, x.TargetEmployee.FullName, x.TargetEmployee.IsActive),
            x.RequestSchedule == null ? null : new ShiftSwapScheduleDto(x.RequestSchedule.Id, x.RequestSchedule.ScheduleDate, x.RequestSchedule.ShiftId, x.RequestSchedule.Shift?.ShiftCode, x.RequestSchedule.Shift?.ShiftName),
            x.TargetSchedule == null ? null : new ShiftSwapScheduleDto(x.TargetSchedule.Id, x.TargetSchedule.ScheduleDate, x.TargetSchedule.ShiftId, x.TargetSchedule.Shift?.ShiftCode, x.TargetSchedule.Shift?.ShiftName));
    }
}

public sealed class AttendanceAdjustmentService : IAttendanceAdjustmentService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceAdjustmentService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AttendanceAdjustmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.AttendanceAdjustments.Include(x => x.Employee).Include(x => x.Attendance).Include(x => x.ReviewedByUserAccount).AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(MapAdjustment).ToList();
    }

    public async Task<(AttendanceAdjustmentResponseDto? Adjustment, string? Error, int? StatusCode)> CreateAsync(AttendanceAdjustmentUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var attendance = await _context.Attendances.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == dto.AttendanceId, cancellationToken);
        if (attendance == null) return (null, "Attendance not found.", StatusCodes.Status404NotFound);
        if (attendance.EmployeeId != dto.EmployeeId) return (null, "Attendance does not belong to the selected employee.", StatusCodes.Status400BadRequest);
        if (dto.RequestedStatus.HasValue && !Enum.IsDefined(typeof(AttendanceStatus), dto.RequestedStatus.Value)) return (null, "Invalid attendance status.", StatusCodes.Status400BadRequest);

        var entity = new AttendanceAdjustment
        {
            AttendanceId = dto.AttendanceId,
            EmployeeId = dto.EmployeeId,
            RequestedCheckInAt = dto.RequestedCheckInAt,
            RequestedCheckOutAt = dto.RequestedCheckOutAt,
            RequestedStatus = dto.RequestedStatus.HasValue ? (AttendanceStatus)dto.RequestedStatus.Value : null,
            Reason = dto.Reason?.Trim()
        };
        _context.AttendanceAdjustments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        var reloaded = await _context.AttendanceAdjustments.Include(x => x.Employee).Include(x => x.Attendance).Include(x => x.ReviewedByUserAccount).AsNoTracking().FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return (MapAdjustment(reloaded), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AttendanceAdjustments.Include(x => x.Attendance).ThenInclude(x => x!.Shift).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Attendance adjustment not found.", StatusCodes.Status404NotFound);
        if (entity.Status != AttendanceAdjustmentStatus.Pending || entity.Attendance == null) return (false, "Only pending attendance adjustment can be approved.", StatusCodes.Status409Conflict);
        ApplyAdjustment(entity.Attendance, entity);
        entity.Status = AttendanceAdjustmentStatus.Approved;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedByUserAccountId = _currentUserService.UserId;
        entity.DecisionNote = dto.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> RejectAsync(int id, DecisionNoteDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AttendanceAdjustments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Attendance adjustment not found.", StatusCodes.Status404NotFound);
        if (entity.Status != AttendanceAdjustmentStatus.Pending) return (false, "Only pending attendance adjustment can be rejected.", StatusCodes.Status409Conflict);
        entity.Status = AttendanceAdjustmentStatus.Rejected;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewedByUserAccountId = _currentUserService.UserId;
        entity.DecisionNote = dto.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.AttendanceAdjustments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Attendance adjustment not found.", StatusCodes.Status404NotFound);
        if (entity.Status != AttendanceAdjustmentStatus.Pending) return (false, "Only pending attendance adjustment can be cancelled.", StatusCodes.Status409Conflict);
        entity.Status = AttendanceAdjustmentStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private static void ApplyAdjustment(Attendance attendance, AttendanceAdjustment adjustment)
    {
        attendance.CheckInAt = adjustment.RequestedCheckInAt ?? attendance.CheckInAt;
        attendance.CheckOutAt = adjustment.RequestedCheckOutAt ?? attendance.CheckOutAt;
        if (adjustment.RequestedStatus.HasValue) attendance.Status = adjustment.RequestedStatus.Value;
        if (attendance.CheckInAt.HasValue && attendance.CheckOutAt.HasValue)
            attendance.WorkingMinutes = (int)Math.Max(0, (attendance.CheckOutAt.Value - attendance.CheckInAt.Value).TotalMinutes);
    }

    private static AttendanceAdjustmentResponseDto MapAdjustment(AttendanceAdjustment x)
    {
        return new AttendanceAdjustmentResponseDto(
            x.Id, x.AttendanceId, x.EmployeeId, x.RequestedCheckInAt, x.RequestedCheckOutAt, x.RequestedStatus.HasValue ? (int)x.RequestedStatus.Value : null,
            x.Reason, (int)x.Status, x.ReviewedByUserAccountId, x.ReviewedAt, x.DecisionNote, x.CreatedAt, x.UpdatedAt,
            x.Employee == null ? null : new LeaveRequestEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive),
            x.Attendance == null ? null : new AttendanceAdjustmentAttendanceDto(x.Attendance.Id, x.Attendance.AttendanceDate, (int)x.Attendance.Status, x.Attendance.CheckInAt, x.Attendance.CheckOutAt),
            x.ReviewedByUserAccount == null ? null : new LeaveRequestReviewDto(x.ReviewedByUserAccount.Id, x.ReviewedByUserAccount.Username));
    }
}

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _context;

    public ReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReportSummaryDto> GetSummaryAsync(int? month, int? year, int? branchId, CancellationToken cancellationToken = default)
    {
        var targetMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month;
        var targetYear = year is >= 2000 ? year.Value : DateTime.Today.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1);

        var employeesQuery = _context.Employees.AsQueryable();
        if (branchId.HasValue) employeesQuery = employeesQuery.Where(x => x.BranchId == branchId.Value);
        var employeeIds = await employeesQuery.Select(x => x.Id).ToListAsync(cancellationToken);

        var attendance = await _context.Attendances.Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate >= startDate && x.AttendanceDate < endDate).ToListAsync(cancellationToken);
        var payroll = await _context.Payrolls.Where(x => employeeIds.Contains(x.EmployeeId) && x.PayrollMonth == targetMonth && x.PayrollYear == targetYear).ToListAsync(cancellationToken);
        var recruitments = await _context.Recruitments.Where(x => !branchId.HasValue || x.BranchId == branchId).ToListAsync(cancellationToken);

        return new ReportSummaryDto(
            targetMonth,
            targetYear,
            branchId,
            employeeIds.Count,
            attendance.Count,
            attendance.Count(x => x.LateMinutes > 0),
            attendance.Count(x => x.Status == AttendanceStatus.Absent),
            attendance.Sum(x => x.OvertimeMinutes),
            payroll.Count,
            payroll.Sum(x => x.TotalSalary),
            payroll.Count(x => x.Status == PayrollStatus.Approved || x.Status == PayrollStatus.Paid),
            payroll.Count(x => x.Status == PayrollStatus.Paid),
            recruitments.Count(x => x.Status == RecruitmentStatus.Open || x.Status == RecruitmentStatus.InProgress),
            await _context.LeaveRequests.CountAsync(x => x.Status == LeaveRequestStatus.Pending, cancellationToken),
            await _context.ShiftSwapRequests.CountAsync(x => x.Status == ShiftSwapStatus.Pending, cancellationToken),
            await _context.AttendanceAdjustments.CountAsync(x => x.Status == AttendanceAdjustmentStatus.Pending, cancellationToken));
    }

    public async Task<string> ExportEmployeesCsvAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees.Include(x => x.Branch).Include(x => x.Role).AsNoTracking().OrderBy(x => x.EmployeeCode).ToListAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("EmployeeCode,FullName,Branch,Role,Phone,Email,IsActive,HireDate");
        foreach (var employee in employees)
        {
            builder.AppendLine(string.Join(",", Csv(employee.EmployeeCode), Csv(employee.FullName), Csv(employee.Branch?.BranchName), Csv(employee.Role?.RoleName), Csv(employee.Phone), Csv(employee.Email), employee.IsActive, employee.HireDate.ToString("yyyy-MM-dd")));
        }
        return builder.ToString();
    }

    public async Task<string> ExportAttendanceCsvAsync(int? month, int? year, CancellationToken cancellationToken = default)
    {
        var targetMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month;
        var targetYear = year is >= 2000 ? year.Value : DateTime.Today.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1);
        var rows = await _context.Attendances.Include(x => x.Employee).AsNoTracking().Where(x => x.AttendanceDate >= startDate && x.AttendanceDate < endDate).OrderBy(x => x.AttendanceDate).ToListAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("AttendanceDate,EmployeeCode,EmployeeName,Status,LateMinutes,EarlyLeaveMinutes,OvertimeMinutes,WorkingMinutes");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.AttendanceDate.ToString("yyyy-MM-dd"), Csv(row.Employee?.EmployeeCode), Csv(row.Employee?.FullName), row.Status, row.LateMinutes, row.EarlyLeaveMinutes, row.OvertimeMinutes, row.WorkingMinutes));
        }
        return builder.ToString();
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
