using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IAttendanceService
{
    Task<IReadOnlyList<AttendanceResponseDto>> GetAllAsync(int? month, int? year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceResponseDto>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<AttendanceSummaryDto?> GetSummaryAsync(int month, int year, int? employeeId, CancellationToken cancellationToken = default);
    Task<(MarkAbsentResultDto? Result, string? Error, int? StatusCode)> MarkAbsentAsync(MarkAbsentRequestDto request, CancellationToken cancellationToken = default);
    Task<(AttendanceResponseDto? Attendance, string? Error, int? StatusCode)> CheckInAsync(CheckInRequestDto request, CancellationToken cancellationToken = default);
    Task<(AttendanceResponseDto? Attendance, string? Error, int? StatusCode)> CheckOutAsync(CheckOutRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;

    public AttendanceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AttendanceResponseDto>> GetAllAsync(int? month, int? year, CancellationToken cancellationToken = default)
    {
        var query = QueryAttendances().AsNoTracking();
        if (month is >= 1 and <= 12 && year is >= 2000)
        {
            var start = new DateTime(year.Value, month.Value, 1);
            var end = start.AddMonths(1);
            query = query.Where(x => x.AttendanceDate >= start && x.AttendanceDate < end);
        }

        var rows = await query.OrderByDescending(x => x.AttendanceDate).ThenBy(x => x.EmployeeId).ToListAsync(cancellationToken);
        return rows.Select(MapAttendance).ToList();
    }

    public async Task<IReadOnlyList<AttendanceResponseDto>> GetByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAttendances()
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId)
            .OrderByDescending(x => x.AttendanceDate)
            .ToListAsync(cancellationToken);
        return rows.Select(MapAttendance).ToList();
    }

    public async Task<AttendanceSummaryDto?> GetSummaryAsync(int month, int year, int? employeeId, CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12 || year < 2000)
        {
            return null;
        }

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var query = QueryAttendances().AsNoTracking().Where(x => x.AttendanceDate >= start && x.AttendanceDate < end);
        if (employeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == employeeId.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        return new AttendanceSummaryDto(
            month,
            year,
            employeeId,
            items.Count(x => x.Status == AttendanceStatus.Present),
            items.Count(x => x.LateMinutes > 0),
            items.Count(x => x.EarlyLeaveMinutes > 0),
            items.Count(x => x.OvertimeMinutes > 0),
            items.Count(x => x.Status == AttendanceStatus.Absent),
            items.Sum(x => x.WorkingMinutes),
            items.Select(MapAttendance).ToList());
    }

    public async Task<(MarkAbsentResultDto? Result, string? Error, int? StatusCode)> MarkAbsentAsync(MarkAbsentRequestDto request, CancellationToken cancellationToken = default)
    {
        var targetDate = request.AttendanceDate.Date;
        var schedules = await _context.Schedules
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .Where(x => x.ScheduleDate == targetDate)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var schedule in schedules)
        {
            if (schedule.Employee == null || !schedule.Employee.IsActive || schedule.Shift == null || !schedule.Shift.IsActive)
            {
                continue;
            }

            var attendance = await _context.Attendances.FirstOrDefaultAsync(x => x.EmployeeId == schedule.EmployeeId && x.AttendanceDate == targetDate, cancellationToken);
            if (attendance != null)
            {
                continue;
            }

            _context.Attendances.Add(new Attendance
            {
                EmployeeId = schedule.EmployeeId,
                ShiftId = schedule.ShiftId,
                ScheduleId = schedule.Id,
                AttendanceDate = targetDate,
                Status = AttendanceStatus.Absent,
                Note = request.Note ?? "Auto-marked absent"
            });
            created += 1;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return (new MarkAbsentResultDto(created, targetDate), null, null);
    }

    public async Task<(AttendanceResponseDto? Attendance, string? Error, int? StatusCode)> CheckInAsync(CheckInRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId <= 0)
        {
            return (null, "EmployeeId is required.", StatusCodes.Status400BadRequest);
        }

        var date = (request.AttendanceDate ?? DateTime.Now).Date;

        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee == null || !employee.IsActive)
        {
            return (null, "Active employee not found.", StatusCodes.Status404NotFound);
        }

        var schedule = await _context.Schedules.Include(x => x.Shift).FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.ScheduleDate == date, cancellationToken);
        if (schedule == null || schedule.Shift == null || !schedule.Shift.IsActive)
        {
            return (null, "Active schedule not found for this employee/date.", StatusCodes.Status400BadRequest);
        }

        var now = request.AttendanceDate ?? DateTime.Now;
        var earliestCheckIn = date.Add(schedule.Shift.StartTime).AddMinutes(-60);
        if (now < earliestCheckIn)
        {
            return (null, "Shift has not started yet.", StatusCodes.Status400BadRequest);
        }

        var attendance = await _context.Attendances.FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date, cancellationToken);
        if (attendance != null && attendance.CheckInAt != null)
        {
            return (null, "Employee already checked in for this date.", StatusCodes.Status409Conflict);
        }

        attendance ??= new Attendance
        {
            EmployeeId = request.EmployeeId,
            AttendanceDate = date,
            ScheduleId = schedule.Id,
            ShiftId = schedule.ShiftId
        };

        attendance.CheckInAt = now;
        attendance.LateMinutes = CalculateLateMinutes(date, now, schedule.Shift);
        attendance.Status = attendance.LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
        attendance.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        if (_context.Entry(attendance).State == EntityState.Detached)
        {
            _context.Attendances.Add(attendance);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var reloaded = await QueryAttendances().AsNoTracking().FirstAsync(x => x.Id == attendance.Id, cancellationToken);
        return (MapAttendance(reloaded), null, null);
    }

    public async Task<(AttendanceResponseDto? Attendance, string? Error, int? StatusCode)> CheckOutAsync(CheckOutRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId <= 0)
        {
            return (null, "EmployeeId is required.", StatusCodes.Status400BadRequest);
        }

        var date = (request.AttendanceDate ?? DateTime.Now).Date;

        var attendance = await _context.Attendances
            .Include(x => x.Shift)
            .Include(x => x.Schedule)
            .ThenInclude(x => x!.Shift)
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date, cancellationToken);
        if (attendance == null)
        {
            return (null, "Attendance record not found.", StatusCodes.Status404NotFound);
        }

        if (attendance.CheckInAt == null)
        {
            return (null, "Employee has not checked in.", StatusCodes.Status400BadRequest);
        }

        if (attendance.CheckOutAt != null)
        {
            return (null, "Employee already checked out.", StatusCodes.Status409Conflict);
        }

        attendance.Shift ??= attendance.Schedule?.Shift;
        if (attendance.Shift == null)
        {
            return (null, "Shift information is missing.", StatusCodes.Status400BadRequest);
        }

        var now = request.AttendanceDate ?? DateTime.Now;
        if (now < attendance.CheckInAt.Value)
        {
            return (null, "Check-out time cannot be earlier than check-in time.", StatusCodes.Status400BadRequest);
        }

        attendance.CheckOutAt = now;
        attendance.WorkingMinutes = (int)Math.Max(0, (now - attendance.CheckInAt.Value).TotalMinutes);

        var scheduledStart = date.Add(attendance.Shift.StartTime);
        var scheduledEnd = date.Add(attendance.Shift.EndTime);
        if (scheduledEnd <= scheduledStart)
        {
            scheduledEnd = scheduledEnd.AddDays(1);
        }

        attendance.OvertimeMinutes = Math.Max(0, (int)Math.Ceiling((now - scheduledEnd).TotalMinutes));
        attendance.EarlyLeaveMinutes = Math.Max(0, (int)Math.Ceiling((scheduledEnd - now).TotalMinutes));
        attendance.Status = attendance.OvertimeMinutes > 0
            ? AttendanceStatus.Overtime
            : attendance.EarlyLeaveMinutes > 0
                ? AttendanceStatus.EarlyLeave
                : attendance.LateMinutes > 0
                    ? AttendanceStatus.Late
                    : AttendanceStatus.Present;

        await _context.SaveChangesAsync(cancellationToken);

        var reloaded = await QueryAttendances().AsNoTracking().FirstAsync(x => x.Id == attendance.Id, cancellationToken);
        return (MapAttendance(reloaded), null, null);
    }

    private IQueryable<Attendance> QueryAttendances()
    {
        return _context.Attendances
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .Include(x => x.Schedule);
    }

    private static AttendanceResponseDto MapAttendance(Attendance x)
    {
        return new AttendanceResponseDto(
            x.Id,
            x.EmployeeId,
            x.ShiftId,
            x.ScheduleId,
            x.AttendanceDate,
            x.CheckInAt,
            x.CheckOutAt,
            x.LateMinutes,
            x.WorkingMinutes,
            x.OvertimeMinutes,
            x.EarlyLeaveMinutes,
            (int)x.Status,
            x.Note,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null ? null : new AttendanceEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.BranchId, x.Employee.IsActive),
            x.Shift == null ? null : new AttendanceShiftDto(x.Shift.Id, x.Shift.ShiftCode, x.Shift.ShiftName, x.Shift.StartTime, x.Shift.EndTime, x.Shift.GraceMinutes, x.Shift.IsActive),
            x.Schedule == null ? null : new AttendanceScheduleDto(x.Schedule.Id, x.Schedule.ScheduleDate, x.Schedule.Note));
    }

    private static int CalculateLateMinutes(DateTime attendanceDate, DateTime checkInAt, Shift shift)
    {
        var allowedCheckIn = attendanceDate.Add(shift.StartTime).AddMinutes(shift.GraceMinutes);
        return checkInAt <= allowedCheckIn ? 0 : (int)Math.Ceiling((checkInAt - allowedCheckIn).TotalMinutes);
    }
}
