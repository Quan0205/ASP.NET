using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AttendanceController : ControllerBase
{
    private readonly AppDbContext _context;

    public AttendanceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Attendance>>> GetAttendances()
    {
        return await _context.Attendances
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .Include(x => x.Schedule)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<Attendance>>> GetByEmployee(int employeeId)
    {
        return await _context.Attendances
            .Where(x => x.EmployeeId == employeeId)
            .Include(x => x.Shift)
            .Include(x => x.Schedule)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<Attendance>> CheckIn(CheckInRequest request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0)
        {
            return BadRequest("EmployeeId is required.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var date = (request.AttendanceDate ?? DateTime.Now).Date;

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return NotFound("Employee not found.");
        }

        var schedule = await _context.Schedules
            .Include(x => x.Shift)
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.ScheduleDate == date, cancellationToken);

        if (schedule == null || schedule.Shift == null)
        {
            return BadRequest("Schedule not found for this employee/date.");
        }

        var attendance = await _context.Attendances
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date, cancellationToken);

        if (attendance != null && attendance.CheckInAt != null)
        {
            return Conflict("Employee already checked in for this date.");
        }

        attendance ??= new Attendance
        {
            EmployeeId = request.EmployeeId,
            AttendanceDate = date,
            ScheduleId = schedule.Id,
            ShiftId = schedule.ShiftId
        };

        if (attendance.CheckOutAt != null)
        {
            return Conflict("Attendance already closed.");
        }

        var now = DateTime.Now;
        attendance.CheckInAt = now;
        attendance.LateMinutes = CalculateLateMinutes(date, now, schedule.Shift);
        attendance.Status = attendance.LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
        attendance.Note = request.Note;

        if (_context.Entry(attendance).State == EntityState.Detached)
        {
            _context.Attendances.Add(attendance);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(attendance);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<Attendance>> CheckOut(CheckOutRequest request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0)
        {
            return BadRequest("EmployeeId is required.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var date = (request.AttendanceDate ?? DateTime.Now).Date;

        var attendance = await _context.Attendances
            .Include(x => x.Shift)
            .Include(x => x.Schedule)
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date, cancellationToken);

        if (attendance == null)
        {
            return NotFound("Attendance record not found.");
        }

        if (attendance.CheckInAt == null)
        {
            return BadRequest("Employee has not checked in.");
        }

        if (attendance.CheckOutAt != null)
        {
            return Conflict("Employee already checked out.");
        }

        if (attendance.Shift == null && attendance.Schedule?.Shift != null)
        {
            attendance.Shift = attendance.Schedule.Shift;
        }

        if (attendance.Shift == null)
        {
            return BadRequest("Shift information is missing.");
        }

        var now = DateTime.Now;
        if (now < attendance.CheckInAt.Value)
        {
            return BadRequest("Check-out time cannot be earlier than check-in time.");
        }

        attendance.CheckOutAt = now;
        attendance.WorkingMinutes = (int)Math.Max(0, (now - attendance.CheckInAt.Value).TotalMinutes);

        var scheduledStart = date.Add(attendance.Shift.StartTime);
        var scheduledEnd = date.Add(attendance.Shift.EndTime);
        if (scheduledEnd <= scheduledStart)
        {
            scheduledEnd = scheduledEnd.AddDays(1);
        }

        attendance.OvertimeMinutes = CalculateOvertimeMinutes(attendance.Shift, attendance.CheckInAt.Value, now);
        attendance.EarlyLeaveMinutes = Math.Max(0, (int)Math.Ceiling((scheduledEnd - now).TotalMinutes));
        attendance.Status = attendance.OvertimeMinutes > 0
            ? AttendanceStatus.Overtime
            : attendance.EarlyLeaveMinutes > 0
                ? AttendanceStatus.EarlyLeave
                : attendance.LateMinutes > 0
                    ? AttendanceStatus.Late
                    : AttendanceStatus.Present;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(attendance);
    }

    private static int CalculateLateMinutes(DateTime attendanceDate, DateTime checkInAt, Shift shift)
    {
        var allowedCheckIn = attendanceDate.Add(shift.StartTime).AddMinutes(shift.GraceMinutes);
        if (checkInAt <= allowedCheckIn)
        {
            return 0;
        }

        return (int)Math.Ceiling((checkInAt - allowedCheckIn).TotalMinutes);
    }

    private static int CalculateOvertimeMinutes(Shift shift, DateTime checkInAt, DateTime checkOutAt)
    {
        var shiftStart = checkInAt.Date.Add(shift.StartTime);
        var shiftEnd = checkInAt.Date.Add(shift.EndTime);
        if (shiftEnd <= shiftStart)
        {
            shiftEnd = shiftEnd.AddDays(1);
        }

        return Math.Max(0, (int)Math.Ceiling((checkOutAt - shiftEnd).TotalMinutes));
    }
}

public sealed record CheckInRequest(int EmployeeId, DateTime? AttendanceDate, string? Note);
public sealed record CheckOutRequest(int EmployeeId, DateTime? AttendanceDate);
