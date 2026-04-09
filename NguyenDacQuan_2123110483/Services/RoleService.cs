using System.Linq.Expressions;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(RoleResponseDto? Role, string? Error, int? StatusCode)> CreateAsync(RoleUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, RoleUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class RoleService : IRoleService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RoleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .OrderBy(x => x.RoleName)
            .Select(MapRole())
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapRole())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(RoleResponseDto? Role, string? Error, int? StatusCode)> CreateAsync(RoleUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null)
        {
            return (null, validation.Value.Error, validation.Value.StatusCode);
        }

        var entity = new Role
        {
            RoleName = dto.RoleName.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = dto.IsActive
        };

        _context.Roles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, RoleUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Role not found.", StatusCodes.Status404NotFound);
        }

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null)
        {
            return (false, validation.Value.Error, validation.Value.StatusCode);
        }

        entity.RoleName = dto.RoleName.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Roles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Role not found.", StatusCodes.Status404NotFound);
        }

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(RoleUpsertDto dto, int? roleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.RoleName))
        {
            return ("RoleName is required.", StatusCodes.Status400BadRequest);
        }

        var roleName = dto.RoleName.Trim();
        var duplicate = await _context.Roles.AnyAsync(x => x.Id != roleId && x.RoleName == roleName, cancellationToken);
        if (duplicate)
        {
            return ("RoleName already exists.", StatusCodes.Status409Conflict);
        }

        return null;
    }

    private static Expression<Func<Role, RoleResponseDto>> MapRole()
    {
        return x => new RoleResponseDto(
            x.Id,
            x.RoleName,
            x.Description,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt);
    }
}
