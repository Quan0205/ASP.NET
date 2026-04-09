using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface ISystemRoleService
{
    Task<IReadOnlyList<SystemRoleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemRoleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(SystemRoleResponseDto? SystemRole, string? Error, int? StatusCode)> CreateAsync(SystemRoleUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, SystemRoleUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class SystemRoleService : ISystemRoleService
{
    private readonly AppDbContext _context;

    public SystemRoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SystemRoleResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await Query().AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<SystemRoleResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : Map(row);
    }

    public async Task<(SystemRoleResponseDto? SystemRole, string? Error, int? StatusCode)> CreateAsync(SystemRoleUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var permissionIds = await ResolvePermissionIdsAsync(dto.PermissionIds, cancellationToken);
        if (permissionIds == null) return (null, "One or more permissions were not found.", StatusCodes.Status400BadRequest);

        var entity = new SystemRole
        {
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            IsActive = dto.IsActive
        };
        _context.SystemRoles.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await SyncPermissionsAsync(entity.Id, permissionIds, cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, SystemRoleUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SystemRoles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "SystemRole not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        var permissionIds = await ResolvePermissionIdsAsync(dto.PermissionIds, cancellationToken);
        if (permissionIds == null) return (false, "One or more permissions were not found.", StatusCodes.Status400BadRequest);

        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        await SyncPermissionsAsync(entity.Id, permissionIds, cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SystemRoles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "SystemRole not found.", StatusCodes.Status404NotFound);
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private IQueryable<SystemRole> Query()
    {
        return _context.SystemRoles.Include(x => x.SystemRolePermissions).ThenInclude(x => x.Permission);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(SystemRoleUpsertDto dto, int? systemRoleId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return ("Code and Name are required.", StatusCodes.Status400BadRequest);
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (await _context.SystemRoles.AnyAsync(x => x.Id != systemRoleId && x.Code == code, cancellationToken))
            return ("Code already exists.", StatusCodes.Status409Conflict);
        if (await _context.SystemRoles.AnyAsync(x => x.Id != systemRoleId && x.Name == name, cancellationToken))
            return ("Name already exists.", StatusCodes.Status409Conflict);
        return null;
    }

    private async Task<List<int>?> ResolvePermissionIdsAsync(IReadOnlyList<int>? permissionIds, CancellationToken cancellationToken)
    {
        var requestedIds = permissionIds?.Where(x => x > 0).Distinct().ToList() ?? [];
        if (requestedIds.Count == 0) return [];
        var existingIds = await _context.Permissions.Where(x => requestedIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(cancellationToken);
        return existingIds.Count == requestedIds.Count ? requestedIds : null;
    }

    private async Task SyncPermissionsAsync(int systemRoleId, IReadOnlyCollection<int> permissionIds, CancellationToken cancellationToken)
    {
        var currentLinks = await _context.SystemRolePermissions.Where(x => x.SystemRoleId == systemRoleId).ToListAsync(cancellationToken);
        var toRemove = currentLinks.Where(x => !permissionIds.Contains(x.PermissionId)).ToList();
        if (toRemove.Count > 0) _context.SystemRolePermissions.RemoveRange(toRemove);
        var currentIds = currentLinks.Select(x => x.PermissionId).ToHashSet();
        var toAdd = permissionIds.Where(x => !currentIds.Contains(x)).Select(x => new SystemRolePermission { SystemRoleId = systemRoleId, PermissionId = x }).ToList();
        if (toAdd.Count > 0) _context.SystemRolePermissions.AddRange(toAdd);
        if (toRemove.Count > 0 || toAdd.Count > 0) await _context.SaveChangesAsync(cancellationToken);
    }

    private static SystemRoleResponseDto Map(SystemRole x)
    {
        var permissions = x.SystemRolePermissions
            .Where(p => p.Permission != null)
            .Select(p => new SystemRolePermissionDto(p.PermissionId, p.Permission!.Code, p.Permission.Name, p.Permission.Description))
            .OrderBy(p => p.Name)
            .ToList();

        return new SystemRoleResponseDto(
            x.Id,
            x.Code,
            x.Name,
            x.Description,
            x.IsActive,
            permissions.Select(p => p.PermissionId).ToList(),
            permissions,
            x.CreatedAt,
            x.UpdatedAt);
    }
}
