using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuditLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int? userAccountId,
        [FromQuery] string? tableName)
    {
        var query = _context.AuditLogs
            .Include(x => x.UserAccount)
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toDate.Value);
        }

        if (userAccountId.HasValue)
        {
            query = query.Where(x => x.UserAccountId == userAccountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            query = query.Where(x => x.TableName == tableName);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuditLog>> GetAuditLog(int id)
    {
        var auditLog = await _context.AuditLogs
            .Include(x => x.UserAccount)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (auditLog == null)
        {
            return NotFound();
        }

        return auditLog;
    }

    [HttpGet("user/{userAccountId:int}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByUserAccount(int userAccountId)
    {
        return await _context.AuditLogs
            .Include(x => x.UserAccount)
            .Where(x => x.UserAccountId == userAccountId)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    [HttpGet("table/{tableName}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetByTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return BadRequest("TableName is required.");
        }

        return await _context.AuditLogs
            .Include(x => x.UserAccount)
            .Where(x => x.TableName == tableName)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
