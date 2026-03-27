using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Models.UserManegement;


namespace Backend.Controllers
{
    [ApiController]
    [Route("api/um")]
    public class UserManagementController : ControllerBase
    {
        private readonly DBcontext _context;
        
        public UserManagementController(DBcontext context)
        {
            _context = context;
        }
        //1.1.1
        // optimal
        [HttpPost("ac/register")]
        public async Task<ActionResult> UserRegistration([FromBody] UserRegistrationRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.email) ||
                string.IsNullOrWhiteSpace(request.password) ||
                string.IsNullOrWhiteSpace(request.name) ||
                request.phone == 0)
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
            

            // Validate email format
            if (!request.email.Contains('@') || !request.email.Contains('.'))
                return BadRequest(new { code = ErrorCodes.User.InvalidEmailFormat, message = "Invalid email format." });

            // Check if email already exists
            if (await _context.Cur.AnyAsync(u => u.Email == request.email))
                return Conflict(new { code = ErrorCodes.User.AlreadyRegistered, message = "User already registered." });

            try
            {
                var user = new CUR
                {
                    Email    = request.email,
                    Password = request.password,
                    Salt     = string.Empty,
                    Name     = request.name,
                    Phone    = request.phone,
                    Clinic   = request.clinic,
                    Position = request.position,
                    Pfp      = request.pfp
                };
                

                _context.Cur.Add(user);
                
                await _context.SaveChangesAsync();
                

                return Ok(new { code = ErrorCodes.Success });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { code = ErrorCodes.App.DatabaseWriteFailure, message = "Database write failure." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { code = ErrorCodes.App.InternalServerError, message = "Internal server error." });
            }
        }
    }
}
