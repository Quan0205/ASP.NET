using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IKpiService
{
    Task<IReadOnlyList<KpiResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<KpiResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(KpiResponseDto? Kpi, string? Error, int? StatusCode)> CreateAsync(KpiUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, KpiUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class KpiService : IKpiService
{
    private readonly AppDbContext _context;

    public KpiService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<KpiResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.KPIs.Include(x => x.Employee).AsNoTracking().OrderByDescending(x => x.KpiYear).ThenByDescending(x => x.KpiMonth).ToListAsync(cancellationToken);
        return rows.Select(MapKpi).ToList();
    }

    public async Task<KpiResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.KPIs.Include(x => x.Employee).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapKpi(row);
    }

    public async Task<(KpiResponseDto? Kpi, string? Error, int? StatusCode)> CreateAsync(KpiUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var entity = new KPI
        {
            EmployeeId = dto.EmployeeId,
            KpiYear = dto.KpiYear,
            KpiMonth = dto.KpiMonth,
            Score = dto.Score,
            Target = dto.Target,
            Result = dto.Result.Trim(),
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
        };

        _context.KPIs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, KpiUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.KPIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "KPI not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        entity.EmployeeId = dto.EmployeeId;
        entity.KpiYear = dto.KpiYear;
        entity.KpiMonth = dto.KpiMonth;
        entity.Score = dto.Score;
        entity.Target = dto.Target;
        entity.Result = dto.Result.Trim();
        entity.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.KPIs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "KPI not found.", StatusCodes.Status404NotFound);
        _context.KPIs.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(KpiUpsertDto dto, int? kpiId, CancellationToken cancellationToken)
    {
        if (dto.EmployeeId <= 0) return ("EmployeeId is required.", StatusCodes.Status400BadRequest);
        if (dto.KpiMonth is < 1 or > 12) return ("KpiMonth must be between 1 and 12.", StatusCodes.Status400BadRequest);
        if (dto.KpiYear is < 2000 or > 2100) return ("KpiYear is invalid.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(dto.Result)) return ("Result is required.", StatusCodes.Status400BadRequest);
        if (dto.Score < 0) return ("Score must be non-negative.", StatusCodes.Status400BadRequest);
        if (dto.Target < 0) return ("Target must be non-negative.", StatusCodes.Status400BadRequest);
        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId && x.IsActive, cancellationToken)) return ("Active employee not found.", StatusCodes.Status400BadRequest);
        var duplicate = await _context.KPIs.AnyAsync(x => x.Id != kpiId && x.EmployeeId == dto.EmployeeId && x.KpiMonth == dto.KpiMonth && x.KpiYear == dto.KpiYear, cancellationToken);
        return duplicate ? ("KPI already exists for this employee and period.", StatusCodes.Status409Conflict) : null;
    }

    private static KpiResponseDto MapKpi(KPI x)
    {
        return new KpiResponseDto(
            x.Id,
            x.EmployeeId,
            x.KpiYear,
            x.KpiMonth,
            x.Score,
            x.Target,
            x.Result,
            x.Note,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null ? null : new KpiEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive));
    }
}
