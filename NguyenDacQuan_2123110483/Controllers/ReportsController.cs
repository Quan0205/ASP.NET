using System.Text;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.ReportsView)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReportSummaryDto>> Summary([FromQuery] int? month, [FromQuery] int? year, [FromQuery] int? branchId, CancellationToken cancellationToken)
        => Ok(await _reportService.GetSummaryAsync(month, year, branchId, cancellationToken));

    [HttpGet("employees/csv")]
    [PermissionAuthorize(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportEmployees(CancellationToken cancellationToken)
    {
        var csv = await _reportService.ExportEmployeesCsvAsync(cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"employees-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    [HttpGet("attendance/csv")]
    [PermissionAuthorize(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportAttendance([FromQuery] int? month, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var csv = await _reportService.ExportAttendanceCsvAsync(month, year, cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"attendance-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
