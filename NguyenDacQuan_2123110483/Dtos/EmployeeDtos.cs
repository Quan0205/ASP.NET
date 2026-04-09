namespace CoffeeHRM.Dtos;

public sealed record BranchLookupDto(
    int Id,
    string BranchCode,
    string BranchName,
    bool IsActive);

public sealed record RoleLookupDto(
    int Id,
    string RoleName,
    bool IsActive);

public sealed record UserAccountLookupDto(
    int Id,
    string Username,
    bool IsActive,
    DateTime? LastLoginAt);

public sealed record EmployeeContractLookupDto(
    int Id,
    string ContractNo,
    DateTime StartDate,
    DateTime? EndDate,
    decimal BaseSalary,
    decimal HourlyRate,
    bool IsActive);

public sealed record EmployeeResponseDto(
    int Id,
    string EmployeeCode,
    string FullName,
    int Gender,
    DateTime? DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    int BranchId,
    int RoleId,
    DateTime HireDate,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    BranchLookupDto? Branch,
    RoleLookupDto? Role,
    IReadOnlyList<EmployeeContractLookupDto>? EmployeeContracts,
    UserAccountLookupDto? UserAccount);

public sealed record EmployeeUpsertDto(
    string EmployeeCode,
    string FullName,
    int Gender,
    DateTime? DateOfBirth,
    string? Phone,
    string? Email,
    string? Address,
    int BranchId,
    int RoleId,
    DateTime HireDate,
    bool IsActive);
