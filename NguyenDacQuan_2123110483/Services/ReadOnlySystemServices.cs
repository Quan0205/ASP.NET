using CoffeeHRM.Data;
using CoffeeHRM.Dtos;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IPermissionReadService
{
    Task<IReadOnlyList<PermissionResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
}

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogResponseDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate, int? userAccountId, string? tableName, CancellationToken cancellationToken = default);
    Task<AuditLogResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLogResponseDto>> GetByUserAsync(int userAccountId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLogResponseDto>? Logs, string? Error, int? StatusCode)> GetByTableAsync(string tableName, CancellationToken cancellationToken = default);
}

public sealed class PermissionReadService : IPermissionReadService
{
    private readonly AppDbContext _context;

    public PermissionReadService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PermissionResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Permissions.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return rows.Select(x => new PermissionResponseDto(x.Id, x.Code, x.Name, x.Description, x.CreatedAt, x.UpdatedAt)).ToList();
    }
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AuditLogResponseDto>> GetAllAsync(DateTime? fromDate, DateTime? toDate, int? userAccountId, string? tableName, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.Include(x => x.UserAccount).AsNoTracking().AsQueryable();
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt <= toDate.Value);
        if (userAccountId.HasValue) query = query.Where(x => x.UserAccountId == userAccountId.Value);
        if (!string.IsNullOrWhiteSpace(tableName)) query = query.Where(x => x.TableName == tableName);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<AuditLogResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _context.AuditLogs.Include(x => x.UserAccount).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return row == null ? null : Map(row);
    }

    public async Task<IReadOnlyList<AuditLogResponseDto>> GetByUserAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        var rows = await _context.AuditLogs.Include(x => x.UserAccount).Where(x => x.UserAccountId == userAccountId).AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<(IReadOnlyList<AuditLogResponseDto>? Logs, string? Error, int? StatusCode)> GetByTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return (null, "TableName is required.", StatusCodes.Status400BadRequest);

        var rows = await _context.AuditLogs.Include(x => x.UserAccount).Where(x => x.TableName == tableName).AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return (rows.Select(Map).ToList(), null, null);
    }

    private static AuditLogResponseDto Map(Models.AuditLog x)
    {
        return new AuditLogResponseDto(x.Id, x.UserAccountId, x.Action, x.TableName, x.RecordId, x.OldValues, x.NewValues, x.IpAddress, x.CreatedAt, x.UpdatedAt, x.UserAccount == null ? null : new AuditLogUserDto(x.UserAccount.Id, x.UserAccount.Username));
    }
}
