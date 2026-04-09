using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.KpiManage)]
public class KPIsController : ControllerBase
{
    private readonly IKpiService _kpiService;

    public KPIsController(IKpiService kpiService)
    {
        _kpiService = kpiService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KpiResponseDto>>> GetKPIs(CancellationToken cancellationToken)
    {
        return Ok(await _kpiService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<KpiResponseDto>> GetKPI(int id, CancellationToken cancellationToken)
    {
        var kpi = await _kpiService.GetByIdAsync(id, cancellationToken);
        return kpi == null ? NotFound() : Ok(kpi);
    }

    [HttpPost]
    public async Task<ActionResult<KpiResponseDto>> PostKPI([FromBody] KpiUpsertDto kpi, CancellationToken cancellationToken)
    {
        var result = await _kpiService.CreateAsync(kpi, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetKPI), new { id = result.Kpi!.Id }, result.Kpi)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutKPI(int id, [FromBody] KpiUpsertDto kpi, CancellationToken cancellationToken)
    {
        var result = await _kpiService.UpdateAsync(id, kpi, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteKPI(int id, CancellationToken cancellationToken)
    {
        var result = await _kpiService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
