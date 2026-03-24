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
        var community =  _context.Ccr.First(c => c.Email == request.email);
        Console.WriteLine(community);
        
        return Ok(community);
    }

}