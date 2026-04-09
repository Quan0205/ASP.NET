using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoffeeHRM.Services;

public interface IAuthService
{
    Task<(AuthResponse? Response, string? Error, int? StatusCode)> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<(AuthResponse? Response, string? Error, int? StatusCode)> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> LogoutAllAsync(string? ipAddress, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ChangePasswordAsync(ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int? StatusCode)> ResetPasswordAsync(int userAccountId, ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<AuthProfileDto?> GetCurrentProfileAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext context,
        IJwtTokenService tokenService,
        IRolePermissionService rolePermissionService,
        ICurrentUserService currentUserService,
        IOptions<JwtOptions> jwtOptions)
    {
        _context = context;
        _tokenService = tokenService;
        _rolePermissionService = rolePermissionService;
        _currentUserService = currentUserService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<(AuthResponse? Response, string? Error, int? StatusCode)> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return (null, "Username and password are required.", StatusCodes.Status400BadRequest);

        var account = await LoadAccountByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (account == null || account.IsDeleted || !account.IsActive || account.Employee == null || !account.Employee.IsActive)
            return (null, "Invalid username or password.", StatusCodes.Status401Unauthorized);
        if (!PasswordHashHelper.Verify(account.PasswordHash, request.Password))
            return (null, "Invalid username or password.", StatusCodes.Status401Unauthorized);
        if (account.SystemRoleId == null || account.SystemRole is null || !account.SystemRole.IsActive)
            return (null, "Account is not authorized.", StatusCodes.Status403Forbidden);

        var permissions = await _rolePermissionService.GetPermissionsAsync(account, cancellationToken);
        var refreshToken = _tokenService.CreateRefreshToken();
        account.LastLoginAt = DateTime.UtcNow;
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserAccountId = account.Id,
            TokenHash = _tokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync(cancellationToken);
        return (BuildAuthResponse(account, permissions, refreshToken, DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes)), null, null);
    }

    public async Task<(AuthResponse? Response, string? Error, int? StatusCode)> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return (null, "RefreshToken is required.", StatusCodes.Status400BadRequest);

