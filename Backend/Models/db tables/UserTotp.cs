using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

[Table("user_totp")]
public class UserTotp
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  [Required]
  [Column("secret")]
  public string Secret { get; set; } = null!; // ENCRYPT THIS
}