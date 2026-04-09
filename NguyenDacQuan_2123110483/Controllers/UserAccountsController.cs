using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.SecurityAccountsManage)]
public class UserAccountsController : ControllerBase
{
    private readonly IUserAccountService _userAccountService;

    public UserAccountsController(IUserAccountService userAccountService)
    {
        _userAccountService = userAccountService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserAccountResponseDto>>> GetUserAccounts(CancellationToken cancellationToken)
        => Ok(await _userAccountService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserAccountResponseDto>> GetUserAccount(int id, CancellationToken cancellationToken)
    {
        var userAccount = await _userAccountService.GetByIdAsync(id, cancellationToken);
        return userAccount == null ? NotFound() : Ok(userAccount);
    }

    [HttpPost]
    public async Task<ActionResult<UserAccountResponseDto>> PostUserAccount([FromBody] UserAccountUpsertDto userAccount, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.CreateAsync(userAccount, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetUserAccount), new { id = result.UserAccount!.Id }, result.UserAccount)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutUserAccount(int id, [FromBody] UserAccountUpsertDto userAccount, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.UpdateAsync(id, userAccount, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUserAccount(int id, CancellationToken cancellationToken)
    {
        var result = await _userAccountService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
