using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.LeaveManage)]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeaveRequestResponseDto>>> GetLeaveRequests(CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponseDto>> Create([FromBody] LeaveRequestUpsertDto request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.CreateAsync(request, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetLeaveRequests), new { id = result.LeaveRequest!.Id }, result.LeaveRequest)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LeaveRequestUpsertDto request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.ApproveAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.RejectAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.CancelAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
