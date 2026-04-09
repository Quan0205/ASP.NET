using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class UserAccount : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(Role))]
    public int RoleId { get; set; }

    [ForeignKey(nameof(SystemRole))]
    public int? SystemRoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public Employee? Employee { get; set; }
    public Role? Role { get; set; }
    public SystemRole? SystemRole { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Payroll> ApprovedPayrolls { get; set; } = new List<Payroll>();
    public ICollection<Payroll> ClosedPayrolls { get; set; } = new List<Payroll>();
    public ICollection<LeaveRequest> ReviewedLeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<ShiftSwapRequest> ReviewedShiftSwapRequests { get; set; } = new List<ShiftSwapRequest>();
    public ICollection<AttendanceAdjustment> ReviewedAttendanceAdjustments { get; set; } = new List<AttendanceAdjustment>();
}
