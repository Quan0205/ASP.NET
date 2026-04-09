using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.RecruitmentManage)]
public class RecruitmentsController : ControllerBase
{
    private readonly IRecruitmentService _recruitmentService;

    public RecruitmentsController(IRecruitmentService recruitmentService)
    {
        _recruitmentService = recruitmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecruitmentResponseDto>>> GetRecruitments(CancellationToken cancellationToken)
    {
        return Ok(await _recruitmentService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RecruitmentResponseDto>> GetRecruitment(int id, CancellationToken cancellationToken)
    {
        var recruitment = await _recruitmentService.GetByIdAsync(id, cancellationToken);
        return recruitment == null ? NotFound() : Ok(recruitment);
    }

    [HttpPost]
    public async Task<ActionResult<RecruitmentResponseDto>> PostRecruitment([FromBody] RecruitmentUpsertDto recruitment, CancellationToken cancellationToken)
    {
        var result = await _recruitmentService.CreateAsync(recruitment, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetRecruitment), new { id = result.Recruitment!.Id }, result.Recruitment)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutRecruitment(int id, [FromBody] RecruitmentUpsertDto recruitment, CancellationToken cancellationToken)
    {
        var result = await _recruitmentService.UpdateAsync(id, recruitment, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRecruitment(int id, CancellationToken cancellationToken)
    {
        var result = await _recruitmentService.CancelAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