        var hashedToken = _tokenService.HashRefreshToken(request.RefreshToken.Trim());
        var storedToken = await _context.RefreshTokens
            .Include(x => x.UserAccount!).ThenInclude(x => x.Employee)
            .Include(x => x.UserAccount!).ThenInclude(x => x.Role)
            .Include(x => x.UserAccount!).ThenInclude(x => x.SystemRole)
            .FirstOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken);

        if (storedToken == null || storedToken.IsDeleted || storedToken.RevokedAt.HasValue || storedToken.IsUsed || storedToken.ExpiresAt <= DateTime.UtcNow)
            return (null, "Refresh token is invalid.", StatusCodes.Status401Unauthorized);

        var account = storedToken.UserAccount;
        if (account == null || account.IsDeleted || !account.IsActive || account.Employee == null || !account.Employee.IsActive || account.SystemRoleId == null)
            return (null, "Account is not active.", StatusCodes.Status401Unauthorized);

        var permissions = await _rolePermissionService.GetPermissionsAsync(account, cancellationToken);
        var newRefreshToken = _tokenService.CreateRefreshToken();
        storedToken.IsUsed = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenHash = _tokenService.HashRefreshToken(newRefreshToken);
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserAccountId = account.Id,
            TokenHash = _tokenService.HashRefreshToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        });
        await _context.SaveChangesAsync(cancellationToken);
        return (BuildAuthResponse(account, permissions, newRefreshToken, DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes)), null, null);
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken)) return;
        var hashedToken = _tokenService.HashRefreshToken(request.RefreshToken.Trim());
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken);
        if (storedToken != null && !storedToken.RevokedAt.HasValue)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> LogoutAllAsync(string? ipAddress, CancellationToken cancellationToken = default)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account == null) return (false, null, StatusCodes.Status401Unauthorized);
        await RevokeAllRefreshTokensAsync(account.Id, ipAddress, cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ChangePasswordAsync(ChangePasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return (false, "CurrentPassword and NewPassword are required.", StatusCodes.Status400BadRequest);
        if (request.NewPassword.Trim().Length < 6)
            return (false, "New password must be at least 6 characters.", StatusCodes.Status400BadRequest);

        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account == null) return (false, null, StatusCodes.Status401Unauthorized);
        if (!PasswordHashHelper.Verify(account.PasswordHash, request.CurrentPassword))
            return (false, "Current password is incorrect.", StatusCodes.Status400BadRequest);

        account.PasswordHash = PasswordHashHelper.Hash(request.NewPassword);
        await RevokeAllRefreshTokensAsync(account.Id, ipAddress, cancellationToken, persistChanges: false);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<(bool Success, string? Error, int? StatusCode)> ResetPasswordAsync(int userAccountId, ResetPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Trim().Length < 6)
            return (false, "New password must be at least 6 characters.", StatusCodes.Status400BadRequest);
        var account = await _context.UserAccounts.FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken);
        if (account == null) return (false, "User account not found.", StatusCodes.Status404NotFound);
        account.PasswordHash = PasswordHashHelper.Hash(request.NewPassword);
        if (request.RequireLogoutAll)
            await RevokeAllRefreshTokensAsync(account.Id, ipAddress, cancellationToken, persistChanges: false);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null, null);
    }

    public async Task<AuthUserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account == null) return null;
        var permissions = await _rolePermissionService.GetPermissionsAsync(account, cancellationToken);
        return BuildUserDto(account, permissions);
    }

    public async Task<AuthProfileDto?> GetCurrentProfileAsync(CancellationToken cancellationToken = default)
    {
        var account = await LoadCurrentAccountAsync(cancellationToken);
        if (account == null) return null;
        var permissions = await _rolePermissionService.GetPermissionsAsync(account, cancellationToken);
        return new AuthProfileDto(
            account.Id,
            account.Username,
            account.Employee?.FullName ?? account.Username,
            account.EmployeeId,
            account.Employee?.EmployeeCode ?? string.Empty,
            account.Employee?.Phone,
            account.Employee?.Email,
            account.Employee?.Branch?.BranchName,
            account.Role?.RoleName,
            account.SystemRole?.Code,
            account.SystemRole?.Name,
            account.LastLoginAt,
            permissions);
    }

    private async Task<UserAccount?> LoadAccountByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await _context.UserAccounts
            .Include(x => x.Employee).ThenInclude(x => x!.Branch)
            .Include(x => x.Role)
            .Include(x => x.SystemRole)
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    private async Task<UserAccount?> LoadCurrentAccountAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue) return null;
        return await _context.UserAccounts
            .Include(x => x.Employee).ThenInclude(x => x!.Branch)
            .Include(x => x.Role)
            .Include(x => x.SystemRole)
            .FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId.Value, cancellationToken);
    }

    private async Task RevokeAllRefreshTokensAsync(int userAccountId, string? ipAddress, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var tokens = await _context.RefreshTokens.Where(x => x.UserAccountId == userAccountId && !x.RevokedAt.HasValue).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }
        if (persistChanges && tokens.Count > 0) await _context.SaveChangesAsync(cancellationToken);
    }

    private AuthResponse BuildAuthResponse(UserAccount account, IReadOnlyList<string> permissions, string refreshToken, DateTime expiresAt)
    {
        var accessToken = _tokenService.CreateAccessToken(account, permissions, expiresAt);
        return new AuthResponse(accessToken, refreshToken, expiresAt, BuildUserDto(account, permissions));
    }

    private static AuthUserDto BuildUserDto(UserAccount account, IReadOnlyList<string> permissions)
    {
        return new AuthUserDto(account.Id, account.Username, account.Employee?.FullName ?? account.Username, account.EmployeeId, account.Employee?.EmployeeCode ?? string.Empty, account.Role?.RoleName, account.SystemRole?.Code, account.SystemRole?.Name, permissions);
    }
}
