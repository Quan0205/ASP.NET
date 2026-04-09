using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.HrEmployeesManage)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponseDto>>> GetEmployees(CancellationToken cancellationToken)
    {
        return Ok(await _employeeService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeResponseDto>> GetEmployee(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        return employee == null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponseDto>> PostEmployee([FromBody] EmployeeUpsertDto employee, CancellationToken cancellationToken)
    {
        var result = await _employeeService.CreateAsync(employee, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetEmployee), new { id = result.Employee!.Id }, result.Employee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployee(int id, [FromBody] EmployeeUpsertDto employee, CancellationToken cancellationToken)
    {
        var result = await _employeeService.UpdateAsync(id, employee, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
