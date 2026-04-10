namespace Backend.Models;

public class PermRequest
{
 public string pt_cpr { get; set; } = string.Empty!;
 public Guid dr_id { get; set; }
 public int perm_int { get; set; }
 public string? note { get; set; }
}
