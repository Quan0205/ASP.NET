using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Payroll : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeContract))]
    public int EmployeeContractId { get; set; }

    [Range(1, 12)]
    public int PayrollMonth { get; set; }

    [Range(2000, 2100)]
    public int PayrollYear { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WorkingHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HourlyRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AllowanceAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BonusAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PenaltyAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalary { get; set; }

    [Required]
    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    public DateTime? PaidDate { get; set; }

    public Employee? Employee { get; set; }
    public EmployeeContract? EmployeeContract { get; set; }
    public ICollection<PayrollDetail> PayrollDetails { get; set; } = new List<PayrollDetail>();
}
