using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("mfa_sessions")]
public class MfaSession
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  [Required]
  [Column("session_token")]
  public string SessionToken { get; set; } = null!;

  [Required]
  [Column("expires")]
  public DateTime Expires { get; set; }

  [Column("used")]
  public bool Used { get; set; } = false;
}