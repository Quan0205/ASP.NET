using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.SecurityAccountsManage)]
public class PermissionsController : ApiControllerBase
{
    private readonly IPermissionReadService _permissionReadService;

    public PermissionsController(IPermissionReadService permissionReadService)
    {
        _permissionReadService = permissionReadService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionResponseDto>>> GetPermissions(CancellationToken cancellationToken)
    {
        return Ok(await _permissionReadService.GetAllAsync(cancellationToken));
    }
}
