using System.Linq.Expressions;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EmployeeResponseDto? Employee, string? Error, int? StatusCode)> CreateAsync(EmployeeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;

    public EmployeeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmployeeResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(x => x.Branch)
            .Include(x => x.Role)
            .AsNoTracking()
            .OrderBy(x => x.EmployeeCode)
            .Select(MapEmployee(includeDetails: false))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(x => x.Branch)
            .Include(x => x.Role)
            .Include(x => x.EmployeeContracts)
            .Include(x => x.UserAccount)
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapEmployee(includeDetails: true))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(EmployeeResponseDto? Employee, string? Error, int? StatusCode)> CreateAsync(EmployeeUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(dto, null, cancellationToken);
        if (validationError is not null)
        {
            return (null, validationError.Value.Error, validationError.Value.StatusCode);
        }

        var entity = new Employee
        {
            EmployeeCode = dto.EmployeeCode.Trim(),
            FullName = dto.FullName.Trim(),
            Gender = Enum.IsDefined(typeof(GenderType), dto.Gender) ? (GenderType)dto.Gender : GenderType.Other,
            DateOfBirth = dto.DateOfBirth,
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
            BranchId = dto.BranchId,
            RoleId = dto.RoleId,
            HireDate = dto.HireDate,
            IsActive = dto.IsActive
        };

        _context.Employees.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Employee not found.", StatusCodes.Status404NotFound);
        }

        var validationError = await ValidateAsync(dto, id, cancellationToken);
        if (validationError is not null)
        {
            return (false, validationError.Value.Error, validationError.Value.StatusCode);
        }

        entity.EmployeeCode = dto.EmployeeCode.Trim();
        entity.FullName = dto.FullName.Trim();
        entity.Gender = Enum.IsDefined(typeof(GenderType), dto.Gender) ? (GenderType)dto.Gender : GenderType.Other;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        entity.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        entity.BranchId = dto.BranchId;
        entity.RoleId = dto.RoleId;
        entity.HireDate = dto.HireDate;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Employee not found.", StatusCodes.Status404NotFound);
        }

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(EmployeeUpsertDto dto, int? employeeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeCode))
        {
            return ("EmployeeCode is required.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return ("FullName is required.", StatusCodes.Status400BadRequest);
        }

        var employeeCode = dto.EmployeeCode.Trim();
        var duplicateEmployeeCode = await _context.Employees.AnyAsync(x => x.Id != employeeId && x.EmployeeCode == employeeCode, cancellationToken);
        if (duplicateEmployeeCode)
        {
            return ("EmployeeCode already exists.", StatusCodes.Status409Conflict);
        }

        if (!await _context.Branches.AnyAsync(x => x.Id == dto.BranchId && x.IsActive, cancellationToken))
        {
            return ("Active branch not found.", StatusCodes.Status400BadRequest);
        }

        if (!await _context.Roles.AnyAsync(x => x.Id == dto.RoleId && x.IsActive, cancellationToken))
        {
            return ("Active role not found.", StatusCodes.Status400BadRequest);
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var email = dto.Email.Trim();
            var duplicateEmail = await _context.Employees.AnyAsync(x => x.Id != employeeId && x.Email == email, cancellationToken);
            if (duplicateEmail)
            {
                return ("Email already exists.", StatusCodes.Status409Conflict);
            }
        }

        return null;
    }

    private static Expression<Func<Employee, EmployeeResponseDto>> MapEmployee(bool includeDetails)
    {
        return x => new EmployeeResponseDto(
            x.Id,
            x.EmployeeCode,
            x.FullName,
            (int)x.Gender,
            x.DateOfBirth,
            x.Phone,
            x.Email,
            x.Address,
            x.BranchId,
            x.RoleId,
            x.HireDate,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt,
            x.Branch == null
                ? null
                : new BranchLookupDto(x.Branch.Id, x.Branch.BranchCode, x.Branch.BranchName, x.Branch.IsActive),
            x.Role == null
                ? null
                : new RoleLookupDto(x.Role.Id, x.Role.RoleName, x.Role.IsActive),
            includeDetails
                ? x.EmployeeContracts
                    .OrderByDescending(c => c.StartDate)
                    .Select(c => new EmployeeContractLookupDto(c.Id, c.ContractNo, c.StartDate, c.EndDate, c.BaseSalary, c.HourlyRate, c.IsActive))
                    .ToList()
                : null,
            includeDetails && x.UserAccount != null
                ? new UserAccountLookupDto(x.UserAccount.Id, x.UserAccount.Username, x.UserAccount.IsActive, x.UserAccount.LastLoginAt)
                : null);
    }
}
