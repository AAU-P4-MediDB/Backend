using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("audit_log")]
public class AuditLog
{
  [Key]
  [Column("id")]
  public long Id { get; set; }

  [Required]
  [Column("timestamp")]
  public DateTime Timestamp { get; set; } = DateTime.UtcNow;

  [Required]
  [Column("user_uuid")]
  public Guid UserUuid { get; set; }

  [Required]
  [Column("patient_uuid")]
  public Guid PatientUuid { get; set; }

  [Required]
  [Column("action")]
  public string Action { get; set; } = null!;

  [Required]
  [Column("resource")]
  public string Resource { get; set; } = null!;
}
