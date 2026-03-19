using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PayrollController : ControllerBase
{
    private readonly AppDbContext _context;

    public PayrollController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payroll>>> GetPayrolls()
    {
        return await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeContract)
            .Include(x => x.PayrollDetails)
            .AsNoTracking()
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Payroll>> GetPayroll(int id)
    {
        var payroll = await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeContract)
            .Include(x => x.PayrollDetails)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (payroll == null)
        {
            return NotFound();
        }

        return payroll;
    }

    [HttpPost]
    public async Task<ActionResult<Payroll>> PostPayroll([FromBody] PayrollRequest request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return BadRequest("Employee not found.");
        }

        var contract = await _context.EmployeeContracts.FirstOrDefaultAsync(x =>
            x.Id == request.EmployeeContractId &&
            x.EmployeeId == request.EmployeeId, cancellationToken);

        if (contract == null)
        {
            return BadRequest("Employee contract not found.");
        }

        var payrollExists = await _context.Payrolls.AnyAsync(x =>
            x.EmployeeId == request.EmployeeId &&
            x.PayrollMonth == request.PayrollMonth &&
            x.PayrollYear == request.PayrollYear, cancellationToken);

        if (payrollExists)
        {
            return Conflict("Payroll already exists for this employee and period.");
        }

        var payroll = new Payroll
        {
            EmployeeId = request.EmployeeId,
            EmployeeContractId = request.EmployeeContractId,
            PayrollMonth = request.PayrollMonth,
            PayrollYear = request.PayrollYear,
            HourlyRate = contract.HourlyRate,
            Status = PayrollStatus.Draft
        };

        RecalculatePayroll(payroll);
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPayroll), new { id = payroll.Id }, payroll);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPayroll(int id, [FromBody] PayrollRequest request, CancellationToken cancellationToken)
    {
        var payroll = await _context.Payrolls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null)
        {
            return NotFound();
        }

        var contract = await _context.EmployeeContracts.FirstOrDefaultAsync(x =>
            x.Id == request.EmployeeContractId &&
            x.EmployeeId == request.EmployeeId, cancellationToken);

        if (contract == null)
        {
            return BadRequest("Employee contract not found.");
        }

        payroll.EmployeeId = request.EmployeeId;
        payroll.EmployeeContractId = request.EmployeeContractId;
        payroll.PayrollMonth = request.PayrollMonth;
        payroll.PayrollYear = request.PayrollYear;
        payroll.HourlyRate = contract.HourlyRate;
        RecalculatePayroll(payroll);

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePayroll(int id, CancellationToken cancellationToken)
    {
        var payroll = await _context.Payrolls.FindAsync([id], cancellationToken);
        if (payroll == null)
        {
            return NotFound();
        }

        _context.Payrolls.Remove(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{payrollId:int}/details")]
    public async Task<ActionResult<IEnumerable<PayrollDetail>>> GetPayrollDetails(int payrollId, CancellationToken cancellationToken)
    {
        var exists = await _context.Payrolls.AnyAsync(x => x.Id == payrollId, cancellationToken);
        if (!exists)
        {
            return NotFound("Payroll not found.");
        }

        var details = await _context.PayrollDetails
            .Where(x => x.PayrollId == payrollId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Ok(details);
    }

    [HttpPost("{payrollId:int}/details")]
    public async Task<ActionResult<PayrollDetail>> AddPayrollDetail(int payrollId, [FromBody] PayrollDetailRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount < 0)
        {
            return BadRequest("Amount must be non-negative.");
        }

        var payroll = await _context.Payrolls
            .Include(x => x.PayrollDetails)
            .FirstOrDefaultAsync(x => x.Id == payrollId, cancellationToken);

        if (payroll == null)
        {
            return NotFound("Payroll not found.");
        }

        var detail = new PayrollDetail
        {
            PayrollId = payrollId,
            DetailType = request.DetailType,
            AttendanceId = request.AttendanceId,
            ScheduleId = request.ScheduleId,
            SourceReferenceId = request.SourceReferenceId,
            Description = request.Description,
            Amount = request.Amount,
            Note = request.Note
        };

        _context.PayrollDetails.Add(detail);
        await _context.SaveChangesAsync(cancellationToken);

        RecalculatePayrollFromDetails(payroll);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPayrollDetails), new { payrollId }, detail);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<Payroll>> GeneratePayroll([FromBody] PayrollGenerateRequest request, CancellationToken cancellationToken)
    {
        if (request.EmployeeId <= 0 || request.PayrollMonth is < 1 or > 12 || request.PayrollYear < 2000)
        {
            return BadRequest("Invalid payroll request.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var employee = await _context.Employees
            .Include(x => x.EmployeeContracts)
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);

        if (employee == null)
        {
            return NotFound("Employee not found.");
        }

        var contract = employee.EmployeeContracts
            .Where(x => x.IsActive && x.StartDate.Date <= new DateTime(request.PayrollYear, request.PayrollMonth, 1))
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefault();

        if (contract == null)
        {
            return BadRequest("Active employee contract not found.");
        }

        var payroll = await _context.Payrolls
            .Include(x => x.PayrollDetails)
            .FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId &&
                                     x.PayrollMonth == request.PayrollMonth &&
                                     x.PayrollYear == request.PayrollYear, cancellationToken);

        if (payroll != null)
        {
            _context.PayrollDetails.RemoveRange(payroll.PayrollDetails);
        }
        else
        {
            payroll = new Payroll
            {
                EmployeeId = request.EmployeeId,
                EmployeeContractId = contract.Id,
                PayrollMonth = request.PayrollMonth,
                PayrollYear = request.PayrollYear,
                Status = PayrollStatus.Draft
            };
            _context.Payrolls.Add(payroll);
        }

        var startDate = new DateTime(request.PayrollYear, request.PayrollMonth, 1);
        var endDate = startDate.AddMonths(1);

        var attendances = await _context.Attendances
            .Include(x => x.Shift)
            .Where(x => x.EmployeeId == request.EmployeeId &&
                         x.AttendanceDate >= startDate &&
                         x.AttendanceDate < endDate &&
                         x.CheckInAt != null &&
                         x.CheckOutAt != null)
            .ToListAsync(cancellationToken);

        if (attendances.Count == 0)
        {
            return BadRequest("No completed attendance found for this payroll period.");
        }

        payroll.EmployeeContractId = contract.Id;
        payroll.HourlyRate = contract.HourlyRate > 0 ? contract.HourlyRate : ComputeHourlyRate(contract);

        payroll.WorkingHours = attendances.Sum(x => x.WorkingMinutes) / 60m;
        payroll.BaseAmount = attendances.Sum(x => GetRegularMinutes(x, contract) / 60m * payroll.HourlyRate);
        payroll.OvertimeAmount = attendances.Sum(x => x.OvertimeMinutes / 60m * payroll.HourlyRate * contract.OvertimeRateMultiplier);
        payroll.AllowanceAmount = 0m;
        payroll.BonusAmount = 0m;
        payroll.PenaltyAmount = attendances.Sum(x =>
            x.LateMinutes * contract.LatePenaltyPerMinute +
            x.EarlyLeaveMinutes * contract.EarlyLeavePenaltyPerMinute);

        payroll.TotalSalary = payroll.BaseAmount + payroll.OvertimeAmount + payroll.AllowanceAmount + payroll.BonusAmount - payroll.PenaltyAmount;
        payroll.Status = PayrollStatus.Generated;

        var details = new List<PayrollDetail>();

        foreach (var attendance in attendances)
        {
            if (attendance.OvertimeMinutes > 0)
            {
                details.Add(new PayrollDetail
                {
                    Payroll = payroll,
                    AttendanceId = attendance.Id,
                    DetailType = PayrollDetailType.Overtime,
                    Description = $"Overtime on {attendance.AttendanceDate:yyyy-MM-dd}",
                    Amount = attendance.OvertimeMinutes / 60m * payroll.HourlyRate * contract.OvertimeRateMultiplier
                });
            }

            if (attendance.LateMinutes > 0)
            {
                details.Add(new PayrollDetail
                {
                    Payroll = payroll,
                    AttendanceId = attendance.Id,
                    DetailType = PayrollDetailType.Penalty,
                    Description = $"Late arrival on {attendance.AttendanceDate:yyyy-MM-dd}",
                    Amount = attendance.LateMinutes * contract.LatePenaltyPerMinute
                });
            }

            if (attendance.EarlyLeaveMinutes > 0)
            {
                details.Add(new PayrollDetail
                {
                    Payroll = payroll,
                    AttendanceId = attendance.Id,
                    DetailType = PayrollDetailType.Penalty,
                    Description = $"Early leave on {attendance.AttendanceDate:yyyy-MM-dd}",
                    Amount = attendance.EarlyLeaveMinutes * contract.EarlyLeavePenaltyPerMinute
                });
            }
        }

        _context.PayrollDetails.AddRange(details);
        await _context.SaveChangesAsync(cancellationToken);
        RecalculatePayrollFromDetails(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        payroll = await _context.Payrolls
            .Include(x => x.Employee)
            .Include(x => x.EmployeeContract)
            .Include(x => x.PayrollDetails)
            .AsNoTracking()
            .FirstAsync(x => x.Id == payroll.Id, cancellationToken);

        return Ok(payroll);
    }

    private static decimal ComputeHourlyRate(EmployeeContract contract)
    {
        var baseSalary = contract.BaseSalary <= 0 ? 0m : contract.BaseSalary;
        if (baseSalary <= 0 || contract.StandardDailyHours <= 0)
        {
            return 0m;
        }

        return baseSalary / (contract.StandardDailyHours * 26m);
    }

    private static decimal GetRegularMinutes(Attendance attendance, EmployeeContract contract)
    {
        var standardMinutes = contract.StandardDailyHours * 60m;
        return Math.Min(attendance.WorkingMinutes, (int)standardMinutes);
    }

    private static void RecalculatePayroll(Payroll payroll)
    {
        payroll.TotalSalary = payroll.BaseAmount + payroll.OvertimeAmount + payroll.AllowanceAmount + payroll.BonusAmount - payroll.PenaltyAmount;
    }

    private void RecalculatePayrollFromDetails(Payroll payroll)
    {
        var details = payroll.PayrollDetails;

        payroll.AllowanceAmount = details.Where(x => x.DetailType == PayrollDetailType.Allowance).Sum(x => x.Amount);
        payroll.BonusAmount = details.Where(x => x.DetailType == PayrollDetailType.Bonus).Sum(x => x.Amount);
        payroll.OvertimeAmount = details.Where(x => x.DetailType == PayrollDetailType.Overtime).Sum(x => x.Amount);
        payroll.PenaltyAmount = details.Where(x => x.DetailType == PayrollDetailType.Penalty).Sum(x => x.Amount);
        RecalculatePayroll(payroll);
    }
}

public sealed record PayrollRequest(int EmployeeId, int EmployeeContractId, int PayrollMonth, int PayrollYear);
public sealed record PayrollGenerateRequest(int EmployeeId, int PayrollMonth, int PayrollYear);
public sealed record PayrollDetailRequest(
    PayrollDetailType DetailType,
    decimal Amount,
    string Description,
    int? AttendanceId,
    int? ScheduleId,
    int? SourceReferenceId,
    string? Note);
