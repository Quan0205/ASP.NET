using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeHRM.Models;

public class RefreshToken : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(UserAccount))]
    public int UserAccountId { get; set; }

    [Required]
    [StringLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public bool IsUsed { get; set; }

    public UserAccount? UserAccount { get; set; }
}
