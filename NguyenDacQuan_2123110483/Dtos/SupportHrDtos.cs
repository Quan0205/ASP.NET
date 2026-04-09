namespace CoffeeHRM.Dtos;

public sealed record RecruitmentBranchDto(
    int Id,
    string BranchCode,
    string BranchName,
    bool IsActive);

public sealed record CandidateSummaryDto(
    int Id,
    string FullName,
    string? Phone,
    string? Email,
    DateTime AppliedDate,
    int Status,
    decimal? InterviewScore,
    string? Note);

public sealed record RecruitmentResponseDto(
    int Id,
    int? BranchId,
    string PositionTitle,
    DateTime OpenDate,
    DateTime? CloseDate,
    int Status,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    RecruitmentBranchDto? Branch,
    IReadOnlyList<CandidateSummaryDto>? Candidates);

public sealed record RecruitmentUpsertDto(
    int? BranchId,
    string PositionTitle,
    DateTime OpenDate,
    DateTime? CloseDate,
    int Status,
    string? Description);

public sealed record CandidateRecruitmentDto(
    int Id,
    string PositionTitle,
    int Status);

public sealed record CandidateResponseDto(
    int Id,
    int RecruitmentId,
    string FullName,
    string? Phone,
    string? Email,
    DateTime AppliedDate,
    int Status,
    decimal? InterviewScore,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    CandidateRecruitmentDto? Recruitment);

public sealed record CandidateUpsertDto(
    int RecruitmentId,
    string FullName,
    string? Phone,
    string? Email,
    DateTime AppliedDate,
    int Status,
    decimal? InterviewScore,
    string? Note);

public sealed record TrainingEmployeeTrainingDto(
    int Id,
    int EmployeeId,
    int Status,
    decimal? Score);

public sealed record TrainingResponseDto(
    int Id,
    string TrainingCode,
    string TrainingName,
    string? Description,
    DateTime StartDate,
    DateTime? EndDate,
    string? Instructor,
    bool IsRequired,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<TrainingEmployeeTrainingDto>? EmployeeTrainings);

public sealed record TrainingUpsertDto(
    string TrainingCode,
    string TrainingName,
    string? Description,
    DateTime StartDate,
    DateTime? EndDate,
    string? Instructor,
    bool IsRequired,
    bool IsActive);

public sealed record EmployeeTrainingEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record EmployeeTrainingTrainingDto(
    int Id,
    string TrainingCode,
    string TrainingName,
    bool IsActive);

public sealed record EmployeeTrainingResponseDto(
    int Id,
    int EmployeeId,
    int TrainingId,
    DateTime AssignedDate,
    DateTime? CompletedDate,
    int Status,
    decimal? Score,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    EmployeeTrainingEmployeeDto? Employee,
    EmployeeTrainingTrainingDto? Training);

public sealed record EmployeeTrainingUpsertDto(
    int EmployeeId,
    int TrainingId,
    DateTime AssignedDate,
    DateTime? CompletedDate,
    int Status,
    decimal? Score);

public sealed record KpiEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record KpiResponseDto(
    int Id,
    int EmployeeId,
    int KpiYear,
    int KpiMonth,
    decimal Score,
    decimal Target,
    string Result,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    KpiEmployeeDto? Employee);

public sealed record KpiUpsertDto(
    int EmployeeId,
    int KpiYear,
    int KpiMonth,
    decimal Score,
    decimal Target,
    string Result,
    string? Note);
