using CoffeeHRM.Data;
using CoffeeHRM.Models;
using Microsoft.EntityFrameworkCore;

namespace CoffeeHRM.Services;

public interface IRolePermissionService
{
    Task<List<string>> GetPermissionsAsync(UserAccount account, CancellationToken cancellationToken = default);
}

public sealed class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _context;

    public RolePermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetPermissionsAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        if (account.SystemRoleId == null)
        {
            return new List<string>();
        }

        return await _context.SystemRolePermissions
            .Where(x => x.SystemRoleId == account.SystemRoleId.Value)
            .Include(x => x.Permission)
            .Where(x => x.Permission != null)
            .Select(x => x.Permission!.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
