using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.ShiftSwapManage)]
public class ShiftSwapRequestsController : ControllerBase
{
    private readonly IShiftSwapRequestService _shiftSwapRequestService;

    public ShiftSwapRequestsController(IShiftSwapRequestService shiftSwapRequestService)
    {
        _shiftSwapRequestService = shiftSwapRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftSwapResponseDto>>> GetShiftSwapRequests(CancellationToken cancellationToken)
        => Ok(await _shiftSwapRequestService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ShiftSwapResponseDto>> Create([FromBody] ShiftSwapUpsertDto request, CancellationToken cancellationToken)
    {
        var result = await _shiftSwapRequestService.CreateAsync(request, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetShiftSwapRequests), new { id = result.ShiftSwap!.Id }, result.ShiftSwap)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _shiftSwapRequestService.ApproveAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _shiftSwapRequestService.RejectAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _shiftSwapRequestService.CancelAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
