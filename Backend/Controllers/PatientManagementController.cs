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

            // The DB rejects height <= 0 via a check constraint — validate it
            // here first so the caller gets a clear reason instead of a raw
            // database error.
            if (request.height <= 0)
                return BadRequest(new
                    { code = ErrorCodes.User.InvalidFieldFormat, message = "Height must be a positive value." });

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
                    Height = request.height,
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
                    // Same story as Vitals/Journal: these are non-nullable list
                    // columns with no default, aren't collected at registration,
                    // and request.prescriptions is null unless a caller sends it.
                    Prescriptions = request.prescriptions ?? new List<presrciptions>(),
                    Appointments = new List<CalenderData>(),
                    LabResults = new List<LabResults>(),
                    Pfp = request.pfp,
                    DrPerms = perms.CreateStartPerms(clinicStaffGuid)
                };


                _context.Pr.Add(patient);

                await _context.SaveChangesAsync();


                return Ok(new { code = ErrorCodes.Success });
            }
            catch (DbUpdateException ex)
            {
                // The client only gets a generic message (avoid leaking schema/query
                // details), but the real Postgres error — usually in InnerException —
                // is what actually explains a write failure like this.
                Console.WriteLine("PatientRegistration DbUpdateException: " + ex);
                return StatusCode(500,
                    new { code = ErrorCodes.App.DatabaseWriteFailure, message = "Database write failure." });
            }
            catch (Exception ex)
            {
                Console.WriteLine("PatientRegistration Exception: " + ex);
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

