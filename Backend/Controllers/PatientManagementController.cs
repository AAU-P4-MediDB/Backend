using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;


namespace Backend.Controllers
{
    [Authorize(Policy = "ClinicStaff")]
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


        //2.1
        [HttpPost("reg")]
        public async Task<ActionResult> PatientRegistration([FromBody] PatientRegistrationRequest request)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.name) ||
                string.IsNullOrWhiteSpace(request.pronouns)
               )
                return BadRequest(new
                    { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
            Guid[] clinicStaffGuid = await _context.Cur
                .Where(c => c.Clinic == request.clinic)
                .Select(c => c.Uuid)
                .ToArrayAsync();

            try
            {

                var patient = new PR
                {
                    Name = AesEncryption.Encrypt(request.name, _aesKey),
                    Pronouns = request.pronouns,
                    Clinic = request.clinic,
                    Birthdate = request.birthdate,
                    Weight = request.weight,
                    BioGender = request.bioGender,
                    CprKey = request.cprKey,
                    // Every CPR-based lookup (usrfet/*, permissions, etc.) finds a
                    // patient via this hash, not the raw key — without it a newly
                    // registered patient is invisible to every doctor-facing endpoint.
                    CprKeyHash = hashing.HashSHA3_512(request.cprKey.ToString()),
                    Diagnosis = request.diagnosis,
                    // Vitals/Journal are NOT NULL columns but aren't collected at
                    // registration time, so default them to an empty JSON list
                    // instead of letting the insert fail with a DB write error.
                    Vitals = string.IsNullOrWhiteSpace(request.vitals) ? "[]" : request.vitals,
                    Journal = "[]",
                    Prescriptions = request.prescriptions,
                    Pfp = request.pfp,
                    DrPerms = perms.CreateStartPerms(clinicStaffGuid)
                };


                _context.Pr.Add(patient);

                await _context.SaveChangesAsync();


                return Ok(new { code = ErrorCodes.Success });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500,
                    new { code = ErrorCodes.App.DatabaseWriteFailure, message = "Database write failure." });
            }
            catch (Exception ex)
            {
                return StatusCode(500,
                    new { code = ErrorCodes.App.InternalServerError, message = "Internal server error." });
            }
        }

        //2.2
        [HttpDelete("{patient}/del")]

        public async Task<ActionResult> PatientDelete(Guid patient)
        {
            var Patient = _context.Pr.Find(patient);
            if (Patient == null)
                return NotFound(new { code = ErrorCodes.App.PatientNotFound });

            _context.Pr.Remove(Patient);

            await _context.SaveChangesAsync();

            return Ok(new { code = ErrorCodes.Success });
        }

        //2.3.1
        [HttpPost("fetch")]
        public async Task<ActionResult> PatientFetch([FromBody] ptdataFetchingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CPR_pt))
                return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

            var parts = request.CPR_pt.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var cprKey))
                return BadRequest(new { code = ErrorCodes.User.InvalidCpr });

            var cprhash = hashing.HashSHA3_512(cprKey.ToString());

            var patient = await _context.Pr.FirstOrDefaultAsync(c => c.CprKeyHash == cprhash);
            if (patient == null)
                return NotFound(new { code = ErrorCodes.User.UserNotFound });

            return Ok(new
            {
                code = ErrorCodes.Success,
                uuid = patient.Uuid,
                name = patient.Name,
                pronouns = patient.Pronouns,
                clinic = patient.Clinic,
                doctor = patient.Doctor
            });
        }

        //2.3.2
        [HttpPost("assignPat/confd")]

        public async Task<ActionResult> PatientAssignment([FromBody] PatientAssignmentRequest request)
        {
            var user = _context.Pr.Find(request.uuid_pt);
            if (user == null)
                return NotFound(new { code = ErrorCodes.App.PatientNotFound });

            user.Doctor = request.uuid_dr;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = ErrorCodes.Success
            });
        }
    }
}

