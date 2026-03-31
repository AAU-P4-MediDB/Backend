namespace Backend.Models;

public class CreateClinicRequest
{
  public string name { get; set; }
  public int phone { get; set; }
  public string? email { get; set; } = null;
  public string location { get; set; }
  public int cvr { get; set; }
  
}