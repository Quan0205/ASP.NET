namespace CoffeeHRM.Models;

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthUserDto(
    int Id,
    string Username,
    string FullName,
    int EmployeeId,
    string EmployeeCode,
    string? JobRoleName,
    string? SystemRoleCode,
    string? SystemRoleName,
    IReadOnlyList<string> Permissions);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    AuthUserDto User);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ResetPasswordRequest(string NewPassword, bool RequireLogoutAll);

public sealed record AuthProfileDto(
    int Id,
    string Username,
    string FullName,
    int EmployeeId,
    string EmployeeCode,
    string? Phone,
    string? Email,
    string? BranchName,
    string? JobRoleName,
    string? SystemRoleCode,
    string? SystemRoleName,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Permissions);
