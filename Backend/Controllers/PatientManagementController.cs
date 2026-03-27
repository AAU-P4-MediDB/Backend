using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;


namespace Backend.Controllers
{
  [ApiController]
  [Route("api/pm")]
  public class PatientManagementController : ControllerBase
  {
    private readonly DBcontext _context;

    public PatientManagementController(DBcontext context)
    {
      _context = context;
    }
    
    
    
    
    
    
    
  }
}

