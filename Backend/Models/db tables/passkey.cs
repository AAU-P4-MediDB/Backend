
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("user_passkeys")]
public class Passkey
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();


  [Column("user_uuid")]
  public Guid UserUuid { get; set; }


  [Required]
  [Column("credential_id")]
  public string CredentialId { get; set; } = null!;


  [Required]
  [Column("public_key")]
  public string PublicKey { get; set; } = null!;


  [Column("sign_count")]
  public long SignCount { get; set; }


  [Column("created")]
  public DateTime Created { get; set; } = DateTime.UtcNow;
}
[Table("user_passkeys")]
public class UserPasskey
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();


  [Column("user_uuid")]
  public Guid UserUuid { get; set; }


  [Required]
  [Column("credential_id")]
  public string CredentialId { get; set; } = null!;


  [Required]
  [Column("public_key")]
  public string PublicKey { get; set; } = null!;


  [Column("sign_count")]
  public long SignCount { get; set; }


  [Column("created")]
  public DateTime Created { get; set; } = DateTime.UtcNow;
}