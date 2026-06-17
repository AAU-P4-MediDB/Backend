using System.ComponentModel.DataAnnotations.Schema;

[Table("refreshtokens")]
public class RefreshToken
{
  [Column("id")]
  public int Id { get; set; }
  
  [Column("useruuid")]
  public Guid UserUuid { get; set; }

  [Column("tokenhash")]
  public string TokenHash { get; set; } = "";

  [Column("expires")]
  public DateTime Expires { get; set; }
  
  [Column("created")]
  public DateTime Created { get; set; } = DateTime.UtcNow;

  [Column("revoked")]
  public DateTime? Revoked { get; set; }


  public bool IsActive =>
    Revoked == null &&
    DateTime.UtcNow < Expires;
}