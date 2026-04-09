using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.HrContractsManage)]
public class EmployeeContractsController : ControllerBase
{
    private readonly IEmployeeContractService _employeeContractService;

    public EmployeeContractsController(IEmployeeContractService employeeContractService)
    {
        _employeeContractService = employeeContractService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeContractResponseDto>>> GetEmployeeContracts(CancellationToken cancellationToken)
    {
        return Ok(await _employeeContractService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeContractResponseDto>> GetEmployeeContract(int id, CancellationToken cancellationToken)
    {
        var contract = await _employeeContractService.GetByIdAsync(id, cancellationToken);
        return contract == null ? NotFound() : Ok(contract);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeContractResponseDto>> PostEmployeeContract([FromBody] EmployeeContractUpsertDto contract, CancellationToken cancellationToken)
    {
        var result = await _employeeContractService.CreateAsync(contract, cancellationToken);
        if (result.Error != null)
        {
            return StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
        }

        return CreatedAtAction(nameof(GetEmployeeContract), new { id = result.Contract!.Id }, result.Contract);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutEmployeeContract(int id, [FromBody] EmployeeContractUpsertDto contract, CancellationToken cancellationToken)
    {
        var result = await _employeeContractService.UpdateAsync(id, contract, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployeeContract(int id, CancellationToken cancellationToken)
    {
        var result = await _employeeContractService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
