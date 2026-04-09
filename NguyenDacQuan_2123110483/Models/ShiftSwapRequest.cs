using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class ShiftSwapRequest : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(RequestEmployee))]
    public int RequestEmployeeId { get; set; }

    [ForeignKey(nameof(TargetEmployee))]
    public int TargetEmployeeId { get; set; }

    [ForeignKey(nameof(RequestSchedule))]
    public int RequestScheduleId { get; set; }

    [ForeignKey(nameof(TargetSchedule))]
    public int TargetScheduleId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public ShiftSwapStatus Status { get; set; } = ShiftSwapStatus.Pending;

    [ForeignKey(nameof(ReviewedByUserAccount))]
    public int? ReviewedByUserAccountId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(250)]
    public string? DecisionNote { get; set; }

    public Employee? RequestEmployee { get; set; }
    public Employee? TargetEmployee { get; set; }
    public Schedule? RequestSchedule { get; set; }
    public Schedule? TargetSchedule { get; set; }
    public UserAccount? ReviewedByUserAccount { get; set; }
}
