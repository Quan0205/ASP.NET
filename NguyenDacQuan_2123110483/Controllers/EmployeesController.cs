using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
    {
        return await _context.Employees
            .Include(x => x.Branch)
            .Include(x => x.Role)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Employee>> GetEmployee(int id)
    {
        var employee = await _context.Employees
            .Include(x => x.Branch)
            .Include(x => x.Role)
            .Include(x => x.EmployeeContracts)
            .Include(x => x.UserAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (employee == null)
        {
            return NotFound();
        }

        return employee;
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> PostEmployee(Employee employee, CancellationToken cancellationToken)
    {
        var branchExists = await _context.Branches.AnyAsync(x => x.Id == employee.BranchId, cancellationToken);
        var roleExists = await _context.Roles.AnyAsync(x => x.Id == employee.RoleId, cancellationToken);

        if (!branchExists)
        {
            return BadRequest("Branch not found.");
        }

        if (!roleExists)
        {
            return BadRequest("Role not found.");
        }

        _context.Employees.Add(employee);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployee(int id, Employee employee, CancellationToken cancellationToken)
    {
        if (employee is null)
        {
            return BadRequest();
        }

        var existing = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var branchExists = await _context.Branches.AnyAsync(x => x.Id == employee.BranchId, cancellationToken);
        var roleExists = await _context.Roles.AnyAsync(x => x.Id == employee.RoleId, cancellationToken);

        if (!branchExists)
        {
            return BadRequest("Branch not found.");
        }

        if (!roleExists)
        {
            return BadRequest("Role not found.");
        }

        existing.EmployeeCode = employee.EmployeeCode;
        existing.FullName = employee.FullName;
        existing.Gender = employee.Gender;
        existing.DateOfBirth = employee.DateOfBirth;
        existing.Phone = employee.Phone;
        existing.Email = employee.Email;
        existing.Address = employee.Address;
        existing.BranchId = employee.BranchId;
        existing.RoleId = employee.RoleId;
        existing.HireDate = employee.HireDate;
        existing.IsActive = employee.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Employees.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }

        _context.Employees.Remove(employee);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return NoContent();
    }
}
