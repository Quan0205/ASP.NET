using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class EmployeeTraining : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(Training))]
    public int TrainingId { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    [Required]
    public EmployeeTrainingStatus Status { get; set; } = EmployeeTrainingStatus.Assigned;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Score { get; set; }

    public Employee? Employee { get; set; }
    public Training? Training { get; set; }
}
