using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;


namespace Backend.Controllers;

[Authorize(Policy = "SystemAdminOnly")]
[ApiController]
[Route("api/sudo/lam")]
public class LocalAdminManagementController : ControllerBase
{
  private readonly DBcontext _context;

  public LocalAdminManagementController(DBcontext context)
  {
    _context = context;
  }


  //4.2.1
  [HttpPost("create")]
  public async Task<ActionResult> CreateLocalAdmin([FromBody] CreateLocalAdminRequest request)
  {
    string salt = hashing.GenerateSalt();

    var LA = new CUR
    {
      Name = request.name,
      Phone = request.phone,
      Email = request.email,
      Position = request.position,
      Pfp = request.pfp,
      Clinic = request.clinic,
      Password = hashing.HashPassword(request.password, salt),
      Salt     = salt,
      // Timeline is a NOT NULL json column with no default — omitting it
      // fails every local-admin creation with a DB write error.
      Timeline = new List<TimeLine>()
    };

    _context.Cur.Add(LA);
    await _context.SaveChangesAsync();
    Console.WriteLine(LA);

    return Ok(new
      {
        code = ErrorCodes.Success,
      }
    );
  }
  
  
  //4.2.3
  [HttpPost("fetch")]
  public async Task<ActionResult> FetchLocalAdmin([FromBody] FetchClinicRequest request)
  {
    var LA =  _context.Cur.FirstOrDefault(c => c.Email == request.email);

    if (LA == null)
    {
      return BadRequest(new {code = ErrorCodes.User.UserNotFound});
    }
        
    return Ok(new
      {
        code = ErrorCodes.Success,
        name = LA.Name,
        uuid = LA.Uuid,
        email = LA.Email,
        phone = LA.Phone,
        clinic = LA.Clinic,
        pfp = LA.Pfp,
      }
    );
  }
  
  
  //4.2.2
  [HttpDelete("{uuid}/del")]
  public async Task<ActionResult> RemoveLocalAdmin(Guid uuid)
  {
    var LA =  _context.Cur.Find(uuid);
    if (LA == null)
      return NotFound(new { code = ErrorCodes.User.UserNotFound });
    Console.WriteLine(LA);

    _context.Cur.Remove(LA);
    await _context.SaveChangesAsync();
        
        
    return Ok(new
      {
        code = ErrorCodes.Success,
      }
    );
  }
  
}