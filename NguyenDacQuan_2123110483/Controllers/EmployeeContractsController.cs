using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeeContractsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeContractsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeContract>>> GetEmployeeContracts()
    {
        return await _context.EmployeeContracts
            .Include(x => x.Employee)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeContract>> GetEmployeeContract(int id)
    {
        var contract = await _context.EmployeeContracts
            .Include(x => x.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (contract == null)
        {
            return NotFound();
        }

        return contract;
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeContract>> PostEmployeeContract([FromBody] EmployeeContract contract, CancellationToken cancellationToken)
    {
        if (contract == null)
        {
            return BadRequest();
        }

        var validationError = ValidateContract(contract);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == contract.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var duplicateContractNo = await _context.EmployeeContracts.AnyAsync(
            x => x.ContractNo == contract.ContractNo,
            cancellationToken);

        if (duplicateContractNo)
        {
            return Conflict("ContractNo already exists.");
        }

        var overlapExists = await HasOverlappingActiveContractAsync(contract.EmployeeId, contract.StartDate, contract.EndDate, null, cancellationToken);
        if (overlapExists && contract.IsActive)
        {
            return BadRequest("Employee already has an active contract in the selected period.");
        }

        contract.ContractNo = contract.ContractNo.Trim();

        _context.EmployeeContracts.Add(contract);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.InnerException?.Message ?? ex.Message);
        }

        return CreatedAtAction(nameof(GetEmployeeContract), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployeeContract(int id, [FromBody] EmployeeContract contract, CancellationToken cancellationToken)
    {
        if (contract == null)
        {
            return BadRequest();
        }

        var existing = await _context.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        var validationError = ValidateContract(contract);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var employeeExists = await _context.Employees.AnyAsync(x => x.Id == contract.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return BadRequest("Employee not found.");
        }

        var duplicateContractNo = await _context.EmployeeContracts.AnyAsync(
            x => x.Id != id && x.ContractNo == contract.ContractNo,
            cancellationToken);

        if (duplicateContractNo)
        {
            return Conflict("ContractNo already exists.");
        }

        var overlapExists = await HasOverlappingActiveContractAsync(contract.EmployeeId, contract.StartDate, contract.EndDate, id, cancellationToken);
        if (overlapExists && contract.IsActive)
        {
            return BadRequest("Employee already has an active contract in the selected period.");
        }

        existing.ContractNo = contract.ContractNo.Trim();
        existing.ContractType = contract.ContractType;
        existing.EmployeeId = contract.EmployeeId;
        existing.StartDate = contract.StartDate;
        existing.EndDate = contract.EndDate;
        existing.BaseSalary = contract.BaseSalary;
        existing.HourlyRate = contract.HourlyRate;
        existing.OvertimeRateMultiplier = contract.OvertimeRateMultiplier;
        existing.LatePenaltyPerMinute = contract.LatePenaltyPerMinute;
        existing.EarlyLeavePenaltyPerMinute = contract.EarlyLeavePenaltyPerMinute;
        existing.StandardDailyHours = contract.StandardDailyHours;
        existing.IsActive = contract.IsActive;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.EmployeeContracts.AnyAsync(x => x.Id == id, cancellationToken))
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
    public async Task<IActionResult> DeleteEmployeeContract(int id, CancellationToken cancellationToken)
    {
        var contract = await _context.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (contract == null)
        {
            return NotFound();
        }

        contract.IsActive = false;

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

    private static string? ValidateContract(EmployeeContract contract)
    {
        if (string.IsNullOrWhiteSpace(contract.ContractNo))
        {
            return "ContractNo is required.";
        }

        if (contract.EmployeeId <= 0)
        {
            return "EmployeeId is required.";
        }

        if (contract.EndDate.HasValue && contract.EndDate.Value.Date < contract.StartDate.Date)
        {
            return "EndDate cannot be earlier than StartDate.";
        }

        if (contract.BaseSalary < 0)
        {
            return "BaseSalary must be non-negative.";
        }

        if (contract.HourlyRate < 0)
        {
            return "HourlyRate must be non-negative.";
        }

        if (contract.OvertimeRateMultiplier <= 0)
        {
            return "OvertimeRateMultiplier must be greater than 0.";
        }

        if (contract.LatePenaltyPerMinute < 0)
        {
            return "LatePenaltyPerMinute must be non-negative.";
        }

        if (contract.EarlyLeavePenaltyPerMinute < 0)
        {
            return "EarlyLeavePenaltyPerMinute must be non-negative.";
        }

        if (contract.StandardDailyHours <= 0)
        {
            return "StandardDailyHours must be greater than 0.";
        }

        return null;
    }

    private async Task<bool> HasOverlappingActiveContractAsync(
        int employeeId,
        DateTime startDate,
        DateTime? endDate,
        int? currentContractId,
        CancellationToken cancellationToken)
    {
        var contracts = await _context.EmployeeContracts
            .Where(x => x.EmployeeId == employeeId && x.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var existing in contracts)
        {
            if (currentContractId.HasValue && existing.Id == currentContractId.Value)
            {
                continue;
            }

            if (IsDateRangeOverlap(startDate, endDate, existing.StartDate, existing.EndDate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDateRangeOverlap(DateTime start1, DateTime? end1, DateTime start2, DateTime? end2)
    {
        var actualEnd1 = end1 ?? DateTime.MaxValue;
        var actualEnd2 = end2 ?? DateTime.MaxValue;

        return start1.Date <= actualEnd2.Date && start2.Date <= actualEnd1.Date;
    }
}
