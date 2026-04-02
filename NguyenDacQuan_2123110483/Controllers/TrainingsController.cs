using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrainingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrainingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Training>>> GetTrainings()
    {
        return await _context.Trainings
            .Include(x => x.EmployeeTrainings)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Training>> GetTraining(int id)
    {
        var training = await _context.Trainings
            .Include(x => x.EmployeeTrainings)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (training == null)
        {
            return NotFound();
        }

        return training;
    }

    [HttpPost]
    public async Task<ActionResult<Training>> PostTraining([FromBody] Training training, CancellationToken cancellationToken)
    {
        if (training == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(training.TrainingCode))
        {
            return BadRequest("TrainingCode is required.");
        }

        if (string.IsNullOrWhiteSpace(training.TrainingName))
        {
            return BadRequest("TrainingName is required.");
        }

        if (training.EndDate.HasValue && training.EndDate.Value.Date < training.StartDate.Date)
        {
            return BadRequest("EndDate cannot be earlier than StartDate.");
        }

        training.TrainingCode = training.TrainingCode.Trim();
        training.TrainingName = training.TrainingName.Trim();
        if (training.Description != null)
        {
            training.Description = training.Description.Trim();
        }
        if (training.Instructor != null)
        {
            training.Instructor = training.Instructor.Trim();
        }

        var duplicateCode = await _context.Trainings.AnyAsync(
            x => x.TrainingCode == training.TrainingCode,
            cancellationToken);
        if (duplicateCode)
        {
            return Conflict("TrainingCode already exists.");
        }

        _context.Trainings.Add(training);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetTraining), new { id = training.Id }, training);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutTraining(int id, [FromBody] Training training, CancellationToken cancellationToken)
    {
        if (training == null)
        {
            return BadRequest();
        }

        var existing = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(training.TrainingCode))
        {
            return BadRequest("TrainingCode is required.");
        }

        if (string.IsNullOrWhiteSpace(training.TrainingName))
        {
            return BadRequest("TrainingName is required.");
        }

        if (training.EndDate.HasValue && training.EndDate.Value.Date < training.StartDate.Date)
        {
            return BadRequest("EndDate cannot be earlier than StartDate.");
        }

        training.TrainingCode = training.TrainingCode.Trim();
        training.TrainingName = training.TrainingName.Trim();
        if (training.Description != null)
        {
            training.Description = training.Description.Trim();
        }
        if (training.Instructor != null)
        {
            training.Instructor = training.Instructor.Trim();
        }

        var duplicateCode = await _context.Trainings.AnyAsync(
            x => x.Id != id && x.TrainingCode == training.TrainingCode,
            cancellationToken);
        if (duplicateCode)
        {
            return Conflict("TrainingCode already exists.");
        }

        existing.TrainingCode = training.TrainingCode;
        existing.TrainingName = training.TrainingName;
        existing.Description = training.Description;
        existing.StartDate = training.StartDate;
        existing.EndDate = training.EndDate;
        existing.Instructor = training.Instructor;
        existing.IsRequired = training.IsRequired;
        existing.IsActive = training.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Trainings.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteTraining(int id, CancellationToken cancellationToken)
    {
        var training = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (training == null)
        {
            return NotFound();
        }

        training.IsActive = false;

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
