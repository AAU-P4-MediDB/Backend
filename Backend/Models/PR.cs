using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("pr")]
public class PR
{
  [Key]
  [Column("uuid")]
  public Guid Uuid { get; set; } = Guid.NewGuid();

  [Required]
  [MaxLength(100)]
  [Column("name")]
  public string Name { get; set; } = null!;

  [Required]
  [Column("birthdate")]
  public DateOnly Birthdate { get; set; }

  [Required]
  [Range(0, 9999)]
  [Column("cpr_key")]
  public int CprKey { get; set; }

  [Required]
  [Column("bio_gender")]
  public bool BioGender { get; set; }

  [Column("pronouns")]
  public string? Pronouns { get; set; }

  [Column("clinic")]
  public Guid? Clinic { get; set; }

  [Column("doctor")]
  public Guid? Doctor { get; set; }

  [Required]
  [Column("weight")]
  public float Weight { get; set; }

  [Required]
  [Column("height")]
  public short Height { get; set; }

  [Column("diagnosis")]
  public List<string>? Diagnosis { get; set; }

  [Required]
  [Column("vitals", TypeName = "json")]
  public string Vitals { get; set; } = null!;

  [Column("prescriptions", TypeName = "json")]
  public string? Prescriptions { get; set; }

  [Column("pfp")]
  public string? Pfp { get; set; }

  [Required]
  [Column("journal", TypeName = "json")]
  public string Journal { get; set; } = null!;

  [Column("appointments", TypeName = "json")]
  public string? Appointments { get; set; }

  [Column("lab_results", TypeName = "json")]
  public string? LabResults { get; set; }

  // Navigation properties
  [ForeignKey("Clinic")]
  public CCR? ClinicNavigation { get; set; }

  [ForeignKey("Doctor")]
  public CUR? DoctorNavigation { get; set; }
}