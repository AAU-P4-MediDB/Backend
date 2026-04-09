using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Controllers;
[ApiController]
[Route("/api/dpm/perm/")]
public class PermManagementController : ControllerBase
{
  private readonly DBcontext _context;

  
  
  public PermManagementController(DBcontext context) 
  {
    _context = context;
  }

  // 3.5.1
  [HttpPost("{uuid}/update")]
  public async Task<IActionResult> UpdateDrPerms(Guid uuid, [FromBody] UpdateDrPermsRequest request)
  {
    var pr = await _context.Pr.FindAsync(uuid);

    if (pr == null)
      return NotFound();

 

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

    return Ok(new
    {
      code = ErrorCodes.Success,
      message = "Permissions fetched",
      drPerms = pr.DrPerms,
    });
  }

}
