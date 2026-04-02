using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BranchesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Branch>>> GetBranches()
    {
        return await _context.Branches
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Branch>> GetBranch(int id)
    {
        var branch = await _context.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (branch == null)
        {
            return NotFound();
        }

        return branch;
    }

    [HttpPost]
    public async Task<ActionResult<Branch>> PostBranch([FromBody] Branch branch, CancellationToken cancellationToken)
    {
        if (branch == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(branch.BranchCode))
        {
            return BadRequest("BranchCode is required.");
        }

        if (string.IsNullOrWhiteSpace(branch.BranchName))
        {
            return BadRequest("BranchName is required.");
        }

        if (string.IsNullOrWhiteSpace(branch.Address))
        {
            return BadRequest("Address is required.");
        }

        branch.BranchCode = branch.BranchCode.Trim();
        branch.BranchName = branch.BranchName.Trim();
        branch.Address = branch.Address.Trim();

        var duplicateCode = await _context.Branches.AnyAsync(
            x => x.BranchCode == branch.BranchCode,
            cancellationToken);

        if (duplicateCode)
        {
            return Conflict("BranchCode already exists.");
        }

        _context.Branches.Add(branch);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutBranch(int id, [FromBody] Branch branch, CancellationToken cancellationToken)
    {
        if (branch == null)
        {
            return BadRequest();
        }

        var existing = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(branch.BranchCode))
        {
            return BadRequest("BranchCode is required.");
        }

        if (string.IsNullOrWhiteSpace(branch.BranchName))
        {
            return BadRequest("BranchName is required.");
        }

        if (string.IsNullOrWhiteSpace(branch.Address))
        {
            return BadRequest("Address is required.");
        }

        branch.BranchCode = branch.BranchCode.Trim();
        branch.BranchName = branch.BranchName.Trim();
        branch.Address = branch.Address.Trim();

        var duplicateCode = await _context.Branches.AnyAsync(
            x => x.Id != id && x.BranchCode == branch.BranchCode,
            cancellationToken);

        if (duplicateCode)
        {
            return Conflict("BranchCode already exists.");
        }

        existing.BranchCode = branch.BranchCode;
        existing.BranchName = branch.BranchName;
        existing.Address = branch.Address;
        existing.Phone = branch.Phone;
        existing.IsActive = branch.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Branches.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteBranch(int id, CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch == null)
        {
            return NotFound();
        }

        branch.IsActive = false;

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
