using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class AttendanceAdjustment : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Attendance))]
    public int AttendanceId { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    public DateTime? RequestedCheckInAt { get; set; }
    public DateTime? RequestedCheckOutAt { get; set; }
    public AttendanceStatus? RequestedStatus { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    public AttendanceAdjustmentStatus Status { get; set; } = AttendanceAdjustmentStatus.Pending;

    [ForeignKey(nameof(ReviewedByUserAccount))]
    public int? ReviewedByUserAccountId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(250)]
    public string? DecisionNote { get; set; }

    public Attendance? Attendance { get; set; }
    public Employee? Employee { get; set; }
    public UserAccount? ReviewedByUserAccount { get; set; }
}
