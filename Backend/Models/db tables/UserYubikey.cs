using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("user_yubikey")]
public class UserYubikey
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  // First 12 modhex characters of a YubiKey OTP uniquely identify the key
  [Required]
  [Column("public_id")]
  public string PublicId { get; set; } = null!;

  [Column("label")]
  public string? Label { get; set; }
}
