using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CoffeeHRM.Services;

public interface IBranchService
{
    Task<IReadOnlyList<BranchResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<BranchResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(BranchResponseDto? Branch, string? Error, int? StatusCode)> CreateAsync(BranchUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, BranchUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class BranchService : IBranchService
{
    private readonly AppDbContext _context;

    public BranchService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BranchResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .AsNoTracking()
            .OrderBy(x => x.BranchName)
            .Select(MapBranch())
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Branches
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapBranch())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(BranchResponseDto? Branch, string? Error, int? StatusCode)> CreateAsync(BranchUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateAsync(dto, null, cancellationToken);
        if (validationError is not null)
        {
            return (null, validationError.Value.Error, validationError.Value.StatusCode);
        }

        var entity = new Branch
        {
            BranchCode = dto.BranchCode.Trim(),
            BranchName = dto.BranchName.Trim(),
            Address = dto.Address.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            IsActive = dto.IsActive
        };

        _context.Branches.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, BranchUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Branch not found.", StatusCodes.Status404NotFound);
        }

        var validationError = await ValidateAsync(dto, id, cancellationToken);
        if (validationError is not null)
        {
            return (false, validationError.Value.Error, validationError.Value.StatusCode);
        }

        entity.BranchCode = dto.BranchCode.Trim();
        entity.BranchName = dto.BranchName.Trim();
        entity.Address = dto.Address.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Branch not found.", StatusCodes.Status404NotFound);
        }

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(BranchUpsertDto dto, int? branchId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.BranchCode))
        {
            return ("BranchCode is required.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(dto.BranchName))
        {
            return ("BranchName is required.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(dto.Address))
        {
            return ("Address is required.", StatusCodes.Status400BadRequest);
        }

        var branchCode = dto.BranchCode.Trim();
        var duplicateCode = await _context.Branches.AnyAsync(x => x.Id != branchId && x.BranchCode == branchCode, cancellationToken);
        if (duplicateCode)
        {
            return ("BranchCode already exists.", StatusCodes.Status409Conflict);
        }

        return null;
    }

    private static Expression<Func<Branch, BranchResponseDto>> MapBranch()
    {
        return x => new BranchResponseDto(
            x.Id,
            x.BranchCode,
            x.BranchName,
            x.Address,
            x.Phone,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt);
    }
}
