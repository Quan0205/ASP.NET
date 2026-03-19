using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class PayrollDetail : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Payroll))]
    public int PayrollId { get; set; }

    [Required]
    public PayrollDetailType DetailType { get; set; }

    public int? AttendanceId { get; set; }
    public int? ScheduleId { get; set; }
    public int? SourceReferenceId { get; set; }

    [Required]
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    [StringLength(250)]
    public string? Note { get; set; }

    public Payroll? Payroll { get; set; }
    public Attendance? Attendance { get; set; }
    public Schedule? Schedule { get; set; }
}
