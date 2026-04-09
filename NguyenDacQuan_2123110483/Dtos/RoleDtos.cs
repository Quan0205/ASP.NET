namespace CoffeeHRM.Dtos;

public sealed record RoleResponseDto(
    int Id,
    string RoleName,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record RoleUpsertDto(
    string RoleName,
    string? Description,
    bool IsActive);
