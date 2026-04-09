using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Error == null ? Ok(result.Response) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Error == null ? Ok(result.Response) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return NoContent();
    }

    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAllAsync(HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status401Unauthorized);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ChangePasswordAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [PermissionAuthorize(PermissionCodes.SecurityAccountsManage)]
    [HttpPost("reset-password/{userAccountId:int}")]
    public async Task<IActionResult> ResetPassword(int userAccountId, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(userAccountId, request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(cancellationToken);
        return user == null ? Unauthorized() : Ok(user);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<AuthProfileDto>> Profile(CancellationToken cancellationToken)
    {
        var profile = await _authService.GetCurrentProfileAsync(cancellationToken);
        return profile == null ? Unauthorized() : Ok(profile);
    }
}
