using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecruitmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RecruitmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Recruitment>>> GetRecruitments()
    {
        return await _context.Recruitments
            .Include(x => x.Branch)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Recruitment>> GetRecruitment(int id)
    {
        var recruitment = await _context.Recruitments
            .Include(x => x.Branch)
            .Include(x => x.Candidates)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (recruitment == null)
        {
            return NotFound();
        }

        return recruitment;
    }

    [HttpPost]
    public async Task<ActionResult<Recruitment>> PostRecruitment([FromBody] Recruitment recruitment, CancellationToken cancellationToken)
    {
        if (recruitment == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(recruitment.PositionTitle))
        {
            return BadRequest("PositionTitle is required.");
        }

        if (recruitment.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(x => x.Id == recruitment.BranchId.Value, cancellationToken);
            if (!branchExists)
            {
                return BadRequest("Branch not found.");
            }
        }

        if (recruitment.CloseDate.HasValue && recruitment.CloseDate.Value.Date < recruitment.OpenDate.Date)
        {
            return BadRequest("CloseDate cannot be earlier than OpenDate.");
        }

        recruitment.PositionTitle = recruitment.PositionTitle.Trim();
        if (recruitment.Description != null)
        {
            recruitment.Description = recruitment.Description.Trim();
        }

        _context.Recruitments.Add(recruitment);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetRecruitment), new { id = recruitment.Id }, recruitment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutRecruitment(int id, [FromBody] Recruitment recruitment, CancellationToken cancellationToken)
    {
        if (recruitment == null)
        {
            return BadRequest();
        }

        var existing = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(recruitment.PositionTitle))
        {
            return BadRequest("PositionTitle is required.");
        }

        if (recruitment.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(x => x.Id == recruitment.BranchId.Value, cancellationToken);
            if (!branchExists)
            {
                return BadRequest("Branch not found.");
            }
        }

        if (recruitment.CloseDate.HasValue && recruitment.CloseDate.Value.Date < recruitment.OpenDate.Date)
        {
            return BadRequest("CloseDate cannot be earlier than OpenDate.");
        }

        existing.BranchId = recruitment.BranchId;
        existing.PositionTitle = recruitment.PositionTitle.Trim();
        existing.OpenDate = recruitment.OpenDate;
        existing.CloseDate = recruitment.CloseDate;
        existing.Status = recruitment.Status;
        existing.Description = recruitment.Description;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Recruitments.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteRecruitment(int id, CancellationToken cancellationToken)
    {
        var recruitment = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (recruitment == null)
        {
            return NotFound();
        }

        recruitment.Status = RecruitmentStatus.Cancelled;
        recruitment.CloseDate ??= DateTime.UtcNow;

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
