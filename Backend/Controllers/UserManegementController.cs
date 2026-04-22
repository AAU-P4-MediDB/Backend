using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

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
        
        //1.1.2
        [HttpPost("ac/login")]
        public async Task<ActionResult> UserLogin([FromBody] UserLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.email) ||
                string.IsNullOrWhiteSpace(request.password)) 
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
            
            var User =  _context.Cur.First(c => c.Email == request.email && c.Password == request.password);
        
            return Ok(new
            {
                code = ErrorCodes.Success,
            });
            
        }
        
        //1.4
        [HttpPost("{User}/reset")]

        public async Task<ActionResult> UserPassWordReset([FromBody] UserPassWordResetRequest request,Guid User_ID)
        {
            if (string.IsNullOrWhiteSpace(request.email) ||
                string.IsNullOrWhiteSpace(request.new_pass)) 
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

            var user = _context.Cur.Find(User_ID);

            user.Password = request.new_pass;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = ErrorCodes.Success
            });
        }
        
        // 1.2
        [HttpDelete("{User}/del")]

        public async Task<ActionResult> User(Guid User)
        {

            var user = _context.Cur.Find(User);
            _context.Cur.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = ErrorCodes.Success
            });
        }
        

        //1.3
        [HttpPost("fetch")]

        public async Task<ActionResult> UserFetching([FromBody] UserFetchingRequest request)
        {
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            if (request.uuid != null)
            {
                Guid? parsedUuid = null;

                if (!string.IsNullOrEmpty(request.uuid) && Guid.TryParse(request.uuid, out var guid))
                {
                    parsedUuid = guid;
                    
                    var user = await _context.Cur
                        .Where(c => c.Uuid == parsedUuid)
                        .FirstOrDefaultAsync();

                    return Ok(new
                    {
                        code = ErrorCodes.Success,
                        uuid = user.Uuid,
                        name = user.Name,
                        clinic = user.Clinic,
                        position = user.Position,
                        pfp = user.Pfp,
                        phone = user.Phone
                    });
                }

                return BadRequest(new {code = ErrorCodes.Misc.InvalidUuidFormat, message = "Invalid uuid format." });
            } 
            if (request.email != null)
            {
                var user = await _context.Cur
                    .Where(c => c.Email == request.email)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    code = ErrorCodes.Success,
                    uuid = user.Uuid,
                    name = user.Name,
                    clinic = user.Clinic,
                    position = user.Position,
                    pfp = user.Pfp,
                    phone = user.Phone
                });
            }
            return BadRequest("No identifier provided");
        }
    }
}
