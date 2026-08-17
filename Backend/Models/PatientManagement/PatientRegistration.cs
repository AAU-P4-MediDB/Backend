namespace Backend.Models;

public class PatientRegistrationRequest
{
  public string name { get; set; } = null!;
  public string pronouns { get; set; } = null!;
  public Guid clinic { get; set; }
  public DateOnly birthdate { get; set; }
  public float weight { get; set; }
  public short height { get; set; }
  public bool bioGender { get; set; }
  public int cprKey { get; set; }
  public List<diagnosis>? diagnosis { get; set; } = null!;
  // Nullable: neither is collected at registration time, and PatientManagementController
  // defaults both when absent. Leaving these non-nullable made [ApiController]'s automatic
  // model validation reject the request before the controller body ever ran.
  public string? vitals { get; set; }
  public List<presrciptions>? prescriptions { get; set; }
  public string? pfp { get; set; }
}
 