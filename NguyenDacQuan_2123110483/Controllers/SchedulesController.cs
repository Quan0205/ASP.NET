using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.OpsSchedulesManage)]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScheduleResponseDto>>> GetSchedules(CancellationToken cancellationToken)
    {
        return Ok(await _scheduleService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ScheduleResponseDto>> GetSchedule(int id, CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.GetByIdAsync(id, cancellationToken);
        return schedule == null ? NotFound() : Ok(schedule);
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ScheduleValidationResultDto>> ValidateSchedule([FromBody] ScheduleRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _scheduleService.ValidateAsync(request, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ScheduleResponseDto>> PostSchedule([FromBody] ScheduleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.CreateAsync(request, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetSchedule), new { id = result.Schedule!.Id }, result.Schedule);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutSchedule(int id, [FromBody] ScheduleRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id, CancellationToken cancellationToken)
    {
        var result = await _scheduleService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
