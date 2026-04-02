using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserAccountsController : ControllerBase
{
    private readonly AppDbContext _context;

    public UserAccountsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAccount>>> GetUserAccounts()
    {
        return await _context.UserAccounts
            .Include(x => x.Employee)
            .Include(x => x.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserAccount>> GetUserAccount(int id)
    {
        var userAccount = await _context.UserAccounts
            .Include(x => x.Employee)
            .Include(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (userAccount == null)
        {
            return NotFound();
        }

        return userAccount;
    }

    [HttpPost]
    public async Task<ActionResult<UserAccount>> PostUserAccount([FromBody] UserAccount userAccount, CancellationToken cancellationToken)
    {
        if (userAccount == null)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(userAccount.Username))
        {
            return BadRequest("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(userAccount.PasswordHash))
        {
            return BadRequest("PasswordHash is required.");
        }

        if (userAccount.EmployeeId <= 0)
        {
            return BadRequest("EmployeeId is required.");
        }

        if (userAccount.RoleId <= 0)
        {
            return BadRequest("RoleId is required.");
        }

        userAccount.Username = userAccount.Username.Trim();
        userAccount.PasswordHash = userAccount.PasswordHash.Trim();

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == userAccount.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var roleExists = await _context.Roles.AnyAsync(x => x.Id == userAccount.RoleId, cancellationToken);
        if (!roleExists)
        {
            return BadRequest("Role not found.");
        }

        var duplicateUsername = await _context.UserAccounts.AnyAsync(
            x => x.Username == userAccount.Username,
            cancellationToken);
        if (duplicateUsername)
        {
            return Conflict("Username already exists.");
        }

        var duplicateEmployee = await _context.UserAccounts.AnyAsync(
            x => x.EmployeeId == userAccount.EmployeeId,
            cancellationToken);
        if (duplicateEmployee)
        {
            return Conflict("Employee already has a user account.");
        }

        _context.UserAccounts.Add(userAccount);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetUserAccount), new { id = userAccount.Id }, userAccount);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutUserAccount(int id, [FromBody] UserAccount userAccount, CancellationToken cancellationToken)
    {
        if (userAccount == null)
        {
            return BadRequest();
        }

        var existing = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(userAccount.Username))
        {
            return BadRequest("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(userAccount.PasswordHash))
        {
            return BadRequest("PasswordHash is required.");
        }

        if (userAccount.EmployeeId <= 0)
        {
            return BadRequest("EmployeeId is required.");
        }

        if (userAccount.RoleId <= 0)
        {
            return BadRequest("RoleId is required.");
        }

        userAccount.Username = userAccount.Username.Trim();
        userAccount.PasswordHash = userAccount.PasswordHash.Trim();

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == userAccount.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var roleExists = await _context.Roles.AnyAsync(x => x.Id == userAccount.RoleId, cancellationToken);
        if (!roleExists)
        {
            return BadRequest("Role not found.");
        }

        var duplicateUsername = await _context.UserAccounts.AnyAsync(
            x => x.Id != id && x.Username == userAccount.Username,
            cancellationToken);
        if (duplicateUsername)
        {
            return Conflict("Username already exists.");
        }

        var duplicateEmployee = await _context.UserAccounts.AnyAsync(
            x => x.Id != id && x.EmployeeId == userAccount.EmployeeId,
            cancellationToken);
        if (duplicateEmployee)
        {
            return Conflict("Employee already has a user account.");
        }

        existing.EmployeeId = userAccount.EmployeeId;
        existing.RoleId = userAccount.RoleId;
        existing.Username = userAccount.Username;
        existing.PasswordHash = userAccount.PasswordHash;
        existing.IsActive = userAccount.IsActive;
        existing.LastLoginAt = userAccount.LastLoginAt;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.UserAccounts.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteUserAccount(int id, CancellationToken cancellationToken)
    {
        var userAccount = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (userAccount == null)
        {
            return NotFound();
        }

        userAccount.IsActive = false;

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
