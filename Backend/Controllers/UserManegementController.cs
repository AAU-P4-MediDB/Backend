using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Microsoft.AspNetCore.RateLimiting;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;



namespace Backend.Controllers
{
    [ApiController]
    [Route("api/um")]
    public class UserManagementController : ControllerBase
    {
        private readonly DBcontext _context;
        private readonly TokenService _tokenService;
        private readonly AuthService _auth;
        private readonly MfaService _mfa;

        public UserManagementController(DBcontext context, TokenService tokenService, AuthService auth, MfaService mfa)
        {
            _context = context;
            _tokenService = tokenService;
            _auth = auth;
            _mfa = mfa;
        }

        private Guid? GetUserUuid()
        {
            var str = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(str, out var guid) ? guid : null;
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
            string salt = hashing.GenerateSalt();

            try
            {
                var user = new CUR
                {
                    Email    = request.email,
                    Password = hashing.HashPassword(request.password, salt),
                    Salt     = salt,
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
        [EnableRateLimiting("login")]
        [HttpPost("ac/login")]
        public async Task<ActionResult> UserLogin([FromBody] LoginRequest request)
        {
            var result = await _auth.Login(request);

            if (result == null)
                return Unauthorized();

            if (result is MfaChallenge challenge)
                return StatusCode(202, challenge);

            return Ok(result);
        }
        

        //1.1.3 (make)
        [HttpPost("ac/refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var hash =
                _tokenService.HashRefreshToken(request.RefreshToken);


            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash);


            if (token == null || !token.IsActive)
                return Unauthorized();


            token.Revoked = DateTime.UtcNow;


            var user = await _context.Cur
                .FirstAsync(x => x.Uuid == token.UserUuid);


            var newAccess =
                _tokenService.GenerateToken(user);


            var newRefresh =
                _tokenService.GenerateRefreshToken(user);


            _context.RefreshTokens.Add(new RefreshToken
            {
                UserUuid = user.Uuid,
                TokenHash =
                    _tokenService.HashRefreshToken(newRefresh),
                Expires = DateTime.UtcNow.AddDays(7)
            });


            await _context.SaveChangesAsync();


            return Ok(new
            {
                accessToken = newAccess,
                refreshToken = newRefresh
            });
        }
        
        
        [EnableRateLimiting("login")]
        [HttpPost("ac/mfa/verify")]
        public async Task<IActionResult> Verify([FromBody] MfaVerifyRequest req)
        {
            var result = await _auth.VerifyMfa(req);

            if (result == null)
                return Unauthorized();

            return Ok(result);
        }
        
        //1.4
        [HttpPost("{User}/reset")]

        public async Task<ActionResult> UserPassWordReset([FromBody] UserPassWordResetRequest request,Guid User_ID)
        {
            if (string.IsNullOrWhiteSpace(request.email) ||
                string.IsNullOrWhiteSpace(request.new_pass)) 
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

            var user = _context.Cur.Find(User_ID);

            user.Password = hashing.HashPassword(request.new_pass, user.Salt);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = ErrorCodes.Success
            });
        }
        
        
        // 1.2
        [Authorize]
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
        

        [Authorize]
        [HttpGet("mfa/status")]
        public async Task<IActionResult> GetMfaStatus()
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var methods = await _mfa.GetMethods(userUuid.Value);
            return Ok(new
            {
                totpEnabled = methods.Contains("totp"),
                yubikeyEnabled = methods.Contains("yubikey")
            });
        }

        [Authorize]
        [HttpGet("mfa/totp/setup")]
        public async Task<IActionResult> SetupTotp()
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var result = await _mfa.SetupTotp(userUuid.Value);
            if (result == null) return NotFound();

            return Ok(new { secret = result.Value.secret, otpauthUri = result.Value.otpauthUri });
        }

        [Authorize]
        [HttpPost("mfa/totp/confirm")]
        public async Task<IActionResult> ConfirmTotp([FromBody] TotpConfirmRequest req)
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var ok = await _mfa.ConfirmTotp(userUuid.Value, req.Secret, req.Code);
            if (!ok) return BadRequest(new { error = "Invalid code." });
            return Ok(new { code = ErrorCodes.Success });
        }

        [Authorize]
        [HttpDelete("mfa/totp")]
        public async Task<IActionResult> DisableTotp()
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            await _mfa.DisableTotp(userUuid.Value);
            return Ok(new { code = ErrorCodes.Success });
        }

        [Authorize]
        [HttpGet("mfa/yubikey")]
        public async Task<IActionResult> GetYubikeys()
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var keys = await _mfa.GetYubikeys(userUuid.Value);
            return Ok(keys.Select(k => new { k.Uuid, k.PublicId, k.Label }));
        }

        [Authorize]
        [HttpPost("mfa/yubikey/register")]
        public async Task<IActionResult> RegisterYubikey([FromBody] YubikeyRegistrationRequest req)
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var (success, error) = await _mfa.RegisterYubikey(userUuid.Value, req.Otp, req.Label);
            if (!success) return BadRequest(new { error });
            return Ok(new { code = ErrorCodes.Success });
        }

        [Authorize]
        [HttpDelete("mfa/yubikey/{keyUuid}")]
        public async Task<IActionResult> RemoveYubikey(Guid keyUuid)
        {
            var userUuid = GetUserUuid();
            if (userUuid == null) return Unauthorized();

            var removed = await _mfa.RemoveYubikey(userUuid.Value, keyUuid);
            if (!removed) return NotFound();
            return Ok(new { code = ErrorCodes.Success });
        }

        //1.3
        [Authorize]
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
