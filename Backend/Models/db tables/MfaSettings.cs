using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("user_mfa")]
public class UserMfa
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  [Column("totp_enabled")]
  public bool TotpEnabled { get; set; } = false;

  [Column("passkey_enabled")]
  public bool PasskeyEnabled { get; set; } = false;

  [Column("yubikey_enabled")]
  public bool YubikeyEnabled { get; set; } = false;
}