namespace CoffeeHRM.Dtos;

public sealed record PayrollEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record PayrollContractDto(
    int Id,
    string ContractNo,
    int ContractType,
    DateTime StartDate,
    DateTime? EndDate,
    decimal BaseSalary,
    decimal HourlyRate,
    decimal OvertimeRateMultiplier,
    decimal LatePenaltyPerMinute,
    decimal EarlyLeavePenaltyPerMinute,
    decimal StandardDailyHours,
    bool IsActive);

public sealed record PayrollDetailResponseDto(
    int Id,
    int PayrollId,
    int DetailType,
    int? AttendanceId,
    int? ScheduleId,
    int? SourceReferenceId,
    string Description,
    decimal Amount,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PayrollResponseDto(
    int Id,
    int EmployeeId,
    int EmployeeContractId,
    int PayrollMonth,
    int PayrollYear,
    decimal BaseAmount,
    decimal WorkingHours,
    decimal HourlyRate,
    decimal OvertimeAmount,
    decimal AllowanceAmount,
    decimal BonusAmount,
    decimal PenaltyAmount,
    decimal InsuranceAmount,
    decimal TaxAmount,
    decimal TotalSalary,
    int Status,
    DateTime? PaidDate,
    DateTime? ApprovedAt,
    int? ApprovedByUserAccountId,
    bool IsClosed,
    DateTime? ClosedAt,
    int? ClosedByUserAccountId,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    PayrollEmployeeDto? Employee,
    PayrollContractDto? EmployeeContract,
    IReadOnlyList<PayrollDetailResponseDto>? PayrollDetails);

public sealed record PayrollClosePeriodResponseDto(
    int Id,
    int PayrollMonth,
    int PayrollYear,
    bool IsClosed,
    DateTime? ClosedAt,
    int? ClosedByUserAccountId,
    string? Note,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PayrollRequestDto(
    int EmployeeId,
    int EmployeeContractId,
    int PayrollMonth,
    int PayrollYear,
    decimal? HourlyRate,
    decimal AllowanceAmount,
    decimal BonusAmount,
    decimal PenaltyAmount,
    decimal InsuranceAmount,
    decimal TaxAmount,
    string? Note);

public sealed record PayrollGenerateRequestDto(
    int EmployeeId,
    int PayrollMonth,
    int PayrollYear,
    decimal AllowanceAmount,
    decimal BonusAmount,
    decimal PenaltyAmount,
    decimal InsuranceAmount,
    decimal TaxAmount,
    string? Note);

public sealed record PayrollDecisionRequestDto(string? Note);
public sealed record PayrollClosePeriodRequestDto(int PayrollMonth, int PayrollYear, string? Note);

public sealed record PayrollDetailRequestDto(
    int DetailType,
    decimal Amount,
    string Description,
    int? AttendanceId,
    int? ScheduleId,
    int? SourceReferenceId,
    string? Note);
