using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.TrainingManage)]
public class TrainingsController : ControllerBase
{
    private readonly ITrainingService _trainingService;

    public TrainingsController(ITrainingService trainingService)
    {
        _trainingService = trainingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrainingResponseDto>>> GetTrainings(CancellationToken cancellationToken)
    {
        return Ok(await _trainingService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TrainingResponseDto>> GetTraining(int id, CancellationToken cancellationToken)
    {
        var training = await _trainingService.GetByIdAsync(id, cancellationToken);
        return training == null ? NotFound() : Ok(training);
    }

    [HttpPost]
    public async Task<ActionResult<TrainingResponseDto>> PostTraining([FromBody] TrainingUpsertDto training, CancellationToken cancellationToken)
    {
        var result = await _trainingService.CreateAsync(training, cancellationToken);
        return result.Error == null
            ? CreatedAtAction(nameof(GetTraining), new { id = result.Training!.Id }, result.Training)
            : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutTraining(int id, [FromBody] TrainingUpsertDto training, CancellationToken cancellationToken)
    {
        var result = await _trainingService.UpdateAsync(id, training, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTraining(int id, CancellationToken cancellationToken)
    {
        var result = await _trainingService.DeactivateAsync(id, cancellationToken);
        return result.Success ? NoContent() : StatusCode(result.StatusCode ?? StatusCodes.Status400BadRequest, result.Error);
    }
}
