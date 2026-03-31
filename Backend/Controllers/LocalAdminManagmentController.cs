using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;


namespace Backend.Controllers;

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
  [HttpGet("create")]
  public async Task<ActionResult> CreateLocalAdmin([FromBody] CreateLocalAdminRequest request)
  {
    var LA = new CUR
    {
      Name = request.name,
      Phone = request.phone,
      Email = request.email,
      Position = request.position,
      Pfp = request.pfp,
      Clinic = request.clinic,
      Password = request.password,
      Salt     = string.Empty,
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
  [HttpGet("fetch")]
  public async Task<ActionResult> FetchLocalAdmin([FromBody] FetchClinicRequest request)
  {
    var LA =  _context.Cur.First(c => c.Email == request.email);
    Console.WriteLine(LA);
        
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
  [HttpGet("{uuid}/del")]
  public async Task<ActionResult> RemoveLocalAdmin(Guid uuid)
  {
    var LA =  _context.Cur.Find(uuid);
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