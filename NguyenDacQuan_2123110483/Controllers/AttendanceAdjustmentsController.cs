using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.AttendanceAdjustmentManage)]
public class AttendanceAdjustmentsController : ControllerBase
{
    private readonly IAttendanceAdjustmentService _attendanceAdjustmentService;

    public AttendanceAdjustmentsController(IAttendanceAdjustmentService attendanceAdjustmentService)
    {
        _attendanceAdjustmentService = attendanceAdjustmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentResponseDto>>> GetAttendanceAdjustments(CancellationToken cancellationToken)
        => Ok(await _attendanceAdjustmentService.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AttendanceAdjustmentResponseDto>> Create([FromBody] AttendanceAdjustmentUpsertDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceAdjustmentService.CreateAsync(request, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetAttendanceAdjustments), new { id = result.Adjustment!.Id }, result.Adjustment)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceAdjustmentService.ApproveAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] DecisionNoteDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceAdjustmentService.RejectAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _attendanceAdjustmentService.CancelAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
