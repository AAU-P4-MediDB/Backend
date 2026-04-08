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


  [HttpPut("{uuid}/update")]
  public async Task<IActionResult> UpdateDrPerms(Guid uuid, [FromBody] UpdateDrPermsRequest request)
  {
    var pr = await _context.Pr.FindAsync(uuid);

    if (pr == null)
      return NotFound();

    // Ensure dictionary exists
    if (pr.DrPerms == null)
      pr.DrPerms = new Dictionary<Guid, int>();

    // Apply updates dynamically
    foreach (var (doctorId, permInt) in request.Updates)
    {
      pr.DrPerms[doctorId] = permInt; // add or update
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

  [HttpPut("{uuid}/get")]
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
