namespace Backend.Models;

public class TimeLine
{
  public DateOnly date { get; set; }
  public Guid PatientId { get; set; }
  public Guid doctor_accessing { get; set; }
  public string data_type { get; set; }
  public string changes { get; set; }
  public int severity { get; set; }
}