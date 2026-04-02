using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ShiftsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShiftsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Shift>>> GetShifts()
    {
        return await _context.Shifts
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Shift>> GetShift(int id)
    {
        var shift = await _context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shift == null)
        {
            return NotFound();
        }

        return shift;
    }

    [HttpPost]
    public async Task<ActionResult<Shift>> PostShift([FromBody] Shift shift, CancellationToken cancellationToken)
    {
        if (shift == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(shift.ShiftCode))
        {
            return BadRequest("ShiftCode is required.");
        }

        if (string.IsNullOrWhiteSpace(shift.ShiftName))
        {
            return BadRequest("ShiftName is required.");
        }

        if (shift.StartTime == shift.EndTime)
        {
            return BadRequest("Shift StartTime and EndTime cannot be the same.");
        }

        if (shift.GraceMinutes < 0)
        {
            return BadRequest("GraceMinutes must be non-negative.");
        }

        shift.ShiftCode = shift.ShiftCode.Trim();
        shift.ShiftName = shift.ShiftName.Trim();

        var duplicateCode = await _context.Shifts.AnyAsync(
            x => x.ShiftCode == shift.ShiftCode,
            cancellationToken);

        if (duplicateCode)
        {
            return Conflict("ShiftCode already exists.");
        }

        _context.Shifts.Add(shift);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetShift), new { id = shift.Id }, shift);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutShift(int id, [FromBody] Shift shift, CancellationToken cancellationToken)
    {
        if (shift == null)
        {
            return BadRequest();
        }

        var existing = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(shift.ShiftCode))
        {
            return BadRequest("ShiftCode is required.");
        }

        if (string.IsNullOrWhiteSpace(shift.ShiftName))
        {
            return BadRequest("ShiftName is required.");
        }

        if (shift.StartTime == shift.EndTime)
        {
            return BadRequest("Shift StartTime and EndTime cannot be the same.");
        }

        if (shift.GraceMinutes < 0)
        {
            return BadRequest("GraceMinutes must be non-negative.");
        }

        shift.ShiftCode = shift.ShiftCode.Trim();
        shift.ShiftName = shift.ShiftName.Trim();

        var duplicateCode = await _context.Shifts.AnyAsync(
            x => x.Id != id && x.ShiftCode == shift.ShiftCode,
            cancellationToken);

        if (duplicateCode)
        {
            return Conflict("ShiftCode already exists.");
        }

        existing.ShiftCode = shift.ShiftCode;
        existing.ShiftName = shift.ShiftName;
        existing.StartTime = shift.StartTime;
        existing.EndTime = shift.EndTime;
        existing.GraceMinutes = shift.GraceMinutes;
        existing.IsActive = shift.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Shifts.AnyAsync(x => x.Id == id, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteShift(int id, CancellationToken cancellationToken)
    {
        var shift = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (shift == null)
        {
            return NotFound();
        }

        shift.IsActive = false;

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
}
