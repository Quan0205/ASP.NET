namespace CoffeeHRM.Dtos;

public sealed record ApiErrorResponse(string Message, int StatusCode);

public sealed record PermissionResponseDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AuditLogUserDto(
    int Id,
    string Username);

public sealed record AuditLogResponseDto(
    int Id,
    int? UserAccountId,
    string Action,
    string TableName,
    string RecordId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    AuditLogUserDto? UserAccount);
