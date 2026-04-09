using System.ComponentModel.DataAnnotations;

namespace CoffeeHRM.Models;

public class Permission : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<SystemRolePermission> SystemRolePermissions { get; set; } = new List<SystemRolePermission>();
}
