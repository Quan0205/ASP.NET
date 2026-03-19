using System.ComponentModel.DataAnnotations;

namespace CoffeeHRM.Models;

public class Branch : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string BranchCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string BranchName { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Recruitment> Recruitments { get; set; } = new List<Recruitment>();
}
