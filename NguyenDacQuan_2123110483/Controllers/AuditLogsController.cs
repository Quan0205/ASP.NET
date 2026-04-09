using CoffeeHRM.Dtos;
using CoffeeHRM.Models;
using CoffeeHRM.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
[PermissionAuthorize(PermissionCodes.AuditView)]
public class AuditLogsController : ApiControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetAuditLogs([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int? userAccountId, [FromQuery] string? tableName, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetAllAsync(fromDate, toDate, userAccountId, tableName, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLogResponseDto>> GetAuditLog(int id, CancellationToken cancellationToken)
    {
        var auditLog = await _auditLogService.GetByIdAsync(id, cancellationToken);
        return auditLog == null ? NotFound() : Ok(auditLog);
    }

    [HttpGet("user/{userAccountId:int}")]
    public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetByUserAccount(int userAccountId, CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetByUserAsync(userAccountId, cancellationToken));
    }

    [HttpGet("table/{tableName}")]
    public async Task<ActionResult<IEnumerable<AuditLogResponseDto>>> GetByTableName(string tableName, CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetByTableAsync(tableName, cancellationToken);
        return result.Error == null ? Ok(result.Logs) : ApiError(result.Error!, result.StatusCode ?? StatusCodes.Status400BadRequest);
    }
}
