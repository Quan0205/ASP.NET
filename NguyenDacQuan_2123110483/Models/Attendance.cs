using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Attendance : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(Shift))]
    public int? ShiftId { get; set; }

    [ForeignKey(nameof(Schedule))]
    public int? ScheduleId { get; set; }

    [DataType(DataType.Date)]
    public DateTime AttendanceDate { get; set; }

    public DateTime? CheckInAt { get; set; }
    public DateTime? CheckOutAt { get; set; }

    public int LateMinutes { get; set; } = 0;
    public int WorkingMinutes { get; set; } = 0;
    public int OvertimeMinutes { get; set; } = 0;
    public int EarlyLeaveMinutes { get; set; } = 0;

    [Required]
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;

    [StringLength(250)]
    public string? Note { get; set; }

    public Employee? Employee { get; set; }
    public Shift? Shift { get; set; }
    public Schedule? Schedule { get; set; }
}
