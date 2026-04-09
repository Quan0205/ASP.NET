using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.MasterShiftsManage)]
public class ShiftsController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShiftResponseDto>>> GetShifts(CancellationToken cancellationToken)
    {
        return Ok(await _shiftService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftResponseDto>> GetShift(int id, CancellationToken cancellationToken)
    {
        var shift = await _shiftService.GetByIdAsync(id, cancellationToken);
        return shift == null ? NotFound() : Ok(shift);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftResponseDto>> PostShift([FromBody] ShiftUpsertDto shift, CancellationToken cancellationToken)
    {
        var result = await _shiftService.CreateAsync(shift, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetShift), new { id = result.Shift!.Id }, result.Shift);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutShift(int id, [FromBody] ShiftUpsertDto shift, CancellationToken cancellationToken)
    {
        var result = await _shiftService.UpdateAsync(id, shift, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteShift(int id, CancellationToken cancellationToken)
    {
        var result = await _shiftService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
