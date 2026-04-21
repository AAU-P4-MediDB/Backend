using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using System.Runtime.InteropServices;
using Backend.Services;


namespace Backend.Controllers
{
  [ApiController]
  [Route("api/pm")]
  public class PatientManagementController : ControllerBase
  {
    private readonly DBcontext _context;
    private readonly string _aesKey;

    public PatientManagementController(DBcontext context, IConfiguration config)
    {
      _context = context;
      _aesKey = config["AES_KEY"] 
                ?? throw new InvalidOperationException("AES key not configured");
    }

    public void start()
    {
        startup.hashPasswords(_context);
    }

    //2.1
    [HttpPost("reg")]
        public async Task<ActionResult> PatientRegistration([FromBody] PatientRegistrationRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.name) ||
                string.IsNullOrWhiteSpace(request.pronouns)
                )
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
            Guid[] clinicStaffGuid = await _context.Cur
                .Where(c => c.Clinic == request.clinic)
                .Select(c => c.Uuid)
                .ToArrayAsync();

            try
            {
                
                var patient = new PR
                {
                    Name     = AesEncryption.Encrypt(request.name,_aesKey),
                    Pronouns = request.pronouns,
                    Clinic   = request.clinic,
                    Birthdate = request.birthdate,
                    Weight = request.weight,
                    BioGender = request.bioGender,
                    CprKey = request.cprKey,
                    Diagnosis = AesEncryption.EncryptList(request.diagnosis, _aesKey),
                    Vitals = request.vitals,
                    Prescriptions = request.prescriptions,
                    Pfp      = request.pfp,
                    DrPerms = perms.CreateStartPerms(clinicStaffGuid)
                };
                

                _context.Pr.Add(patient);
                
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

        //2.2
        [HttpDelete("{patient}/del")] 
        
          public async Task<ActionResult> PatientDelete(Guid patient)
          {
            var Patient = _context.Pr.Find(patient);

            _context.Pr.Remove(Patient);

            await _context.SaveChangesAsync();
                
            return Ok(new { code = ErrorCodes.Success });
          }
          
        //2.3.2
        [HttpPost("assignPat/confd")]

          public async Task<ActionResult> PatientAssignment([FromBody] PatientAssignmentRequest request)
        {
            var user = _context.Pr.Find(request.uuid_pt);

            user.Doctor = request.uuid_dr;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = ErrorCodes.Success
            });
        } 
  }
}

