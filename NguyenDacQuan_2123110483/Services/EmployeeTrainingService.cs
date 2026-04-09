using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IEmployeeTrainingService
{
    Task<IReadOnlyList<EmployeeTrainingResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeTrainingResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(EmployeeTrainingResponseDto? EmployeeTraining, string? Error, int? StatusCode)> CreateAsync(EmployeeTrainingUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeTrainingUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class EmployeeTrainingService : IEmployeeTrainingService
{
    private readonly AppDbContext _context;

    public EmployeeTrainingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmployeeTrainingResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.EmployeeTrainings.Include(x => x.Employee).Include(x => x.Training).AsNoTracking().OrderByDescending(x => x.AssignedDate).ToListAsync(cancellationToken);
        return rows.Select(MapEmployeeTraining).ToList();
    }

    public async Task<EmployeeTrainingResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.EmployeeTrainings.Include(x => x.Employee).Include(x => x.Training).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapEmployeeTraining(row);
    }

    public async Task<(EmployeeTrainingResponseDto? EmployeeTraining, string? Error, int? StatusCode)> CreateAsync(EmployeeTrainingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var entity = new EmployeeTraining
        {
            EmployeeId = dto.EmployeeId,
            TrainingId = dto.TrainingId,
            AssignedDate = dto.AssignedDate,
            CompletedDate = dto.CompletedDate,
            Status = (EmployeeTrainingStatus)dto.Status,
            Score = dto.Score
        };

        _context.EmployeeTrainings.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, EmployeeTrainingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.EmployeeTrainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Employee training not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        entity.EmployeeId = dto.EmployeeId;
        entity.TrainingId = dto.TrainingId;
        entity.AssignedDate = dto.AssignedDate;
        entity.CompletedDate = dto.CompletedDate;
        entity.Status = (EmployeeTrainingStatus)dto.Status;
        entity.Score = dto.Score;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.EmployeeTrainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Employee training not found.", StatusCodes.Status404NotFound);
        _context.EmployeeTrainings.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(EmployeeTrainingUpsertDto dto, int? employeeTrainingId, CancellationToken cancellationToken)
    {
        if (dto.EmployeeId <= 0) return ("EmployeeId is required.", StatusCodes.Status400BadRequest);
        if (dto.TrainingId <= 0) return ("TrainingId is required.", StatusCodes.Status400BadRequest);
        if (!Enum.IsDefined(typeof(EmployeeTrainingStatus), dto.Status)) return ("Invalid employee training status.", StatusCodes.Status400BadRequest);
        if (dto.CompletedDate.HasValue && dto.CompletedDate.Value < dto.AssignedDate) return ("CompletedDate cannot be earlier than AssignedDate.", StatusCodes.Status400BadRequest);
        if (!await _context.Employees.AnyAsync(x => x.Id == dto.EmployeeId && x.IsActive, cancellationToken)) return ("Active employee not found.", StatusCodes.Status400BadRequest);
        if (!await _context.Trainings.AnyAsync(x => x.Id == dto.TrainingId && x.IsActive, cancellationToken)) return ("Active training not found.", StatusCodes.Status400BadRequest);
        var duplicate = await _context.EmployeeTrainings.AnyAsync(x => x.Id != employeeTrainingId && x.EmployeeId == dto.EmployeeId && x.TrainingId == dto.TrainingId, cancellationToken);
        return duplicate ? ("Employee training already exists.", StatusCodes.Status409Conflict) : null;
    }

    private static EmployeeTrainingResponseDto MapEmployeeTraining(EmployeeTraining x)
    {
        return new EmployeeTrainingResponseDto(
            x.Id,
            x.EmployeeId,
            x.TrainingId,
            x.AssignedDate,
            x.CompletedDate,
            (int)x.Status,
            x.Score,
            x.CreatedAt,
            x.UpdatedAt,
            x.Employee == null ? null : new EmployeeTrainingEmployeeDto(x.Employee.Id, x.Employee.EmployeeCode, x.Employee.FullName, x.Employee.IsActive),
            x.Training == null ? null : new EmployeeTrainingTrainingDto(x.Training.Id, x.Training.TrainingCode, x.Training.TrainingName, x.Training.IsActive));
    }
}
