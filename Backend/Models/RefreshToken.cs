public class RefreshToken
{
  public int Id { get; set; }

  public Guid UserUuid { get; set; }

  public string TokenHash { get; set; } = "";

  public DateTime Expires { get; set; }

  public DateTime Created { get; set; } = DateTime.UtcNow;

  public DateTime? Revoked { get; set; }


  public bool IsActive =>
    Revoked == null &&
    DateTime.UtcNow < Expires;
}