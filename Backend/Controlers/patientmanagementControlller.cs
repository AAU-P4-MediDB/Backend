using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;


namespace Backend.Controllers
{
  [ApiController]
  [Route("api/pm")]
  public class PatientManagementControlller : ControllerBase
  {
    private readonly DBcontext _context;

    public PatientManagementControlller(DBcontext context)
    {
      _context = context;
    }
    
    
    
    
    
    
    
  }
}

