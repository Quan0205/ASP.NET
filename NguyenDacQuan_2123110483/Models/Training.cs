using System.ComponentModel.DataAnnotations;

namespace CoffeeHRM.Models;

public class Training : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string TrainingCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string TrainingName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }

    [StringLength(100)]
    public string? Instructor { get; set; }

    public bool IsRequired { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeTraining> EmployeeTrainings { get; set; } = new List<EmployeeTraining>();
}
