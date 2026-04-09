using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IUserAccountService
{
    Task<IReadOnlyList<UserAccountResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserAccountResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(UserAccountResponseDto? UserAccount, string? Error, int? StatusCode)> CreateAsync(UserAccountUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, UserAccountUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class UserAccountService : IUserAccountService
{
    private readonly AppDbContext _context;

    public UserAccountService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserAccountResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await Query().AsNoTracking().OrderBy(x => x.Username).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<UserAccountResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : Map(row);
    }

    public async Task<(UserAccountResponseDto? UserAccount, string? Error, int? StatusCode)> CreateAsync(UserAccountUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, requirePassword: true, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var systemRoleId = await ResolveSystemRoleIdAsync(dto.SystemRoleId, dto.RoleId, cancellationToken);
        if (systemRoleId == null) return (null, "SystemRole not found.", StatusCodes.Status400BadRequest);

        var entity = new UserAccount
        {
            EmployeeId = dto.EmployeeId,
            RoleId = dto.RoleId,
            SystemRoleId = systemRoleId,
            Username = dto.Username.Trim(),
            PasswordHash = PasswordHashHelper.Hash(dto.Password!.Trim()),
            IsActive = dto.IsActive,
            LastLoginAt = dto.LastLoginAt
        };

        _context.UserAccounts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, UserAccountUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "User account not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, requirePassword: false, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        var systemRoleId = await ResolveSystemRoleIdAsync(dto.SystemRoleId, dto.RoleId, cancellationToken);
        if (systemRoleId == null) return (false, "SystemRole not found.", StatusCodes.Status400BadRequest);

        entity.EmployeeId = dto.EmployeeId;
        entity.RoleId = dto.RoleId;
        entity.SystemRoleId = systemRoleId;
        entity.Username = dto.Username.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Password))
            entity.PasswordHash = PasswordHashHelper.Hash(dto.Password.Trim());
        entity.IsActive = dto.IsActive;
        entity.LastLoginAt = dto.LastLoginAt;

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "User account not found.", StatusCodes.Status404NotFound);
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private IQueryable<UserAccount> Query()
    {
        return _context.UserAccounts.Include(x => x.Employee).Include(x => x.Role).Include(x => x.SystemRole);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(UserAccountUpsertDto dto, int? userAccountId, bool requirePassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Username)) return ("Username is required.", StatusCodes.Status400BadRequest);
        if (dto.EmployeeId <= 0) return ("EmployeeId is required.", StatusCodes.Status400BadRequest);
        if (dto.RoleId <= 0) return ("RoleId is required.", StatusCodes.Status400BadRequest);
        if (requirePassword && string.IsNullOrWhiteSpace(dto.Password)) return ("Password is required.", StatusCodes.Status400BadRequest);
        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId && x.IsActive, cancellationToken)) return ("Active employee not found.", StatusCodes.Status400BadRequest);
        if (!await _context.Roles.AnyAsync(x => x.Id == dto.RoleId && x.IsActive, cancellationToken)) return ("Active role not found.", StatusCodes.Status400BadRequest);
        var username = dto.Username.Trim();
        if (await _context.UserAccounts.AnyAsync(x => x.Id != userAccountId && x.Username == username, cancellationToken)) return ("Username already exists.", StatusCodes.Status409Conflict);
        if (await _context.UserAccounts.AnyAsync(x => x.Id != userAccountId && x.EmployeeId == dto.EmployeeId, cancellationToken)) return ("Employee already has a user account.", StatusCodes.Status409Conflict);
        return null;
    }

    private async Task<int?> ResolveSystemRoleIdAsync(int? requestedSystemRoleId, int jobRoleId, CancellationToken cancellationToken)
    {
        if (requestedSystemRoleId.HasValue && requestedSystemRoleId.Value > 0)
        {
            var exists = await _context.SystemRoles.AnyAsync(x => x.Id == requestedSystemRoleId.Value && x.IsActive, cancellationToken);
            return exists ? requestedSystemRoleId.Value : null;
        }

        var jobRole = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobRoleId, cancellationToken);
        if (jobRole == null) return null;
        var inferredCode = InferSystemRoleCode(jobRole.RoleName);
        var inferredRole = await _context.SystemRoles.FirstOrDefaultAsync(x => x.Code == inferredCode && x.IsActive, cancellationToken);
        return inferredRole?.Id;
    }

    private static string InferSystemRoleCode(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return "EMPLOYEE";
        var normalized = roleName.Trim().ToLowerInvariant();
        if (normalized.Contains("admin") || normalized.Contains("quản trị") || normalized.Contains("quan tri")) return "ADMIN";
        if (normalized.Contains("hr") || normalized.Contains("nhân sự") || normalized.Contains("nhan su") || normalized.Contains("tuyển")) return "HR";
        if (normalized.Contains("manager") || normalized.Contains("quản lý") || normalized.Contains("quan ly")) return "MANAGER";
        return "EMPLOYEE";
    }

    private static UserAccountResponseDto Map(UserAccount x)
    {
        return new UserAccountResponseDto(
            x.Id,
            x.EmployeeId,
            x.RoleId,
            x.SystemRoleId,
            x.Username,
            x.IsActive,
            x.LastLoginAt,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null ? null : new UserAccountEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive),
            x.Role == null ? null : new UserAccountRoleDto(x.Role.Id, x.Role.RoleName, x.Role.IsActive),
            x.SystemRole == null ? null : new UserAccountSystemRoleDto(x.SystemRole.Id, x.SystemRole.Code, x.SystemRole.Name, x.SystemRole.IsActive));
    }
}
