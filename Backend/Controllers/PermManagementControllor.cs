using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Backend.Controllers;

[Authorize(Policy = "DoctorOnly")]
[ApiController]
[Route("/api/dpm/perm/")]
public class PermManagementController : ControllerBase
{
  private readonly DBcontext _context;
  public PermManagementController(DBcontext context)
  {
    _context = context;
  }

  private Guid? GetUserUuid()
  {
    var str = User.FindFirstValue("sub")
           ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(str, out var guid) ? guid : null;
  }

  // 3.5.1
  [HttpPost("{uuid}/update")]
  public async Task<IActionResult> UpdateDrPerms(Guid uuid, [FromBody] UpdateDrPermsRequest request)
  {
    var pr = await _context.Pr.FindAsync(uuid);

    if (pr == null)
      return NotFound();

    // Only the patient's own assigned doctor may grant/revoke access to other doctors.
    var callerUuid = GetUserUuid();
    if (callerUuid == null)
      return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

    if (pr.Doctor != callerUuid)
      return StatusCode(403, new { code = ErrorCodes.Security.Forbidden, message = "Only the assigned doctor can manage permissions for this patient." });

    // Apply updates dynamically
    foreach (var (doctorId, permInt) in request.Updates)
    {
      pr.DrPerms[doctorId.ToString()] = permInt; // add or update
    }

    // IMPORTANT: force EF to detect change
    _context.Entry(pr).Property(p => p.DrPerms).IsModified = true;

    await _context.SaveChangesAsync();

    return Ok(new
    {
      code = ErrorCodes.Success,
      message = "Permissions updated",
    });
  }

  // 3.5.2
  [HttpGet("{uuid}/get")]
  public async Task<IActionResult> FetchDrPerms(Guid uuid)
  {
    var pr = await _context.Pr.FindAsync(uuid);

    if (pr == null)
      return NotFound();

    // Only the patient's own assigned doctor may see who else has been granted access.
    var callerUuid = GetUserUuid();
    if (callerUuid == null)
      return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

    if (pr.Doctor != callerUuid)
      return StatusCode(403, new { code = ErrorCodes.Security.Forbidden, message = "Only the assigned doctor can view permissions for this patient." });

    return Ok(new
    {
      code = ErrorCodes.Success,
      message = "Permissions fetched",
      drPerms = pr.DrPerms,
    });
  }

  // Doctors who share a clinic with one of the caller's own patients,
  // excluding the caller — used to populate the "share with" dropdown on
  // the permissions page. This mirrors PatientRegistration's own notion of
  // "colleague" (Backend/Backend/Controllers/PatientManagementController.cs),
  // which seeds a new patient's DrPerms from staff sharing the *patient's*
  // clinic — not the registering doctor's own CUR.Clinic. Those two aren't
  // guaranteed to match (a doctor can be assigned patients from a clinic
  // other than the one on their own staff record), so filtering by the
  // caller's own CUR.Clinic would wrongly hide real colleagues.
  [HttpGet("doctors")]
  public async Task<IActionResult> FetchClinicDoctors()
  {
    var callerUuid = GetUserUuid();
    if (callerUuid == null)
      return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

    var myPatientClinics = await _context.Pr
      .Where(p => p.Doctor == callerUuid)
      .Select(p => p.Clinic)
      .Distinct()
      .ToListAsync();

    var doctors = await _context.Cur
      .Where(c => c.Position == PositionType.Doctor
                  && c.Uuid != callerUuid
                  && myPatientClinics.Contains(c.Clinic))
      .Select(c => new
      {
        uuid = c.Uuid,
        name = c.Name,
        email = c.Email,
        position = c.Position.ToString(),
        clinic = c.Clinic,
      })
      .ToListAsync();

    return Ok(new
    {
      code = ErrorCodes.Success,
      doctors,
    });
  }

  //3.5.3
  [HttpPost("request")]
  public async Task<IActionResult> PermsRequest([FromBody] PermRequest request)
  {
    int[] cpr = request.pt_cpr.Split('-').Select(int.Parse).ToArray();

    DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);
      
      
    var  user = _context.Pr.FirstOrDefault(c => c.CprKey == cpr[1] &&  c.Birthdate == dateOnly);
    if (user == null)
      return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });
    
    user.DrPermRequests.Add(request);
    
    _context.Entry(user).Property(p => p.DrPermRequests).IsModified = true;
    
    await _context.SaveChangesAsync();
    
    return Ok(new { code = ErrorCodes.Success });
  }
  
  
  // 3.5.4
  [HttpGet("request/get/{uuid}")]
  public async Task<IActionResult> FetchDrPermsRequests(Guid uuid)
  {
    // The caller's own identity is authoritative, not the URL parameter,
    // so a doctor cannot read another doctor's pending permission requests
    // by guessing their uuid.
    var callerUuid = GetUserUuid();
    if (callerUuid == null)
      return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

    var dataArr = await _context.Pr
      .Where(c => c.Doctor == callerUuid)
      .Select(c => c.DrPermRequests)
      .ToListAsync();
    
    List<PermRequest> permRequests = new List<PermRequest>();

    foreach (var datas in dataArr)
    {
      foreach (var perm in datas)
      {
        permRequests.Add(perm);
      }
    }
    

    return Ok(new
    {
      code = ErrorCodes.Success,
      message = "Permissions fetched",
      dr_perm_requests =  permRequests,
    });
  }
}
