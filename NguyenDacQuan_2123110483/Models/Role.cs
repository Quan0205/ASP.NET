using System.ComponentModel.DataAnnotations;

namespace CoffeeHRM.Models;

public class Role : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();
}
