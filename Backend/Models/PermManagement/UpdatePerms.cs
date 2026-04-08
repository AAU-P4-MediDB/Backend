namespace Backend.Models;

public class UpdateDrPermsRequest
{
  public Dictionary<Guid, int> Updates { get; set; } = new();
}