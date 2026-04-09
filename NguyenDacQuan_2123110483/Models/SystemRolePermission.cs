using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class SystemRolePermission
{
    [ForeignKey(nameof(SystemRole))]
    public int SystemRoleId { get; set; }

    [ForeignKey(nameof(Permission))]
    public int PermissionId { get; set; }

    public SystemRole? SystemRole { get; set; }
    public Permission? Permission { get; set; }
}
