using Backend.Models;

namespace Backend.Models.UserManegement;

public class UserRegistrationRequest
{
  public string email { get; set; } = null!;
  public string password { get; set; } = null!;
  public string name { get; set; } = null!;
  public string clinic { get; set; } = null!;
  public string pfp { get; set; } = null!;
  public PositionType position { get; set; }

  public int phone { get; set; }
}
 