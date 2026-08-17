using Backend.Models;

namespace Backend.Models;

public class UserRegistrationRequest
{
  public string email { get; set; } = null!;
  public string password { get; set; } = null!;
  public string name { get; set; } = null!;
  public Guid clinic { get; set; }
  public string pfp { get; set; } = String.Empty;
  public PositionType position { get; set; }
  public int phone { get; set; }
}
 