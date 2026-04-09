using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.OpsAttendanceManage)]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetAttendances([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.GetAllAsync(month, year, cancellationToken));
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<IEnumerable<AttendanceResponseDto>>> GetByEmployee(int employeeId, CancellationToken cancellationToken)
    {
        return Ok(await _attendanceService.GetByEmployeeAsync(employeeId, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetSummary([FromQuery] int month, [FromQuery] int year, [FromQuery] int? employeeId, CancellationToken cancellationToken)
    {
        var summary = await _attendanceService.GetSummaryAsync(month, year, employeeId, cancellationToken);
        return summary == null ? BadRequest("Invalid month or year.") : Ok(summary);
    }

    [HttpPost("mark-absent")]
    [PermissionAuthorize(PermissionCodes.OperationsManage)]
    public async Task<ActionResult<MarkAbsentResultDto>> MarkAbsent([FromBody] MarkAbsentRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.MarkAbsentAsync(request, cancellationToken);
        return result.Error == null ? Ok(result.Result) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceResponseDto>> CheckIn([FromBody] CheckInRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.CheckInAsync(request, cancellationToken);
        return result.Error == null ? Ok(result.Attendance) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceResponseDto>> CheckOut([FromBody] CheckOutRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _attendanceService.CheckOutAsync(request, cancellationToken);
        return result.Error == null ? Ok(result.Attendance) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
