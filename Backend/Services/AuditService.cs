using Backend.Models;

namespace Backend.Services;

public class AuditService
{
  private readonly DBcontext _context;

  public AuditService(DBcontext context)
  {
    _context = context;
  }

  public async Task LogAsync(Guid userUuid, Guid patientUuid, string action, string resource)
  {
    _context.AuditLog.Add(new AuditLog
    {
      UserUuid = userUuid,
      PatientUuid = patientUuid,
      Action = action,
      Resource = resource
    });

    await _context.SaveChangesAsync();
  }
}
