using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeTrainingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeTrainingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeTraining>>> GetEmployeeTrainings()
    {
        return await _context.EmployeeTrainings
            .Include(x => x.Employee)
            .Include(x => x.Training)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeTraining>> GetEmployeeTraining(int id)
    {
        var employeeTraining = await _context.EmployeeTrainings
            .Include(x => x.Employee)
            .Include(x => x.Training)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (employeeTraining == null)
        {
            return NotFound();
        }

        return employeeTraining;
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeTraining>> PostEmployeeTraining([FromBody] EmployeeTraining employeeTraining, CancellationToken cancellationToken)
    {
        if (employeeTraining == null)
        {
            return BadRequest();
        }

        var validationError = ValidateEmployeeTraining(employeeTraining);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == employeeTraining.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var training = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == employeeTraining.TrainingId, cancellationToken);
        if (training == null)
        {
            return BadRequest("Training not found.");
        }

        if (!training.IsActive)
        {
            return BadRequest("Training is inactive.");
        }

        var duplicate = await _context.EmployeeTrainings.AnyAsync(
            x => x.EmployeeId == employeeTraining.EmployeeId && x.TrainingId == employeeTraining.TrainingId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict("Employee training already exists.");
        }

        _context.EmployeeTrainings.Add(employeeTraining);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetEmployeeTraining), new { id = employeeTraining.Id }, employeeTraining);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployeeTraining(int id, [FromBody] EmployeeTraining employeeTraining, CancellationToken cancellationToken)
    {
        if (employeeTraining == null)
        {
            return BadRequest();
        }

        var existing = await _context.EmployeeTrainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var validationError = ValidateEmployeeTraining(employeeTraining);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == employeeTraining.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var training = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == employeeTraining.TrainingId, cancellationToken);
        if (training == null)
        {
            return BadRequest("Training not found.");
        }

        if (!training.IsActive)
        {
            return BadRequest("Training is inactive.");
        }

        var duplicate = await _context.EmployeeTrainings.AnyAsync(
            x => x.Id != id && x.EmployeeId == employeeTraining.EmployeeId && x.TrainingId == employeeTraining.TrainingId,
            cancellationToken);
        if (duplicate)
        {
            return Conflict("Employee training already exists.");
        }

        existing.EmployeeId = employeeTraining.EmployeeId;
        existing.TrainingId = employeeTraining.TrainingId;
        existing.AssignedDate = employeeTraining.AssignedDate;
        existing.CompletedDate = employeeTraining.CompletedDate;
        existing.Status = employeeTraining.Status;
        existing.Score = employeeTraining.Score;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.EmployeeTrainings.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteEmployeeTraining(int id, CancellationToken cancellationToken)
    {
        var employeeTraining = await _context.EmployeeTrainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (employeeTraining == null)
        {
            return NotFound();
        }

        _context.EmployeeTrainings.Remove(employeeTraining);

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

    private static string? ValidateEmployeeTraining(EmployeeTraining employeeTraining)
    {
        if (employeeTraining.EmployeeId <= 0)
        {
            return "EmployeeId is required.";
        }

        if (employeeTraining.TrainingId <= 0)
        {
            return "TrainingId is required.";
        }

        if (employeeTraining.CompletedDate.HasValue && employeeTraining.CompletedDate.Value < employeeTraining.AssignedDate)
        {
            return "CompletedDate cannot be earlier than AssignedDate.";
        }

        return null;
    }
}
