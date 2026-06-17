using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("user_recovery_codes")]
public class UserRecoveryCode
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  [Required]
  [Column("code_hash")]
  public string CodeHash { get; set; } = null!;

  [Column("used")]
  public bool Used { get; set; } = false;
}