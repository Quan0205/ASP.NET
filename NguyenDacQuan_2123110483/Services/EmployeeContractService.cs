using System.Linq.Expressions;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IEmployeeContractService
{
    Task<IReadOnlyList<EmployeeContractResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeContractResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EmployeeContractResponseDto? Contract, string? Error, int? StatusCode)> CreateAsync(EmployeeContractUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeContractUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EmployeeContractService : IEmployeeContractService
{
    private readonly AppDbContext _context;

    public EmployeeContractService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmployeeContractResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeContracts
            .Include(x => x.Employee)
            .AsNoTracking()
            .OrderByDescending(x => x.StartDate)
            .Select(MapContract())
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeContractResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeContracts
            .Include(x => x.Employee)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapContract())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(EmployeeContractResponseDto? Contract, string? Error, int? StatusCode)> CreateAsync(EmployeeContractUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null)
        {
            return (null, validation.Value.Error, validation.Value.StatusCode);
        }

        var entity = new EmployeeContract
        {
            ContractNo = dto.ContractNo.Trim(),
            ContractType = Enum.IsDefined(typeof(ContractType), dto.ContractType) ? (ContractType)dto.ContractType : ContractType.FullTime,
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            BaseSalary = dto.BaseSalary,
            HourlyRate = dto.HourlyRate,
            OvertimeRateMultiplier = dto.OvertimeRateMultiplier,
            LatePenaltyPerMinute = dto.LatePenaltyPerMinute,
            EarlyLeavePenaltyPerMinute = dto.EarlyLeavePenaltyPerMinute,
            StandardDailyHours = dto.StandardDailyHours,
            IsActive = dto.IsActive
        };

        _context.EmployeeContracts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeContractUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Employee contract not found.", StatusCodes.Status404NotFound);
        }

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null)
        {
            return (false, validation.Value.Error, validation.Value.StatusCode);
        }

        entity.ContractNo = dto.ContractNo.Trim();
        entity.ContractType = Enum.IsDefined(typeof(ContractType), dto.ContractType) ? (ContractType)dto.ContractType : ContractType.FullTime;
        entity.EmployeeId = dto.EmployeeId;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.BaseSalary = dto.BaseSalary;
        entity.HourlyRate = dto.HourlyRate;
        entity.OvertimeRateMultiplier = dto.OvertimeRateMultiplier;
        entity.LatePenaltyPerMinute = dto.LatePenaltyPerMinute;
        entity.EarlyLeavePenaltyPerMinute = dto.EarlyLeavePenaltyPerMinute;
        entity.StandardDailyHours = dto.StandardDailyHours;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.EmployeeContracts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Employee contract not found.", StatusCodes.Status404NotFound);
        }

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(EmployeeContractUpsertDto dto, int? contractId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.ContractNo))
        {
            return ("ContractNo is required.", StatusCodes.Status400BadRequest);
        }

        if (dto.EmployeeId <= 0)
        {
            return ("EmployeeId is required.", StatusCodes.Status400BadRequest);
        }

        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date)
        {
            return ("EndDate cannot be earlier than StartDate.", StatusCodes.Status400BadRequest);
        }

        if (dto.BaseSalary < 0)
        {
            return ("BaseSalary must be non-negative.", StatusCodes.Status400BadRequest);
        }

        if (dto.HourlyRate < 0)
        {
            return ("HourlyRate must be non-negative.", StatusCodes.Status400BadRequest);
        }

        if (dto.OvertimeRateMultiplier <= 0)
        {
            return ("OvertimeRateMultiplier must be greater than 0.", StatusCodes.Status400BadRequest);
        }

        if (dto.LatePenaltyPerMinute < 0)
        {
            return ("LatePenaltyPerMinute must be non-negative.", StatusCodes.Status400BadRequest);
        }

        if (dto.EarlyLeavePenaltyPerMinute < 0)
        {
            return ("EarlyLeavePenaltyPerMinute must be non-negative.", StatusCodes.Status400BadRequest);
        }

        if (dto.StandardDailyHours <= 0)
        {
            return ("StandardDailyHours must be greater than 0.", StatusCodes.Status400BadRequest);
        }

        var employee = await _context.Employees.FirstOrDefaultAsync(x => x.Id == dto.EmployeeId, cancellationToken);
        if (employee == null || !employee.IsActive)
        {
            return ("Active employee not found.", StatusCodes.Status400BadRequest);
        }

        var contractNo = dto.ContractNo.Trim();
        var duplicateContractNo = await _context.EmployeeContracts.AnyAsync(x => x.Id != contractId && x.ContractNo == contractNo, cancellationToken);
        if (duplicateContractNo)
        {
            return ("ContractNo already exists.", StatusCodes.Status409Conflict);
        }

        var overlapExists = await HasOverlappingActiveContractAsync(dto.EmployeeId, dto.StartDate, dto.EndDate, contractId, cancellationToken);
        if (overlapExists && dto.IsActive)
        {
            return ("Employee already has an active contract in the selected period.", StatusCodes.Status400BadRequest);
        }

        return null;
    }

    private async Task<bool> HasOverlappingActiveContractAsync(int employeeId, DateTime startDate, DateTime? endDate, int? currentContractId, CancellationToken cancellationToken)
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

    private static Expression<Func<EmployeeContract, EmployeeContractResponseDto>> MapContract()
    {
        return x => new EmployeeContractResponseDto(
            x.Id,
            x.ContractNo,
            (int)x.ContractType,
            x.EmployeeId,
            x.StartDate,
            x.EndDate,
            x.BaseSalary,
            x.HourlyRate,
            x.OvertimeRateMultiplier,
            x.LatePenaltyPerMinute,
            x.EarlyLeavePenaltyPerMinute,
            x.StandardDailyHours,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null
                ? null
                : new EmployeeContractEmployeeDto(
                    x.Employee.Id,
                    x.Employee.EmployeeCode,
                    x.Employee.FullName,
                    x.Employee.IsActive));
    }
}
