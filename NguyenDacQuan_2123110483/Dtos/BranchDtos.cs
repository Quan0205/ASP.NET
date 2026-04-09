namespace CoffeeHRM.Dtos;

public sealed record BranchResponseDto(
    int Id,
    string BranchCode,
    string BranchName,
    string Address,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record BranchUpsertDto(
    string BranchCode,
    string BranchName,
    string Address,
    string? Phone,
    bool IsActive);
