using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.SecurityAccountsManage)]
public class SystemRolesController : ControllerBase
{
    private readonly ISystemRoleService _systemRoleService;

    public SystemRolesController(ISystemRoleService systemRoleService)
    {
        _systemRoleService = systemRoleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SystemRoleResponseDto>>> GetSystemRoles(CancellationToken cancellationToken)
        => Ok(await _systemRoleService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SystemRoleResponseDto>> GetSystemRole(int id, CancellationToken cancellationToken)
    {
        var systemRole = await _systemRoleService.GetByIdAsync(id, cancellationToken);
        return systemRole == null ? NotFound() : Ok(systemRole);
    }

    [HttpPost]
    public async Task<ActionResult<SystemRoleResponseDto>> PostSystemRole([FromBody] SystemRoleUpsertDto systemRole, CancellationToken cancellationToken)
    {
        var result = await _systemRoleService.CreateAsync(systemRole, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetSystemRole), new { id = result.SystemRole!.Id }, result.SystemRole)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutSystemRole(int id, [FromBody] SystemRoleUpsertDto systemRole, CancellationToken cancellationToken)
    {
        var result = await _systemRoleService.UpdateAsync(id, systemRole, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSystemRole(int id, CancellationToken cancellationToken)
    {
        var result = await _systemRoleService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
