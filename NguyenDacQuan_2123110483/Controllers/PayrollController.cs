using System.Text;
using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.PayrollManage)]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PayrollResponseDto>>> GetPayrolls(CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PayrollResponseDto>> GetPayroll(int id, CancellationToken cancellationToken)
    {
        var payroll = await _payrollService.GetByIdAsync(id, cancellationToken);
        return payroll == null ? NotFound() : Ok(payroll);
    }

    [HttpGet("close-periods")]
    [PermissionAuthorize(PermissionCodes.OperationsManage)]
    public async Task<ActionResult<IEnumerable<PayrollClosePeriodResponseDto>>> GetClosePeriods(CancellationToken cancellationToken)
    {
        return Ok(await _payrollService.GetClosePeriodsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PayrollResponseDto>> PostPayroll([FromBody] PayrollRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.CreateAsync(request, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetPayroll), new { id = result.Payroll!.Id }, result.Payroll);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutPayroll(int id, [FromBody] PayrollRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.UpdateAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePayroll(int id, CancellationToken cancellationToken)
    {
        var result = await _payrollService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> ApprovePayroll(int id, [FromBody] PayrollDecisionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.ApproveAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> PayPayroll(int id, [FromBody] PayrollDecisionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.PayAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> CancelPayroll(int id, [FromBody] PayrollDecisionRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.CancelAsync(id, request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("close-period")]
    [PermissionAuthorize(PermissionCodes.OperationsManage)]
    public async Task<IActionResult> ClosePeriod([FromBody] PayrollClosePeriodRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.ClosePeriodAsync(request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPost("reopen-period")]
    [PermissionAuthorize(PermissionCodes.OperationsManage)]
    public async Task<IActionResult> ReopenPeriod([FromBody] PayrollClosePeriodRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.ReopenPeriodAsync(request, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpGet("{payrollId:int}/details")]
    public async Task<ActionResult<IEnumerable<PayrollDetailResponseDto>>> GetPayrollDetails(int payrollId, CancellationToken cancellationToken)
    {
        var details = await _payrollService.GetDetailsAsync(payrollId, cancellationToken);
        return details == null ? NotFound("Payroll not found.") : Ok(details);
    }

    [HttpPost("{payrollId:int}/details")]
    public async Task<ActionResult<PayrollDetailResponseDto>> AddPayrollDetail(int payrollId, [FromBody] PayrollDetailRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.AddDetailAsync(payrollId, request, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetPayrollDetails), new { payrollId }, result.Detail);
    }

    [HttpPost("generate")]
    public async Task<ActionResult<PayrollResponseDto>> GeneratePayroll([FromBody] PayrollGenerateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _payrollService.GenerateAsync(request, cancellationToken);
        return result.Error == null ? Ok(result.Payroll) : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpGet("export/csv")]
    [PermissionAuthorize(PermissionCodes.ReportsExport)]
    public async Task<IActionResult> ExportPayrollCsv(CancellationToken cancellationToken)
    {
        var csv = await _payrollService.ExportCsvAsync(cancellationToken);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"payroll-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
