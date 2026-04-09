using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Employee : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public GenderType Gender { get; set; } = GenderType.Other;

    public DateTime? DateOfBirth { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [ForeignKey(nameof(Branch))]
    public int BranchId { get; set; }

    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public Branch? Branch { get; set; }
    public Role? Role { get; set; }
    public ICollection<EmployeeContract> EmployeeContracts { get; set; } = new List<EmployeeContract>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();
    public ICollection<KPI> KPIs { get; set; } = new List<KPI>();
    public ICollection<EmployeeTraining> EmployeeTrainings { get; set; } = new List<EmployeeTraining>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<ShiftSwapRequest> RequestedShiftSwaps { get; set; } = new List<ShiftSwapRequest>();
    public ICollection<ShiftSwapRequest> TargetShiftSwaps { get; set; } = new List<ShiftSwapRequest>();
    public ICollection<AttendanceAdjustment> AttendanceAdjustments { get; set; } = new List<AttendanceAdjustment>();
    public UserAccount? UserAccount { get; set; }
}
