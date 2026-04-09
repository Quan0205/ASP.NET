using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.TrainingManage)]
public class EmployeeTrainingsController : ControllerBase
{
    private readonly IEmployeeTrainingService _employeeTrainingService;

    public EmployeeTrainingsController(IEmployeeTrainingService employeeTrainingService)
    {
        _employeeTrainingService = employeeTrainingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeTrainingResponseDto>>> GetEmployeeTrainings(CancellationToken cancellationToken)
    {
        return Ok(await _employeeTrainingService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeTrainingResponseDto>> GetEmployeeTraining(int id, CancellationToken cancellationToken)
    {
        var employeeTraining = await _employeeTrainingService.GetByIdAsync(id, cancellationToken);
        return employeeTraining == null ? NotFound() : Ok(employeeTraining);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeTrainingResponseDto>> PostEmployeeTraining([FromBody] EmployeeTrainingUpsertDto employeeTraining, CancellationToken cancellationToken)
    {
        var result = await _employeeTrainingService.CreateAsync(employeeTraining, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetEmployeeTraining), new { id = result.EmployeeTraining!.Id }, result.EmployeeTraining)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployeeTraining(int id, [FromBody] EmployeeTrainingUpsertDto employeeTraining, CancellationToken cancellationToken)
    {
        var result = await _employeeTrainingService.UpdateAsync(id, employeeTraining, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployeeTraining(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeTrainingService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
