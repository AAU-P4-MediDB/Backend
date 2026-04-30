namespace Backend.Models;

public class presrciptions
{
  public int date { get; set; }
  public string name { get; set; } = null!;
  public string? dosage { get; set; }
  public string? instructions { get; set; }
  public string status { get; set; } = "active";
  public int duration { get; set; }
}