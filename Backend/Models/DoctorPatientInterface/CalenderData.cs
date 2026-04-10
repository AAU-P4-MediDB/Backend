namespace Backend.Models;

public class CalenderData
{
  public string name { get; set; }
  public Guid PatientGuid { get; set; }
  public string cpr { get; set; }
  public string reason { get; set; }
  public int time { get; set; } //unixtime
  public string pfp { get; set; }
}