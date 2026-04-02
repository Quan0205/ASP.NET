using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SchedulesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Schedule>>> GetSchedules()
    {
        return await _context.Schedules
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Schedule>> GetSchedule(int id)
    {
        var schedule = await _context.Schedules
            .Include(x => x.Employee)
            .Include(x => x.Shift)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (schedule == null)
        {
            return NotFound();
        }

        return schedule;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ScheduleValidationResult>> ValidateSchedule([FromBody] ScheduleRequest request)
    {
        var result = await ValidateScheduleInternalAsync(request);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Schedule>> PostSchedule([FromBody] ScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await ValidateScheduleInternalAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            return Conflict(result);
        }

        var schedule = new Schedule
        {
            EmployeeId = request.EmployeeId,
            ShiftId = request.ShiftId,
            ScheduleDate = request.ScheduleDate.Date,
            Note = request.Note
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.Id }, schedule);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutSchedule(int id, [FromBody] ScheduleRequest request, CancellationToken cancellationToken)
    {
        var existing = await _context.Schedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var validation = await ValidateScheduleInternalAsync(request, cancellationToken, id);
        if (!validation.IsValid)
        {
            return Conflict(validation);
        }

        existing.EmployeeId = request.EmployeeId;
        existing.ShiftId = request.ShiftId;
        existing.ScheduleDate = request.ScheduleDate.Date;
        existing.Note = request.Note;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        var schedule = await _context.Schedules.FindAsync([id], cancellationToken);
        if (schedule == null)
        {
            return NotFound();
        }

        _context.Schedules.Remove(schedule);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return NoContent();
    }

    private async Task<ScheduleValidationResult> ValidateScheduleInternalAsync(ScheduleRequest request, CancellationToken cancellationToken = default, int? scheduleId = null)
    {
        if (request.EmployeeId <= 0 || request.ShiftId <= 0)
        {
            return new ScheduleValidationResult(false, "EmployeeId and ShiftId are required.");
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return new ScheduleValidationResult(false, "Employee not found.");
        }

        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == request.ShiftId, cancellationToken);
        if (shift == null)
        {
            return new ScheduleValidationResult(false, "Shift not found.");
        }

        var date = request.ScheduleDate.Date;
        var hasDuplicate = await _context.Schedules.AnyAsync(x =>
            x.Id != scheduleId &&
            x.EmployeeId == request.EmployeeId &&
            x.ScheduleDate == date, cancellationToken);

        if (hasDuplicate)
        {
            return new ScheduleValidationResult(false, "Employee already has a schedule on this date.");
        }

        return new ScheduleValidationResult(true, "Schedule is valid.");
    }
}

public sealed record ScheduleRequest(int EmployeeId, int ShiftId, DateTime ScheduleDate, string? Note);
public sealed record ScheduleValidationResult(bool IsValid, string Message);
