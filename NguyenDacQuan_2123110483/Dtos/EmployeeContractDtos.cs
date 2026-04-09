namespace CoffeeHRM.Dtos;

public sealed record EmployeeContractEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record EmployeeContractResponseDto(
    int Id,
    string ContractNo,
    int ContractType,
    int EmployeeId,
    DateTime StartDate,
    DateTime? EndDate,
    decimal BaseSalary,
    decimal HourlyRate,
    decimal OvertimeRateMultiplier,
    decimal LatePenaltyPerMinute,
    decimal EarlyLeavePenaltyPerMinute,
    decimal StandardDailyHours,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    EmployeeContractEmployeeDto? Employee);

public sealed record EmployeeContractUpsertDto(
    string ContractNo,
    int ContractType,
    int EmployeeId,
    DateTime StartDate,
    DateTime? EndDate,
    decimal BaseSalary,
    decimal HourlyRate,
    decimal OvertimeRateMultiplier,
    decimal LatePenaltyPerMinute,
    decimal EarlyLeavePenaltyPerMinute,
    decimal StandardDailyHours,
    bool IsActive);
