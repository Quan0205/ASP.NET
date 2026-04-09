namespace CoffeeHRM.Dtos;

public sealed record UserAccountEmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    bool IsActive);

public sealed record UserAccountRoleDto(
    int Id,
    string RoleName,
    bool IsActive);

public sealed record UserAccountSystemRoleDto(
    int Id,
    string Code,
    string Name,
    bool IsActive);

public sealed record UserAccountResponseDto(
    int Id,
    int EmployeeId,
    int RoleId,
    int? SystemRoleId,
    string Username,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    UserAccountEmployeeDto? Employee,
    UserAccountRoleDto? Role,
    UserAccountSystemRoleDto? SystemRole);

public sealed record UserAccountUpsertDto(
    int EmployeeId,
    int RoleId,
    int? SystemRoleId,
    string Username,
    string? Password,
    bool IsActive,
    DateTime? LastLoginAt);

public sealed record SystemRolePermissionDto(
    int PermissionId,
    string Code,
    string Name,
    string? Description);

public sealed record SystemRoleResponseDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<int> PermissionIds,
    IReadOnlyList<SystemRolePermissionDto> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SystemRoleUpsertDto(
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    IReadOnlyList<int>? PermissionIds);
