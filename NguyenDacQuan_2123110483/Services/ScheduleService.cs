using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ScheduleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ScheduleValidationResultDto> ValidateAsync(ScheduleRequestDto request, CancellationToken cancellationToken = default, int? scheduleId = null);
    Task<(ScheduleResponseDto? Schedule, string? Error, int? StatusCode)> CreateAsync(ScheduleRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, ScheduleRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ScheduleService : IScheduleService
{
    private readonly AppDbContext _context;

    public ScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ScheduleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QuerySchedules().AsNoTracking().OrderByDescending(x => x.ScheduleDate).ToListAsync(cancellationToken);
        return rows.Select(MapSchedule).ToList();
    }

    public async Task<ScheduleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySchedules().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapSchedule(row);
    }

    public async Task<ScheduleValidationResultDto> ValidateAsync(ScheduleRequestDto request, CancellationToken cancellationToken = default, int? scheduleId = null)
    {
        if (request.EmployeeId <= 0 || request.ShiftId <= 0)
        {
            return new ScheduleValidationResultDto(false, "EmployeeId and ShiftId are required.");
        }

        if (!await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId && x.IsActive, cancellationToken))
        {
            return new ScheduleValidationResultDto(false, "Active employee not found.");
        }

        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == request.ShiftId, cancellationToken);
        if (shift == null || !shift.IsActive)
        {
            return new ScheduleValidationResultDto(false, "Active shift not found.");
        }

        var date = request.ScheduleDate.Date;
        var hasDuplicate = await _context.Schedules.AnyAsync(
            x => x.Id != scheduleId && x.EmployeeId == request.EmployeeId && x.ScheduleDate == date,
            cancellationToken);
        if (hasDuplicate)
        {
            return new ScheduleValidationResultDto(false, "Employee already has a schedule on this date.");
        }

        return new ScheduleValidationResultDto(true, "Schedule is valid.");
    }

    public async Task<(ScheduleResponseDto? Schedule, string? Error, int? StatusCode)> CreateAsync(ScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return (null, validation.Message, StatusCodes.Status409Conflict);
        }

        var entity = new Schedule
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            ScheduleDate = request.ScheduleDate.Date,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
        };

        _context.Schedules.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, ScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Schedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Schedule not found.", StatusCodes.Status404NotFound);
        }

        var validation = await ValidateAsync(request, cancellationToken, id);
        if (!validation.IsValid)
        {
            return (false, validation.Message, StatusCodes.Status409Conflict);
        }

        entity.EmployeeId = request.EmployeeId;
        entity.ShiftId = request.ShiftId;
        entity.ScheduleDate = request.ScheduleDate.Date;
        entity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Schedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Schedule not found.", StatusCodes.Status404NotFound);
        }

        _context.Schedules.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private IQueryable<Schedule> QuerySchedules()
    {
        return _context.Schedules
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .Include(x => x.Attendance);
    }

    private static ScheduleResponseDto MapSchedule(Schedule x)
    {
        return new ScheduleResponseDto(
            x.Id,
            x.EmployeeId,
            x.ShiftId,
            x.ScheduleDate,
            x.Note,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null ? null : new ScheduleEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.BranchId, x.Employee.IsActive),
            x.Shift == null ? null : new ScheduleShiftDto(x.Shift.Id, x.Shift.ShiftCode, x.Shift.ShiftName, x.Shift.StartTime, x.Shift.EndTime, x.Shift.GraceMinutes, x.Shift.IsActive),
            x.Attendance == null ? null : new ScheduleAttendanceDto(x.Attendance.Id, (int)x.Attendance.Status, x.Attendance.CheckInAt, x.Attendance.CheckOutAt));
    }
}
