namespace Backend.Models;

public class CreateLocalAdminRequest
{
  public string name { get; set; }
  public string password { get; set; }
  public string email { get; set; }
  public int phone { get; set; }
  public string? pfp { get; set; }
  public PositionType position { get; set; }
  
  public Guid clinic { get; set; }
}