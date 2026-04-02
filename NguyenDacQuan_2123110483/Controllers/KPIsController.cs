using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class KPIsController : ControllerBase
{
    private readonly AppDbContext _context;

    public KPIsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KPI>>> GetKPIs()
    {
        return await _context.KPIs
            .Include(x => x.Employee)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<KPI>> GetKPI(int id)
    {
        var kpi = await _context.KPIs
            .Include(x => x.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (kpi == null)
        {
            return NotFound();
        }

        return kpi;
    }

    [HttpPost]
    public async Task<ActionResult<KPI>> PostKPI([FromBody] KPI kpi, CancellationToken cancellationToken)
    {
        if (kpi == null)
        {
            return BadRequest();
        }

        var validationError = ValidateKpi(kpi);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == kpi.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var duplicate = await _context.KPIs.AnyAsync(
            x => x.EmployeeId == kpi.EmployeeId &&
                 x.KpiMonth == kpi.KpiMonth &&
                 x.KpiYear == kpi.KpiYear,
            cancellationToken);
        if (duplicate)
        {
            return Conflict("KPI already exists for this employee and period.");
        }

        _context.KPIs.Add(kpi);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetKPI), new { id = kpi.Id }, kpi);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutKPI(int id, [FromBody] KPI kpi, CancellationToken cancellationToken)
    {
        if (kpi == null)
        {
            return BadRequest();
        }

        var existing = await _context.KPIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var validationError = ValidateKpi(kpi);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == kpi.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var duplicate = await _context.KPIs.AnyAsync(
            x => x.Id != id &&
                 x.EmployeeId == kpi.EmployeeId &&
                 x.KpiMonth == kpi.KpiMonth &&
                 x.KpiYear == kpi.KpiYear,
            cancellationToken);
        if (duplicate)
        {
            return Conflict("KPI already exists for this employee and period.");
        }

        existing.EmployeeId = kpi.EmployeeId;
        existing.KpiYear = kpi.KpiYear;
        existing.KpiMonth = kpi.KpiMonth;
        existing.Score = kpi.Score;
        existing.Target = kpi.Target;
        existing.Result = kpi.Result;
        existing.Note = kpi.Note;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.KPIs.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteKPI(int id, CancellationToken cancellationToken)
    {
        var kpi = await _context.KPIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (kpi == null)
        {
            return NotFound();
        }

        _context.KPIs.Remove(kpi);

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

    private static string? ValidateKpi(KPI kpi)
    {
        if (kpi.EmployeeId <= 0)
        {
            return "EmployeeId is required.";
        }

        if (kpi.KpiMonth < 1 || kpi.KpiMonth > 12)
        {
            return "KpiMonth must be between 1 and 12.";
        }

        if (kpi.KpiYear < 2000 || kpi.KpiYear > 2100)
        {
            return "KpiYear is invalid.";
        }

        if (string.IsNullOrWhiteSpace(kpi.Result))
        {
            return "Result is required.";
        }

        if (kpi.Score < 0)
        {
            return "Score must be non-negative.";
        }

        if (kpi.Target < 0)
        {
            return "Target must be non-negative.";
        }

        return null;
    }
}
