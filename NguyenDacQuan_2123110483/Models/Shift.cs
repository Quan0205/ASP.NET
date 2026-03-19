using System.ComponentModel.DataAnnotations;

namespace CoffeeHRM.Models;

public class Shift : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string ShiftCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ShiftName { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int GraceMinutes { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
