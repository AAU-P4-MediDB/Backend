using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Controllers;

[ApiController]
[Route("api/sudo")]
public class ClinicManagementController : ControllerBase
{
    private readonly DBcontext _context;

    public ClinicManagementController(DBcontext context)
    {
        _context = context;
    }

    //4.1.2
    [HttpPost("cc")]
    public async Task<ActionResult> CreateClinic([FromBody] CreateClinicRequest request)
    {
        var clinic = new CCR
        {
            Name = request.name,
            Phone = request.phone,
            Email = request.email,
            Location = request.location,
            Cvr = request.cvr,
        };
        
        _context.Ccr.Add(clinic);
        await _context.SaveChangesAsync();
        Console.WriteLine(clinic);
        
        return Ok(new
            {
                code = ErrorCodes.Success,
            }
        );
    }
    //4.1.2
    [HttpPost("fc")]
    public async Task<ActionResult> FetchClinic([FromBody] FetchClinicRequest request)
    {
        var clinic =  _context.Ccr.First(c => c.Email == request.email);
        Console.WriteLine(clinic);
        
        return Ok(new
            {
                code = ErrorCodes.Success,
                name = clinic.Name,
                uuid = clinic.Uuid,
                location = clinic.Location,
                email = clinic.Email,
                phone = clinic.Phone,
                cvr = clinic.Cvr
            });
    }
    
    
    //4.1.3
    [HttpDelete("dc/{uuid}")]
    public async Task<ActionResult> RemoveClinic(Guid uuid)
    {
        var clinic =  _context.Ccr.Find(uuid);
        Console.WriteLine(clinic);
        
        _context.Ccr.Remove(clinic);
        await _context.SaveChangesAsync();
        
        return Ok(new
            {
                code = ErrorCodes.Success,
            }
        );
    }
    
    

}