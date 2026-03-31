using System.Text.Json;
namespace Backend.Models;

public class VitalsUpdateRequest
{
  public JsonElement vitals { get; set; }
}

public class JournalUpdateRequest
{
  public JsonElement journal { get; set; }
}
public class PrescriptionUpdateRequest
{
  public JsonElement prescriptions { get; set; }
}
public class DiagnosesUpdateRequest
{
  public string diagnoses { get; set; }
}
public class AppointmentUpdateRequest
{
  public JsonElement appointment { get; set; }
}

public class LabResultUpdateRequest
{
  public JsonElement lab_result { get; set; }
}
public class InfoUpdateRequest
{
  public int cpr_key { get; set; }
  public string? name { get; set; }
  public string? pronouns { get; set; }
  public int bday { get; set; }
  public bool bio_sex { get; set; }
  public string? pfp { get; set; }
}

