using System.Linq.Expressions;
using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ShiftResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(ShiftResponseDto? Shift, string? Error, int? StatusCode)> CreateAsync(ShiftUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, ShiftUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ShiftService : IShiftService
{
    private readonly AppDbContext _context;

    public ShiftService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShiftResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Shifts
            .AsNoTracking()
            .OrderBy(x => x.ShiftCode)
            .Select(MapShift())
            .ToListAsync(cancellationToken);
    }

    public async Task<ShiftResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Shifts
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(MapShift())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(ShiftResponseDto? Shift, string? Error, int? StatusCode)> CreateAsync(ShiftUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null)
        {
            return (null, validation.Value.Error, validation.Value.StatusCode);
        }

        var entity = new Shift
        {
            ShiftCode = dto.ShiftCode.Trim(),
            ShiftName = dto.ShiftName.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            GraceMinutes = dto.GraceMinutes,
            IsActive = dto.IsActive
        };

        _context.Shifts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, ShiftUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Shift not found.", StatusCodes.Status404NotFound);
        }

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null)
        {
            return (false, validation.Value.Error, validation.Value.StatusCode);
        }

        entity.ShiftCode = dto.ShiftCode.Trim();
        entity.ShiftName = dto.ShiftName.Trim();
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.GraceMinutes = dto.GraceMinutes;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Shifts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
        {
            return (false, "Shift not found.", StatusCodes.Status404NotFound);
        }

        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(ShiftUpsertDto dto, int? shiftId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.ShiftCode))
        {
            return ("ShiftCode is required.", StatusCodes.Status400BadRequest);
        }

        if (string.IsNullOrWhiteSpace(dto.ShiftName))
        {
            return ("ShiftName is required.", StatusCodes.Status400BadRequest);
        }

        if (dto.StartTime == dto.EndTime)
        {
            return ("Shift StartTime and EndTime cannot be the same.", StatusCodes.Status400BadRequest);
        }

        if (dto.GraceMinutes < 0)
        {
            return ("GraceMinutes must be non-negative.", StatusCodes.Status400BadRequest);
        }

        var shiftCode = dto.ShiftCode.Trim();
        var duplicate = await _context.Shifts.AnyAsync(x => x.Id != shiftId && x.ShiftCode == shiftCode, cancellationToken);
        if (duplicate)
        {
            return ("ShiftCode already exists.", StatusCodes.Status409Conflict);
        }

        return null;
    }

    private static Expression<Func<Shift, ShiftResponseDto>> MapShift()
    {
        return x => new ShiftResponseDto(
            x.Id,
            x.ShiftCode,
            x.ShiftName,
            x.StartTime,
            x.EndTime,
            x.GraceMinutes,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt);
    }
}
