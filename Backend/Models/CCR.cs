using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("CCR")]
public class CCR
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();
 
  [Required]
  [MaxLength(1000)]
  [Column("name")]
  public string Name { get; set; } = null!;
 
  [Required]
  [MaxLength(1000)]
  [Column("location")]
  public string Location { get; set; } = null!;
 
  [MaxLength(100)]
  [Column("email")]
  public string? Email { get; set; }
 
  [Required]
  [Column("cvr")]
  [Range(0, 99999999)]
  public int Cvr { get; set; }
 
  [Required]
  [Column("phone")]
  [Range(0, 99999999)]
  public int Phone { get; set; }
}