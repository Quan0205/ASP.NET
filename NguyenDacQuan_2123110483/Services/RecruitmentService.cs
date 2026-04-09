using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IRecruitmentService
{
    Task<IReadOnlyList<RecruitmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RecruitmentResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(RecruitmentResponseDto? Recruitment, string? Error, int? StatusCode)> CreateAsync(RecruitmentUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, RecruitmentUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _context;

    public RecruitmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RecruitmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Recruitments.Include(x => x.Branch).AsNoTracking().OrderByDescending(x => x.OpenDate).ToListAsync(cancellationToken);
        return rows.Select(x => MapRecruitment(x, null)).ToList();
    }

    public async Task<RecruitmentResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.Recruitments.Include(x => x.Branch).Include(x => x.Candidates).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapRecruitment(row, row.Candidates);
    }

    public async Task<(RecruitmentResponseDto? Recruitment, string? Error, int? StatusCode)> CreateAsync(RecruitmentUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var entity = new Recruitment
        {
            BranchId = dto.BranchId,
            PositionTitle = dto.PositionTitle.Trim(),
            OpenDate = dto.OpenDate,
            CloseDate = dto.CloseDate,
            Status = (RecruitmentStatus)dto.Status,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim()
        };

        _context.Recruitments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, RecruitmentUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Recruitment not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        entity.BranchId = dto.BranchId;
        entity.PositionTitle = dto.PositionTitle.Trim();
        entity.OpenDate = dto.OpenDate;
        entity.CloseDate = dto.CloseDate;
        entity.Status = (RecruitmentStatus)dto.Status;
        entity.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Recruitment not found.", StatusCodes.Status404NotFound);

        entity.Status = RecruitmentStatus.Cancelled;
        entity.CloseDate ??= DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(RecruitmentUpsertDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.PositionTitle))
            return ("PositionTitle is required.", StatusCodes.Status400BadRequest);
        if (!Enum.IsDefined(typeof(RecruitmentStatus), dto.Status))
            return ("Invalid recruitment status.", StatusCodes.Status400BadRequest);
        if (dto.BranchId.HasValue && !await _context.Branches.AnyAsync(x => x.Id == dto.BranchId.Value && x.IsActive, cancellationToken))
            return ("Active branch not found.", StatusCodes.Status400BadRequest);
        if (dto.CloseDate.HasValue && dto.CloseDate.Value.Date < dto.OpenDate.Date)
            return ("CloseDate cannot be earlier than OpenDate.", StatusCodes.Status400BadRequest);
        return null;
    }

    private static RecruitmentResponseDto MapRecruitment(Recruitment x, IEnumerable<Candidate>? candidates)
    {
        return new RecruitmentResponseDto(
            x.Id,
            x.BranchId,
            x.PositionTitle,
            x.OpenDate,
            x.CloseDate,
            (int)x.Status,
            x.Description,
            x.CreatedAt,
            x.UpdatedAt,
            x.Branch == null ? null : new RecruitmentBranchDto(x.Branch.Id, x.Branch.BranchCode, x.Branch.BranchName, x.Branch.IsActive),
            candidates?.Select(c => new CandidateSummaryDto(c.Id, c.FullName, c.Phone, c.Email, c.AppliedDate, (int)c.Status, c.InterviewScore, c.Note)).ToList());
    }
}
