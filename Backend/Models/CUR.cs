using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public enum PositionType
{
  secretary,
  Nurse,
  Doctor,
  Local_administrator,
  System_administrator,
}

[Table("cur")]
public class CUR
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [MaxLength(200)]
  [Column("email")]
  public string Email { get; set; } = null!;

  [Required]
  [Column("password")]
  public string Password { get; set; } = null!;

  [Required]
  [Column("salt")]
  public string Salt { get; set; } = null!;

  [Required]
  [MaxLength(1000)]
  [Column("name")]
  public string Name { get; set; } = null!;

  [Required]
  [Column("position")]
  public PositionType Position { get; set; }

  [Column("pfp")]
  public string? Pfp { get; set; }

  [Column("clinic")]
  public Guid? Clinic { get; set; }

  [Required]
  [Range(0, 99999999)]
  [Column("phone")]
  public int Phone { get; set; }
  
  [Column("timeline", TypeName = "json")]
  public Dictionary<int, TimeLine> Timeline { get; set; }

  // Navigation property
  [ForeignKey("Clinic")]
  public CCR? ClinicNavigation { get; set; }
}