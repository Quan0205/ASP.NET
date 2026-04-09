using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface ITrainingService
{
    Task<IReadOnlyList<TrainingResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TrainingResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(TrainingResponseDto? Training, string? Error, int? StatusCode)> CreateAsync(TrainingUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, TrainingUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class TrainingService : ITrainingService
{
    private readonly AppDbContext _context;

    public TrainingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TrainingResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Trainings.Include(x => x.EmployeeTrainings).AsNoTracking().OrderBy(x => x.TrainingCode).ToListAsync(cancellationToken);
        return rows.Select(MapTraining).ToList();
    }

    public async Task<TrainingResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.Trainings.Include(x => x.EmployeeTrainings).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapTraining(row);
    }

    public async Task<(TrainingResponseDto? Training, string? Error, int? StatusCode)> CreateAsync(TrainingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var entity = new Training
        {
            TrainingCode = dto.TrainingCode.Trim(),
            TrainingName = dto.TrainingName.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Instructor = string.IsNullOrWhiteSpace(dto.Instructor) ? null : dto.Instructor.Trim(),
            IsRequired = dto.IsRequired,
            IsActive = dto.IsActive
        };
        _context.Trainings.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, TrainingUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Training not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        entity.TrainingCode = dto.TrainingCode.Trim();
        entity.TrainingName = dto.TrainingName.Trim();
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Instructor = string.IsNullOrWhiteSpace(dto.Instructor) ? null : dto.Instructor.Trim();
        entity.IsRequired = dto.IsRequired;
        entity.IsActive = dto.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Trainings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Training not found.", StatusCodes.Status404NotFound);
        entity.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(TrainingUpsertDto dto, int? trainingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.TrainingCode)) return ("TrainingCode is required.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(dto.TrainingName)) return ("TrainingName is required.", StatusCodes.Status400BadRequest);
        if (dto.EndDate.HasValue && dto.EndDate.Value.Date < dto.StartDate.Date) return ("EndDate cannot be earlier than StartDate.", StatusCodes.Status400BadRequest);
        var code = dto.TrainingCode.Trim();
        var duplicate = await _context.Trainings.AnyAsync(x => x.Id != trainingId && x.TrainingCode == code, cancellationToken);
        return duplicate ? ("TrainingCode already exists.", StatusCodes.Status409Conflict) : null;
    }

    private static TrainingResponseDto MapTraining(Training x)
    {
        return new TrainingResponseDto(
            x.Id,
            x.TrainingCode,
            x.TrainingName,
            x.Description,
            x.StartDate,
            x.EndDate,
            x.Instructor,
            x.IsRequired,
            x.IsActive,
            x.CreatedAt,
            x.UpdatedAt,
            x.EmployeeTrainings?.Select(et => new TrainingEmployeeTrainingDto(et.Id, et.EmployeeId, (int)et.Status, et.Score)).ToList());
    }
}
