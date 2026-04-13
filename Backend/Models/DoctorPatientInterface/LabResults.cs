using System.Text.Json;

namespace Backend.Models;

public class LabResults
{ 
  public string test { get; set; }
  public string ordered_by { get; set; }
  public int time { get; set; }
  public string status { get; set; }
  public JsonElement results { get; set; }
  public string notes { get; set; }
}