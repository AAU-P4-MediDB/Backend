namespace Backend.Models;

public class UserLoginRequest
{
  public string email { get; set; } = null!;
  public string password { get; set; } = null!;
}