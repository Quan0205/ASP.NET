using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class KPI : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [Range(2000, 2100)]
    public int KpiYear { get; set; }

    [Range(1, 12)]
    public int KpiMonth { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Score { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal Target { get; set; }

    [Required]
    [StringLength(100)]
    public string Result { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Note { get; set; }

    public Employee? Employee { get; set; }
}
