using System.Text.Json;
namespace Backend.Models;

public class VitalsUpdateRequest
{
  public JsonElement vitals { get; set; }
}