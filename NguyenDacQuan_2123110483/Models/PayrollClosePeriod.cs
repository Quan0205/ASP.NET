using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class PayrollClosePeriod : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [Range(1, 12)]
    public int PayrollMonth { get; set; }

    [Range(2000, 2100)]
    public int PayrollYear { get; set; }

    public bool IsClosed { get; set; }

    public DateTime? ClosedAt { get; set; }

    [ForeignKey(nameof(ClosedByUserAccount))]
    public int? ClosedByUserAccountId { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }

    public UserAccount? ClosedByUserAccount { get; set; }
}
