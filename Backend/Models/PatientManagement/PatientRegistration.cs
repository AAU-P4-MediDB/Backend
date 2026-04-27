namespace Backend.Models;

public class PatientRegistrationRequest
{
  public string name { get; set; } = null!;
  public string pronouns { get; set; } = null!;
  public Guid clinic { get; set; }
  public DateOnly birthdate { get; set; }
  public float weight { get; set; }
  public bool bioGender { get; set; }
  public int cprKey { get; set; }
  public List<diagnosis>? diagnosis { get; set; } = null!;
  public string vitals { get; set; } = null!;
  public string? prescriptions { get; set; }
  public string? pfp { get; set; }
}
 