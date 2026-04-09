using System.Text;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IPayrollService
{
    Task<IReadOnlyList<PayrollResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PayrollResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollClosePeriodResponseDto>> GetClosePeriodsAsync(CancellationToken cancellationToken = default);
    Task<(PayrollResponseDto? Payroll, string? Error, int? StatusCode)> CreateAsync(PayrollRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, PayrollRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> PayAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ClosePeriodAsync(PayrollClosePeriodRequestDto request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ReopenPeriodAsync(PayrollClosePeriodRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollDetailResponseDto>?> GetDetailsAsync(int payrollId, CancellationToken cancellationToken = default);
    Task<(PayrollDetailResponseDto? Detail, string? Error, int? StatusCode)> AddDetailAsync(int payrollId, PayrollDetailRequestDto request, CancellationToken cancellationToken = default);
    Task<(PayrollResponseDto? Payroll, string? Error, int? StatusCode)> GenerateAsync(PayrollGenerateRequestDto request, CancellationToken cancellationToken = default);
    Task<string> ExportCsvAsync(CancellationToken cancellationToken = default);
}

public sealed class PayrollService : IPayrollService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PayrollService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<PayrollResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryPayrolls().AsNoTracking()
            .OrderByDescending(x => x.PayrollYear)
            .ThenByDescending(x => x.PayrollMonth)
            .ThenBy(x => x.EmployeeId)
            .ToListAsync(cancellationToken);
        return rows.Select(x => MapPayroll(x, true)).ToList();
    }

    public async Task<PayrollResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await QueryPayrolls().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapPayroll(row, true);
    }

    public async Task<IReadOnlyList<PayrollClosePeriodResponseDto>> GetClosePeriodsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.PayrollClosePeriods.AsNoTracking()
            .OrderByDescending(x => x.PayrollYear)
            .ThenByDescending(x => x.PayrollMonth)
            .ToListAsync(cancellationToken);
        return rows.Select(x => new PayrollClosePeriodResponseDto(x.Id, x.PayrollMonth, x.PayrollYear, x.IsClosed, x.ClosedAt, x.ClosedByUserAccountId, x.Note, x.CreatedAt, x.UpdatedAt)).ToList();
    }

    public async Task<(PayrollResponseDto? Payroll, string? Error, int? StatusCode)> CreateAsync(PayrollRequestDto request, CancellationToken cancellationToken = default)
    {
        if (await IsPeriodClosedAsync(request.PayrollMonth, request.PayrollYear, cancellationToken))
            return (null, "Payroll period is closed.", StatusCodes.Status409Conflict);

        var validation = await ValidatePayrollRequestAsync(request, cancellationToken);
        if (validation is not null)
            return (null, validation.Value.Error, validation.Value.StatusCode);

        var payroll = new Payroll
        {
            EmployeeId = request.EmployeeId,
            EmployeeContractId = request.EmployeeContractId,
            PayrollMonth = request.PayrollMonth,
            PayrollYear = request.PayrollYear,
            HourlyRate = request.HourlyRate ?? 0m,
            AllowanceAmount = request.AllowanceAmount,
            BonusAmount = request.BonusAmount,
            PenaltyAmount = request.PenaltyAmount,
            InsuranceAmount = request.InsuranceAmount,
            TaxAmount = request.TaxAmount,
            Status = PayrollStatus.Draft,
            Note = request.Note
        };

        RecalculatePayroll(payroll);
        _context.Payrolls.Add(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(payroll.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, PayrollRequestDto request, CancellationToken cancellationToken = default)
    {
        var payroll = await _context.Payrolls.Include(x => x.PayrollDetails).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null)
            return (false, "Payroll not found.", StatusCodes.Status404NotFound);
        if (!CanEditPayroll(payroll))
            return (false, "Only draft payroll can be updated.", StatusCodes.Status409Conflict);
        if (await IsPeriodClosedAsync(request.PayrollMonth, request.PayrollYear, cancellationToken, payroll.Id))
            return (false, "Payroll period is closed.", StatusCodes.Status409Conflict);

        var validation = await ValidatePayrollRequestAsync(request, cancellationToken, payroll.Id);
        if (validation is not null)
            return (false, validation.Value.Error, validation.Value.StatusCode);

        payroll.EmployeeId = request.EmployeeId;
        payroll.EmployeeContractId = request.EmployeeContractId;
        payroll.PayrollMonth = request.PayrollMonth;
        payroll.PayrollYear = request.PayrollYear;
        payroll.HourlyRate = request.HourlyRate ?? payroll.HourlyRate;
        payroll.AllowanceAmount = request.AllowanceAmount;
        payroll.BonusAmount = request.BonusAmount;
        payroll.PenaltyAmount = request.PenaltyAmount;
        payroll.InsuranceAmount = request.InsuranceAmount;
        payroll.TaxAmount = request.TaxAmount;
        payroll.Note = request.Note;
        RecalculatePayrollFromDetails(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var payroll = await _context.Payrolls.Include(x => x.PayrollDetails).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null)
            return (false, "Payroll not found.", StatusCodes.Status404NotFound);
        if (!CanEditPayroll(payroll))
            return (false, "Only draft payroll can be deleted.", StatusCodes.Status409Conflict);

        _context.PayrollDetails.RemoveRange(payroll.PayrollDetails);
        _context.Payrolls.Remove(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ApproveAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default)
    {
        var payroll = await _context.Payrolls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null) return (false, "Payroll not found.", StatusCodes.Status404NotFound);
        if (payroll.Status is not PayrollStatus.Generated and not PayrollStatus.Draft) return (false, "Only draft or generated payroll can be approved.", StatusCodes.Status409Conflict);
        payroll.Status = PayrollStatus.Approved;
        payroll.ApprovedAt = DateTime.UtcNow;
        payroll.ApprovedByUserAccountId = _currentUserService.UserId;
        payroll.Note = request.Note ?? payroll.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> PayAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default)
    {
        var payroll = await _context.Payrolls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null) return (false, "Payroll not found.", StatusCodes.Status404NotFound);
        if (payroll.Status != PayrollStatus.Approved) return (false, "Only approved payroll can be marked as paid.", StatusCodes.Status409Conflict);
        payroll.Status = PayrollStatus.Paid;
        payroll.PaidDate = DateTime.UtcNow;
        payroll.Note = request.Note ?? payroll.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, PayrollDecisionRequestDto request, CancellationToken cancellationToken = default)
    {
        var payroll = await _context.Payrolls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (payroll == null) return (false, "Payroll not found.", StatusCodes.Status404NotFound);
        if (payroll.Status == PayrollStatus.Paid) return (false, "Paid payroll cannot be cancelled.", StatusCodes.Status409Conflict);
        payroll.Status = PayrollStatus.Cancelled;
        payroll.Note = request.Note ?? payroll.Note;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ClosePeriodAsync(PayrollClosePeriodRequestDto request, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollClosePeriods.FirstOrDefaultAsync(x => x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear, cancellationToken);
        if (period == null)
        {
            period = new PayrollClosePeriod { PayrollMonth = request.PayrollMonth, PayrollYear = request.PayrollYear };
            _context.PayrollClosePeriods.Add(period);
        }
        period.IsClosed = true;
        period.ClosedAt = DateTime.UtcNow;
        period.ClosedByUserAccountId = _currentUserService.UserId;
        period.Note = request.Note;
        var payrolls = await _context.Payrolls.Where(x => x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear).ToListAsync(cancellationToken);
        foreach (var payroll in payrolls)
        {
            payroll.IsClosed = true;
            payroll.ClosedAt = DateTime.UtcNow;
            payroll.ClosedByUserAccountId = _currentUserService.UserId;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ReopenPeriodAsync(PayrollClosePeriodRequestDto request, CancellationToken cancellationToken = default)
    {
        var period = await _context.PayrollClosePeriods.FirstOrDefaultAsync(x => x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear, cancellationToken);
        if (period == null) return (false, "Payroll close period not found.", StatusCodes.Status404NotFound);
        period.IsClosed = false;
        period.Note = request.Note;
        var payrolls = await _context.Payrolls.Where(x => x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear).ToListAsync(cancellationToken);
        foreach (var payroll in payrolls)
        {
            payroll.IsClosed = false;
            payroll.ClosedAt = null;
            payroll.ClosedByUserAccountId = null;
        }
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<IReadOnlyList<PayrollDetailResponseDto>?> GetDetailsAsync(int payrollId, CancellationToken cancellationToken = default)
    {
        if (!await _context.Payrolls.AnyAsync(x => x.Id == payrollId, cancellationToken)) return null;
        var details = await _context.PayrollDetails.Where(x => x.PayrollId == payrollId).AsNoTracking().ToListAsync(cancellationToken);
        return details.Select(MapPayrollDetail).ToList();
    }

    public async Task<(PayrollDetailResponseDto? Detail, string? Error, int? StatusCode)> AddDetailAsync(int payrollId, PayrollDetailRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0) return (null, "Amount must be non-negative.", StatusCodes.Status400BadRequest);
        var payroll = await _context.Payrolls.Include(x => x.PayrollDetails).FirstOrDefaultAsync(x => x.Id == payrollId, cancellationToken);
        if (payroll == null) return (null, "Payroll not found.", StatusCodes.Status404NotFound);
        if (!CanEditPayroll(payroll)) return (null, "Only draft payroll can be updated.", StatusCodes.Status409Conflict);
        if (request.AttendanceId.HasValue && !await _context.Attendances.AnyAsync(x => x.Id == request.AttendanceId.Value, cancellationToken)) return (null, "Attendance not found.", StatusCodes.Status400BadRequest);
        if (request.ScheduleId.HasValue && !await _context.Schedules.AnyAsync(x => x.Id == request.ScheduleId.Value, cancellationToken)) return (null, "Schedule not found.", StatusCodes.Status400BadRequest);
        if (!Enum.IsDefined(typeof(PayrollDetailType), request.DetailType)) return (null, "Invalid payroll detail type.", StatusCodes.Status400BadRequest);

        var detail = new PayrollDetail
        {
            PayrollId = payrollId,
            DetailType = (PayrollDetailType)request.DetailType,
            AttendanceId = request.AttendanceId,
            ScheduleId = request.ScheduleId,
            SourceReferenceId = request.SourceReferenceId,
            Description = request.Description,
            Amount = request.Amount,
            Note = request.Note
        };
        _context.PayrollDetails.Add(detail);
        await _context.SaveChangesAsync(cancellationToken);
        payroll.PayrollDetails.Add(detail);
        RecalculatePayrollFromDetails(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return (MapPayrollDetail(detail), null, null);
    }

    public async Task<(PayrollResponseDto? Payroll, string? Error, int? StatusCode)> GenerateAsync(PayrollGenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EmployeeId <= 0 || request.PayrollMonth is < 1 or > 12 || request.PayrollYear < 2000)
            return (null, "Invalid payroll request.", StatusCodes.Status400BadRequest);
        if (await IsPeriodClosedAsync(request.PayrollMonth, request.PayrollYear, cancellationToken))
            return (null, "Payroll period is closed.", StatusCodes.Status409Conflict);

        var employee = await _context.Employees.Include(x => x.EmployeeContracts).FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee == null || !employee.IsActive) return (null, "Active employee not found.", StatusCodes.Status404NotFound);
        var contract = employee.EmployeeContracts.Where(x => x.IsActive && x.StartDate.Date <= new DateTime(request.PayrollYear, request.PayrollMonth, 1)).OrderByDescending(x => x.StartDate).FirstOrDefault();
        if (contract == null) return (null, "Active employee contract not found.", StatusCodes.Status400BadRequest);

        var payroll = await _context.Payrolls.Include(x => x.PayrollDetails).FirstOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear, cancellationToken);
        if (payroll != null && !CanEditPayroll(payroll)) return (null, "Only draft payroll can be regenerated.", StatusCodes.Status409Conflict);
        if (payroll != null) _context.PayrollDetails.RemoveRange(payroll.PayrollDetails);
        else
        {
            payroll = new Payroll { EmployeeId = request.EmployeeId, EmployeeContractId = contract.Id, PayrollMonth = request.PayrollMonth, PayrollYear = request.PayrollYear, Status = PayrollStatus.Draft };
            _context.Payrolls.Add(payroll);
        }

        var startDate = new DateTime(request.PayrollYear, request.PayrollMonth, 1);
        var endDate = startDate.AddMonths(1);
        var attendances = await _context.Attendances.Include(x => x.Shift).Where(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate >= startDate && x.AttendanceDate < endDate).ToListAsync(cancellationToken);
        var completedAttendances = attendances.Where(x => x.CheckInAt != null && x.CheckOutAt != null).ToList();
        var absentCount = attendances.Count(x => x.Status == AttendanceStatus.Absent);

        payroll.EmployeeContractId = contract.Id;
        payroll.HourlyRate = contract.HourlyRate > 0 ? contract.HourlyRate : ComputeHourlyRate(contract);
        payroll.WorkingHours = completedAttendances.Sum(x => x.WorkingMinutes) / 60m;
        payroll.BaseAmount = completedAttendances.Sum(x => GetRegularMinutes(x, contract) / 60m * payroll.HourlyRate);
        payroll.OvertimeAmount = completedAttendances.Sum(x => x.OvertimeMinutes / 60m * payroll.HourlyRate * contract.OvertimeRateMultiplier);
        payroll.AllowanceAmount = request.AllowanceAmount;
        payroll.BonusAmount = request.BonusAmount;
        payroll.PenaltyAmount = completedAttendances.Sum(x => x.LateMinutes * contract.LatePenaltyPerMinute + x.EarlyLeaveMinutes * contract.EarlyLeavePenaltyPerMinute) + request.PenaltyAmount + absentCount * payroll.HourlyRate * contract.StandardDailyHours;
        payroll.InsuranceAmount = request.InsuranceAmount;
        payroll.TaxAmount = request.TaxAmount;
        payroll.Note = request.Note;
        payroll.Status = PayrollStatus.Generated;
        payroll.ApprovedAt = null;
        payroll.ApprovedByUserAccountId = null;

        var details = BuildGeneratedDetails(payroll, completedAttendances, contract);
        _context.PayrollDetails.AddRange(details);
        await _context.SaveChangesAsync(cancellationToken);
        payroll.PayrollDetails = details;
        RecalculatePayrollFromDetails(payroll);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(payroll.Id, cancellationToken), null, null);
    }

    public async Task<string> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryPayrolls().AsNoTracking().OrderByDescending(x => x.PayrollYear).ThenByDescending(x => x.PayrollMonth).ToListAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("PayrollId,EmployeeCode,EmployeeName,Month,Year,BaseAmount,OvertimeAmount,AllowanceAmount,BonusAmount,PenaltyAmount,InsuranceAmount,TaxAmount,TotalSalary,Status");
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Id, EscapeCsv(row.Employee?.EmployeeCode), EscapeCsv(row.Employee?.FullName), row.PayrollMonth, row.PayrollYear, row.BaseAmount, row.OvertimeAmount, row.AllowanceAmount, row.BonusAmount, row.PenaltyAmount, row.InsuranceAmount, row.TaxAmount, row.TotalSalary, row.Status));
        }
        return builder.ToString();
    }

    private IQueryable<Payroll> QueryPayrolls()
    {
        return _context.Payrolls.Include(x => x.Employee).Include(x => x.EmployeeContract).Include(x => x.PayrollDetails);
    }

    private async Task<(string Error, int StatusCode)?> ValidatePayrollRequestAsync(PayrollRequestDto request, CancellationToken cancellationToken, int? payrollId = null)
    {
        if (request.EmployeeId <= 0 || request.EmployeeContractId <= 0 || request.PayrollMonth is < 1 or > 12 || request.PayrollYear < 2000)
            return ("Invalid payroll request.", StatusCodes.Status400BadRequest);
        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken);
        if (employee == null || !employee.IsActive) return ("Active employee not found.", StatusCodes.Status400BadRequest);
        var contract = await _context.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == request.EmployeeContractId && x.EmployeeId == request.EmployeeId, cancellationToken);
        if (contract == null || !contract.IsActive) return ("Active employee contract not found.", StatusCodes.Status400BadRequest);
        var payrollExists = await _context.Payrolls.AnyAsync(x => x.Id != payrollId && x.EmployeeId == request.EmployeeId && x.PayrollMonth == request.PayrollMonth && x.PayrollYear == request.PayrollYear, cancellationToken);
        return payrollExists ? ("Payroll already exists for this employee and period.", StatusCodes.Status409Conflict) : null;
    }

    private async Task<bool> IsPeriodClosedAsync(int payrollMonth, int payrollYear, CancellationToken cancellationToken, int? currentPayrollId = null)
    {
        if (await _context.PayrollClosePeriods.AnyAsync(x => x.PayrollMonth == payrollMonth && x.PayrollYear == payrollYear && x.IsClosed, cancellationToken))
            return true;
        return await _context.Payrolls.AnyAsync(x => x.Id != currentPayrollId && x.PayrollMonth == payrollMonth && x.PayrollYear == payrollYear && x.IsClosed, cancellationToken);
    }

    private static bool CanEditPayroll(Payroll payroll) => payroll.Status == PayrollStatus.Draft && !payroll.IsClosed;
    private static decimal ComputeHourlyRate(EmployeeContract contract) => contract.BaseSalary <= 0 || contract.StandardDailyHours <= 0 ? 0m : contract.BaseSalary / (contract.StandardDailyHours * 26m);
    private static decimal GetRegularMinutes(Attendance attendance, EmployeeContract contract) => Math.Min(attendance.WorkingMinutes, (int)(contract.StandardDailyHours * 60m));

    private static void RecalculatePayroll(Payroll payroll)
    {
        payroll.TotalSalary = payroll.BaseAmount + payroll.OvertimeAmount + payroll.AllowanceAmount + payroll.BonusAmount - payroll.PenaltyAmount - payroll.InsuranceAmount - payroll.TaxAmount;
    }

    private void RecalculatePayrollFromDetails(Payroll payroll)
    {
        var details = payroll.PayrollDetails.Where(x => !x.IsDeleted).ToList();
        payroll.AllowanceAmount = details.Where(x => x.DetailType == PayrollDetailType.Allowance).Sum(x => x.Amount);
        payroll.BonusAmount = details.Where(x => x.DetailType == PayrollDetailType.Bonus).Sum(x => x.Amount);
        payroll.OvertimeAmount = details.Where(x => x.DetailType == PayrollDetailType.Overtime).Sum(x => x.Amount);
        payroll.PenaltyAmount = details.Where(x => x.DetailType == PayrollDetailType.Penalty).Sum(x => x.Amount);
        payroll.InsuranceAmount = details.Where(x => x.DetailType == PayrollDetailType.Insurance).Sum(x => x.Amount);
        payroll.TaxAmount = details.Where(x => x.DetailType == PayrollDetailType.Tax).Sum(x => x.Amount);
        RecalculatePayroll(payroll);
    }

    private static List<PayrollDetail> BuildGeneratedDetails(Payroll payroll, IReadOnlyList<Attendance> attendances, EmployeeContract contract)
    {
        var details = new List<PayrollDetail>();
        foreach (var attendance in attendances)
        {
            if (attendance.OvertimeMinutes > 0)
                details.Add(new PayrollDetail { Payroll = payroll, AttendanceId = attendance.Id, DetailType = PayrollDetailType.Overtime, Description = $"Overtime on {attendance.AttendanceDate:yyyy-MM-dd}", Amount = attendance.OvertimeMinutes / 60m * payroll.HourlyRate * contract.OvertimeRateMultiplier });
            var penaltyAmount = attendance.LateMinutes * contract.LatePenaltyPerMinute + attendance.EarlyLeaveMinutes * contract.EarlyLeavePenaltyPerMinute;
            if (penaltyAmount > 0)
                details.Add(new PayrollDetail { Payroll = payroll, AttendanceId = attendance.Id, DetailType = PayrollDetailType.Penalty, Description = $"Attendance penalty on {attendance.AttendanceDate:yyyy-MM-dd}", Amount = penaltyAmount });
        }
        if (payroll.AllowanceAmount > 0) details.Add(new PayrollDetail { Payroll = payroll, DetailType = PayrollDetailType.Allowance, Description = "Allowance", Amount = payroll.AllowanceAmount });
        if (payroll.BonusAmount > 0) details.Add(new PayrollDetail { Payroll = payroll, DetailType = PayrollDetailType.Bonus, Description = "Bonus", Amount = payroll.BonusAmount });
        if (payroll.InsuranceAmount > 0) details.Add(new PayrollDetail { Payroll = payroll, DetailType = PayrollDetailType.Insurance, Description = "Insurance", Amount = payroll.InsuranceAmount });
        if (payroll.TaxAmount > 0) details.Add(new PayrollDetail { Payroll = payroll, DetailType = PayrollDetailType.Tax, Description = "Tax", Amount = payroll.TaxAmount });
        return details;
    }

    private static PayrollResponseDto MapPayroll(Payroll x, bool includeDetails)
    {
        return new PayrollResponseDto(
            x.Id, x.EmployeeId, x.EmployeeContractId, x.PayrollMonth, x.PayrollYear, x.BaseAmount, x.WorkingHours, x.HourlyRate,
            x.OvertimeAmount, x.AllowanceAmount, x.BonusAmount, x.PenaltyAmount, x.InsuranceAmount, x.TaxAmount, x.TotalSalary,
            (int)x.Status, x.PaidDate, x.ApprovedAt, x.ApprovedByUserAccountId, x.IsClosed, x.ClosedAt, x.ClosedByUserAccountId, x.Note,
            x.CreatedAt, x.UpdatedAt,
            x.Employee == null ? null : new PayrollEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive),
            x.EmployeeContract == null ? null : new PayrollContractDto(x.EmployeeContract.Id, x.EmployeeContract.ContractNo, (int)x.EmployeeContract.ContractType, x.EmployeeContract.StartDate, x.EmployeeContract.EndDate, x.EmployeeContract.BaseSalary, x.EmployeeContract.HourlyRate, x.EmployeeContract.OvertimeRateMultiplier, x.EmployeeContract.LatePenaltyPerMinute, x.EmployeeContract.EarlyLeavePenaltyPerMinute, x.EmployeeContract.StandardDailyHours, x.EmployeeContract.IsActive),
            includeDetails ? x.PayrollDetails.Where(d => !d.IsDeleted).Select(MapPayrollDetail).ToList() : null);
    }

    private static PayrollDetailResponseDto MapPayrollDetail(PayrollDetail x)
    {
        return new PayrollDetailResponseDto(x.Id, x.PayrollId, (int)x.DetailType, x.AttendanceId, x.ScheduleId, x.SourceReferenceId, x.Description, x.Amount, x.Note, x.CreatedAt, x.UpdatedAt);
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
