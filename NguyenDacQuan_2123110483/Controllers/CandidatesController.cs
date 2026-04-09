using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.RecruitmentManage)]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;

    public CandidatesController(ICandidateService candidateService)
    {
        _candidateService = candidateService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CandidateResponseDto>>> GetCandidates(CancellationToken cancellationToken)
    {
        return Ok(await _candidateService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidateResponseDto>> GetCandidate(int id, CancellationToken cancellationToken)
    {
        var candidate = await _candidateService.GetByIdAsync(id, cancellationToken);
        return candidate == null ? NotFound() : Ok(candidate);
    }

    [HttpPost]
    public async Task<ActionResult<CandidateResponseDto>> PostCandidate([FromBody] CandidateUpsertDto candidate, CancellationToken cancellationToken)
    {
        var result = await _candidateService.CreateAsync(candidate, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetCandidate), new { id = result.Candidate!.Id }, result.Candidate)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutCandidate(int id, [FromBody] CandidateUpsertDto candidate, CancellationToken cancellationToken)
    {
        var result = await _candidateService.UpdateAsync(id, candidate, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCandidate(int id, CancellationToken cancellationToken)
    {
        var result = await _candidateService.DeleteAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
