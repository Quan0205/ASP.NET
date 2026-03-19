using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class EmployeeContract : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string ContractNo { get; set; } = string.Empty;

    [Required]
    public ContractType ContractType { get; set; } = ContractType.FullTime;

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeRateMultiplier { get; set; } = 1.50m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal LatePenaltyPerMinute { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal EarlyLeavePenaltyPerMinute { get; set; }

    public decimal StandardDailyHours { get; set; } = 8m;

    public bool IsActive { get; set; } = true;

    public Employee? Employee { get; set; }
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
}
