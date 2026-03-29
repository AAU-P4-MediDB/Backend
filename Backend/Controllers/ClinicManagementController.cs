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
    [HttpGet("fc")]
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
}