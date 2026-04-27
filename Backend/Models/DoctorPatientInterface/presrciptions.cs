namespace Backend.Models;

public class presrciptions
{
  public string date { get; set; } = null!;
  public string name { get; set; } = null!;
  public string? dosage { get; set; }
  public string? instructions { get; set; }
  public string status { get; set; } = "active";
}