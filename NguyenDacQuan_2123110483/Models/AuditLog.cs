using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class AuditLog : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(UserAccount))]
    public int? UserAccountId { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string TableName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RecordId { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? OldValues { get; set; }

    [StringLength(2000)]
    public string? NewValues { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public UserAccount? UserAccount { get; set; }
}
