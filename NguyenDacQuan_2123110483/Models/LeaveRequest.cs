using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class LeaveRequest : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Employee))]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(50)]
    public string LeaveType { get; set; } = "Paid";

    [StringLength(500)]
    public string? Reason { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal TotalDays { get; set; }

    public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;

    [ForeignKey(nameof(ReviewedByUserAccount))]
    public int? ReviewedByUserAccountId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(250)]
    public string? DecisionNote { get; set; }

    public Employee? Employee { get; set; }
    public UserAccount? ReviewedByUserAccount { get; set; }
}
