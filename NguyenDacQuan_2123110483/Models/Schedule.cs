using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Schedule : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(Shift))]
    public int ShiftId { get; set; }

    [DataType(DataType.Date)]
    public DateTime ScheduleDate { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }

    public Employee? Employee { get; set; }
    public Shift? Shift { get; set; }
    public Attendance? Attendance { get; set; }
    public ICollection<ShiftSwapRequest> RequestedShiftSwaps { get; set; } = new List<ShiftSwapRequest>();
    public ICollection<ShiftSwapRequest> TargetShiftSwaps { get; set; } = new List<ShiftSwapRequest>();
}
