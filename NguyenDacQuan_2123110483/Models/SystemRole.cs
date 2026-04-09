using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class SystemRole : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public List<int> PermissionIds { get; set; } = new();

    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
    public ICollection<SystemRolePermission> SystemRolePermissions { get; set; } = new List<SystemRolePermission>();
}
