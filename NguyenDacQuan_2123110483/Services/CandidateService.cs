using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface ICandidateService
{
    Task<IReadOnlyList<CandidateResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CandidateResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(CandidateResponseDto? Candidate, string? Error, int? StatusCode)> CreateAsync(CandidateUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, CandidateUpsertDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class CandidateService : ICandidateService
{
    private readonly AppDbContext _context;

    public CandidateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CandidateResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Candidates.Include(x => x.Recruitment).AsNoTracking().OrderByDescending(x => x.AppliedDate).ToListAsync(cancellationToken);
        return rows.Select(MapCandidate).ToList();
    }

    public async Task<CandidateResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.Candidates.Include(x => x.Recruitment).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : MapCandidate(row);
    }

    public async Task<(CandidateResponseDto? Candidate, string? Error, int? StatusCode)> CreateAsync(CandidateUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, cancellationToken);
        if (validation is not null) return (null, validation.Value.Error, validation.Value.StatusCode);

        var entity = new Candidate
        {
            RecruitmentId = dto.RecruitmentId,
            FullName = dto.FullName.Trim(),
            Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            AppliedDate = dto.AppliedDate,
            Status = (CandidateStatus)dto.Status,
            InterviewScore = dto.InterviewScore,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
        };

        _context.Candidates.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken), null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> UpdateAsync(int id, CandidateUpsertDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Candidate not found.", StatusCodes.Status404NotFound);

        var validation = await ValidateAsync(dto, id, cancellationToken);
        if (validation is not null) return (false, validation.Value.Error, validation.Value.StatusCode);

        entity.RecruitmentId = dto.RecruitmentId;
        entity.FullName = dto.FullName.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        entity.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        entity.AppliedDate = dto.AppliedDate;
        entity.Status = (CandidateStatus)dto.Status;
        entity.InterviewScore = dto.InterviewScore;
        entity.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return (false, "Candidate not found.", StatusCodes.Status404NotFound);
        _context.Candidates.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    private async Task<(string Error, int StatusCode)?> ValidateAsync(CandidateUpsertDto dto, int? candidateId, CancellationToken cancellationToken)
    {
        if (dto.RecruitmentId <= 0) return ("RecruitmentId is required.", StatusCodes.Status400BadRequest);
        if (string.IsNullOrWhiteSpace(dto.FullName)) return ("FullName is required.", StatusCodes.Status400BadRequest);
        if (!Enum.IsDefined(typeof(CandidateStatus), dto.Status)) return ("Invalid candidate status.", StatusCodes.Status400BadRequest);

        var recruitment = await _context.Recruitments.FirstOrDefaultAsync(x => x.Id == dto.RecruitmentId, cancellationToken);
        if (recruitment == null) return ("Recruitment not found.", StatusCodes.Status400BadRequest);
        if (recruitment.Status == RecruitmentStatus.Closed || recruitment.Status == RecruitmentStatus.Cancelled) return ("Recruitment is closed.", StatusCodes.Status400BadRequest);

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (email != null)
        {
            var duplicateEmail = await _context.Candidates.AnyAsync(x => x.Id != candidateId && x.RecruitmentId == dto.RecruitmentId && x.Email == email, cancellationToken);
            if (duplicateEmail) return ("Candidate email already exists in this recruitment.", StatusCodes.Status409Conflict);
        }

        return null;
    }

    private static CandidateResponseDto MapCandidate(Candidate x)
    {
        return new CandidateResponseDto(
            x.Id,
            x.RecruitmentId,
            x.FullName,
            x.Phone,
            x.Email,
            x.AppliedDate,
            (int)x.Status,
            x.InterviewScore,
            x.Note,
            x.CreatedAt,
            x.UpdatedAt,
            x.Recruitment == null ? null : new CandidateRecruitmentDto(x.Recruitment.Id, x.Recruitment.PositionTitle, (int)x.Recruitment.Status));
    }
}
