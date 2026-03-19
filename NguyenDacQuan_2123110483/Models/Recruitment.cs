using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Recruitment : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Branch))]
    public int? BranchId { get; set; }

    [Required]
    [StringLength(150)]
    public string PositionTitle { get; set; } = string.Empty;

    public DateTime OpenDate { get; set; } = DateTime.UtcNow;
    public DateTime? CloseDate { get; set; }

    [Required]
    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Open;

    [StringLength(500)]
    public string? Description { get; set; }

    public Branch? Branch { get; set; }
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
}
