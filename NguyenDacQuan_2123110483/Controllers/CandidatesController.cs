using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CandidatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CandidatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Candidate>>> GetCandidates()
    {
        return await _context.Candidates
            .Include(x => x.Recruitment)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Candidate>> GetCandidate(int id)
    {
        var candidate = await _context.Candidates
            .Include(x => x.Recruitment)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (candidate == null)
        {
            return NotFound();
        }

        return candidate;
    }

    [HttpPost]
    public async Task<ActionResult<Candidate>> PostCandidate([FromBody] Candidate candidate, CancellationToken cancellationToken)
    {
        if (candidate == null)
        {
            return BadRequest();
        }

        if (candidate.RecruitmentId <= 0)
        {
            return BadRequest("RecruitmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(candidate.FullName))
        {
            return BadRequest("FullName is required.");
        }

        var recruitment = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == candidate.RecruitmentId, cancellationToken);
        if (recruitment == null)
        {
            return BadRequest("Recruitment not found.");
        }

        if (recruitment.Status == RecruitmentStatus.Closed || recruitment.Status == RecruitmentStatus.Cancelled)
        {
            return BadRequest("Recruitment is closed.");
        }

        candidate.FullName = candidate.FullName.Trim();
        if (candidate.Email != null)
        {
            candidate.Email = candidate.Email.Trim();
        }
        if (candidate.Note != null)
        {
            candidate.Note = candidate.Note.Trim();
        }

        var duplicateEmail = await _context.Candidates.AnyAsync(
            x => x.RecruitmentId == candidate.RecruitmentId && x.Email == candidate.Email && x.Email != null,
            cancellationToken);
        if (duplicateEmail)
        {
            return Conflict("Candidate email already exists in this recruitment.");
        }

        _context.Candidates.Add(candidate);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetCandidate), new { id = candidate.Id }, candidate);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCandidate(int id, [FromBody] Candidate candidate, CancellationToken cancellationToken)
    {
        if (candidate == null)
        {
            return BadRequest();
        }

        var existing = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (candidate.RecruitmentId <= 0)
        {
            return BadRequest("RecruitmentId is required.");
        }

        if (string.IsNullOrWhiteSpace(candidate.FullName))
        {
            return BadRequest("FullName is required.");
        }

        var recruitment = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == candidate.RecruitmentId, cancellationToken);
        if (recruitment == null)
        {
            return BadRequest("Recruitment not found.");
        }

        if (recruitment.Status == RecruitmentStatus.Closed || recruitment.Status == RecruitmentStatus.Cancelled)
        {
            return BadRequest("Recruitment is closed.");
        }

        candidate.FullName = candidate.FullName.Trim();
        if (candidate.Email != null)
        {
            candidate.Email = candidate.Email.Trim();
        }
        if (candidate.Note != null)
        {
            candidate.Note = candidate.Note.Trim();
        }

        var duplicateEmail = await _context.Candidates.AnyAsync(
            x => x.Id != id &&
                 x.RecruitmentId == candidate.RecruitmentId &&
                 x.Email == candidate.Email &&
                 x.Email != null,
            cancellationToken);
        if (duplicateEmail)
        {
            return Conflict("Candidate email already exists in this recruitment.");
        }

        existing.RecruitmentId = candidate.RecruitmentId;
        existing.FullName = candidate.FullName;
        existing.Phone = candidate.Phone;
        existing.Email = candidate.Email;
        existing.AppliedDate = candidate.AppliedDate;
        existing.Status = candidate.Status;
        existing.InterviewScore = candidate.InterviewScore;
        existing.Note = candidate.Note;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Candidates.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteCandidate(int id, CancellationToken cancellationToken)
    {
        var candidate = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (candidate == null)
        {
            return NotFound();
        }

        _context.Candidates.Remove(candidate);

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
