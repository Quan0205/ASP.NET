using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class Candidate : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Recruitment))]
    public int RecruitmentId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

    [Required]
    public CandidateStatus Status { get; set; } = CandidateStatus.Applied;

    [Column(TypeName = "decimal(5,2)")]
    public decimal? InterviewScore { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }

    public Recruitment? Recruitment { get; set; }
}
