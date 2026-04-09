using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.MasterBranchesManage)]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BranchResponseDto>>> GetBranches(CancellationToken cancellationToken)
    {
        return Ok(await _branchService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BranchResponseDto>> GetBranch(int id, CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetByIdAsync(id, cancellationToken);
        return branch == null ? NotFound() : Ok(branch);
    }

    [HttpPost]
    public async Task<ActionResult<BranchResponseDto>> PostBranch([FromBody] BranchUpsertDto branch, CancellationToken cancellationToken)
    {
        var result = await _branchService.CreateAsync(branch, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetBranch), new { id = result.Branch!.Id }, result.Branch);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutBranch(int id, [FromBody] BranchUpsertDto branch, CancellationToken cancellationToken)
    {
        var result = await _branchService.UpdateAsync(id, branch, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBranch(int id, CancellationToken cancellationToken)
    {
        var result = await _branchService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
