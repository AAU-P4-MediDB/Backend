using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;

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
    var dataArr = await _context.Pr
      .Where(c => c.Doctor == uuid)
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
