using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
    {
        return await _context.Roles
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Role>> GetRole(int id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (role == null)
        {
            return NotFound();
        }

        return role;
    }

    [HttpPost]
    public async Task<ActionResult<Role>> PostRole([FromBody] Role role, CancellationToken cancellationToken)
    {
        if (role == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(role.RoleName))
        {
            return BadRequest("RoleName is required.");
        }

        role.RoleName = role.RoleName.Trim();
        if (role.Description != null)
        {
            role.Description = role.Description.Trim();
        }

        var duplicateName = await _context.Roles.AnyAsync(
            x => x.RoleName == role.RoleName,
            cancellationToken);

        if (duplicateName)
        {
            return Conflict("RoleName already exists.");
        }

        _context.Roles.Add(role);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutRole(int id, [FromBody] Role role, CancellationToken cancellationToken)
    {
        if (role == null)
        {
            return BadRequest();
        }

        var existing = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(role.RoleName))
        {
            return BadRequest("RoleName is required.");
        }

        role.RoleName = role.RoleName.Trim();
        if (role.Description != null)
        {
            role.Description = role.Description.Trim();
        }

        var duplicateName = await _context.Roles.AnyAsync(
            x => x.Id != id && x.RoleName == role.RoleName,
            cancellationToken);

        if (duplicateName)
        {
            return Conflict("RoleName already exists.");
        }

        existing.RoleName = role.RoleName;
        existing.Description = role.Description;
        existing.IsActive = role.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Roles.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteRole(int id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (role == null)
        {
            return NotFound();
        }

        role.IsActive = false;

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
