using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace Backend.Controllers
{

  [Authorize(Policy = "DoctorOnly")]
  [ApiController]
  [Route("api/dpm")]
  public class DoctorPatientInterfaceController : ControllerBase
  {
    private readonly DBcontext _context;
    private readonly string _aesKey;
    private readonly AuditService _audit;

    public DoctorPatientInterfaceController(DBcontext context, IConfiguration config, AuditService audit)
    {
      _context = context;
      _aesKey = config["AES_KEY"]
                ?? throw new InvalidOperationException("AES key not configured");
      _audit = audit;
    }

    private Guid? GetUserUuid()
    {
      var str = User.FindFirstValue("sub")
             ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
      return Guid.TryParse(str, out var guid) ? guid : null;
    }

    // A doctor may access a specific category of a patient's record if
    // they are the assigned doctor (full access), or if the assigned
    // doctor has granted them that specific read/write permission bit.
    private static bool HasPatientAccess(PR patient, Guid callerUuid, PermCategory category, PermAction action)
    {
      if (patient.Doctor == callerUuid)
        return true;

      if (patient.DrPerms == null || !patient.DrPerms.TryGetValue(callerUuid.ToString(), out var permInt))
        return false;

      return perms.HasCategoryPermission(permInt, category, action);
    }

    private ActionResult? AuthorizePatientAccess(PR patient, PermCategory category, PermAction action)
    {
      var callerUuid = GetUserUuid();
      if (callerUuid == null)
        return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

      if (!HasPatientAccess(patient, callerUuid.Value, category, action))
        return StatusCode(403, new { code = ErrorCodes.Security.Forbidden, message = "You do not have access to this data for this patient." });

      return null;
    }

    //3.1.1
    [HttpPost("usrfet/vitals")]
    public async Task<ActionResult> VitalsFetching([FromBody] ptdataFetchingRequest request)
    {
      
      if (string.IsNullOrWhiteSpace(request.CPR_pt)) 
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
      
      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);
      
      
      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.Vitals, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        vitals = user.Vitals
      });
    }
    
    //3.1.2
    [HttpPost("usrfet/journal")]
    public async Task<ActionResult> JournalFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
      
      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);
      
      
      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.Journal, PermAction.Read);
      if (authError != null) return authError;

      var viewerUuid = GetUserUuid();
      if (viewerUuid != null)
        await _audit.LogAsync(viewerUuid.Value, user.Uuid, "View", "Journal");

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        journal = user.Journal
      });
    }
    
    //3.1.3
    [HttpPost("usrfet/prescription")]
    public async Task<ActionResult> PrescriptionFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);


      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.Prescription, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        Prescription = user.Prescriptions
      });
    }

    //3.1.4
    [HttpPost("usrfet/diagnosis")]
    public async Task<ActionResult> DiagnosisFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);


      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.Diagnosis, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        diagnosis = user.Diagnosis
      });
    }

    //3.1.5
    [HttpPost("usrfet/appointment")]
    public async Task<ActionResult> AppointmentFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);
      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.Appointments, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        appointment = user.Appointments
      });
    }
    
    //3.1.6
    [HttpPost("usrfet/info")]
    public async Task<ActionResult> InfoFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var parts = request.CPR_pt.Split('-');

      if (parts.Length != 2 ||
          !int.TryParse(parts[0], out var cprDatePart) ||
          !int.TryParse(parts[1], out var cprKey))
      {
        return BadRequest(new { code = ErrorCodes.User.InvalidCpr });
      }

      DateOnly dateOnly = Parser.Parsebirthdate(cprDatePart);
      
      var cprhash = hashing.HashSHA3_512(cprKey.ToString());

      var user = await _context.Pr
        .FirstOrDefaultAsync(c => c.CprKeyHash == cprhash);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.PersonInfo, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        name = user.Name,
        pronouns = user.Pronouns,
        bday = user.Birthdate,
        biosex = user.BioGender,
        clinic = user.Clinic,
        pfp = user.Pfp
      });
    }
    //3.1.7
    [HttpPost("usrfet/labresult")]
    public async Task<ActionResult> LabResultFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt))
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      DateOnly dateOnly = Parser.Parsebirthdate(cpr[0]);


      var cprhash = hashing.HashSHA3_512(cpr[1].ToString());

      var user = _context.Pr.FirstOrDefault(c => c.CprKeyHash == cprhash);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound, message = "User not found." });

      var authError = AuthorizePatientAccess(user, PermCategory.LabResults, PermAction.Read);
      if (authError != null) return authError;

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        lab_results = user.LabResults
      });
    }
    
    
    //3.2.1
    [HttpPost("usrup/{uuid}/vital")]
    public async Task<ActionResult> UpdateVitals(Guid uuid, [FromBody] VitalsUpdateRequest request)
    {
      if (request.vitals.ValueKind == JsonValueKind.Undefined)
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.Vitals, PermAction.Write);
      if (authError != null) return authError;

      List<JsonElement> vitalsList;

      if (string.IsNullOrEmpty(user.Vitals))
      {
        vitalsList = new List<JsonElement>();
      }
      else
      {
        vitalsList = JsonSerializer.Deserialize<List<JsonElement>>(user.Vitals);
      }

      // Add new unknown JSON object
      vitalsList.Add(request.vitals);

      // Save back
      user.Vitals = JsonSerializer.Serialize(vitalsList);

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    
    //3.2.2
    [HttpPost("usrup/{uuid}/Journal")]
    public async Task<ActionResult> UpdateJournal(Guid uuid, [FromBody] JournalUpdateRequest request)
    {
      if (request.journal.ValueKind == JsonValueKind.Undefined)
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);
      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.Journal, PermAction.Write);
      if (authError != null) return authError;

      List<JsonElement> journalList;

      if (string.IsNullOrEmpty(user.Journal))
      {
        journalList = new List<JsonElement>();
      }
      else
      {
        journalList = JsonSerializer.Deserialize<List<JsonElement>>(user.Journal);
      }

      // Add new unknown JSON object
      journalList.Add(request.journal);

      // Save back
      user.Journal = JsonSerializer.Serialize(journalList);

      await _context.SaveChangesAsync();

      var editorUuid = GetUserUuid();
      if (editorUuid != null)
        await _audit.LogAsync(editorUuid.Value, user.Uuid, "Edit", "Journal");

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.3
    [HttpPost("usrup/{uuid}/prescription")]
    public async Task<ActionResult> Updateprescription(Guid uuid, [FromBody] presrciptions request)
    {

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.Prescription, PermAction.Write);
      if (authError != null) return authError;

      List<presrciptions> prescriptionList;

      if (string.IsNullOrEmpty(user.Journal))
      {
        prescriptionList = new List<presrciptions>();
      }
      else
      {
        prescriptionList = user.Prescriptions;
      }

    // Add new unknown JSON object
      prescriptionList.Add(request);

      // Save back


      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    
    //3.2.4 diagnosis
    [HttpPost("usrup/{uuid}/diagnosis")]
    public async Task<ActionResult> Updatediagnosis(Guid uuid, [FromBody] diagnosis request)
    {
      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.Diagnosis, PermAction.Write);
      if (authError != null) return authError;

      List<diagnosis> diagnosisList;

      if (user.Diagnosis == null || !user.Diagnosis.Any())
      {
        diagnosisList = new List<diagnosis>();
      }
      else
      {
        diagnosisList = user.Diagnosis;
      }

      
      diagnosisList.Add(request);

      // Save back
      user.Diagnosis = diagnosisList;

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    
    //3.2.5 appointment
    [HttpPost("usrup/{uuid}/appointment")]
    public async Task<ActionResult> Updateappointment(Guid uuid, [FromBody] CalenderData request)
    {

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.Appointments, PermAction.Write);
      if (authError != null) return authError;

      // Save back
      user.Appointments.Add(request);
      
      _context.Entry(user).Property(p => p.Appointments).IsModified = true;

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.7 labresult
    [HttpPost("usrup/{uuid}/labresult")]
    public async Task<ActionResult> Updatelabresult(Guid uuid, [FromBody] LabResults request)
    {
      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.LabResults, PermAction.Write);
      if (authError != null) return authError;

      user.LabResults.Add(request);
      
      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.6 info
    [HttpPost("usrup/{uuid}/info")]
    public async Task<ActionResult> Updateinfo(Guid uuid, [FromBody] InfoUpdateRequest request)
    {
      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      var authError = AuthorizePatientAccess(user, PermCategory.PersonInfo, PermAction.Write);
      if (authError != null) return authError;

      if (request.bday != 0)
      {
        int day = request.bday / 10000;
        int month = (request.bday / 100) % 100;
        int year = 2000 + (request.bday % 100);
        user.Birthdate = new DateOnly(year, month, day);
      }

      if (request.cpr_key != 0)
      {
        user.CprKey = request.cpr_key;
      }

      if (request.pronouns != null)
      {
        user.Pronouns = request.pronouns;
      }

      if (request.name != null)
      {
        user.Name = request.name;
      }

      if (request.bio_sex != user.BioGender)
      {
        user.BioGender = request.bio_sex;
      }

      if (request.pfp != null)
      {
        user.Pfp = request.pfp;
      }
      
      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.3 Patient Overview
    [HttpGet("pf/{doctor_uuid}")]

    public async Task<ActionResult> PatientOverview(Guid doctor_uuid)
    {
        // The caller's own identity is authoritative, not the URL parameter,
        // so a doctor cannot list another doctor's patients by guessing their uuid.
        var callerUuid = GetUserUuid();
        if (callerUuid == null)
          return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

        var callerKey = callerUuid.Value.ToString();

        // Patients you're the assigned doctor for.
        List<PatientOverview> patientOverview = await _context.Pr
          .Where(c => c.Doctor == callerUuid)
          .Select(c => new PatientOverview {
              uuid = c.Uuid,
              name = c.Name,
              cpr = Parser.convertToCpr(c.Birthdate, c.CprKey),
              pronouns = c.Pronouns,
              birthdate = c.Birthdate,
              pfp = c.Pfp
              })
              .ToListAsync();

        // Patients another doctor has shared with you. DrPerms is a JSON
        // column, so it's filtered in memory rather than translated to SQL.
        var sharedCandidates = await _context.Pr
          .Where(c => c.Doctor != callerUuid)
          .ToListAsync();

        patientOverview.AddRange(sharedCandidates
          .Where(c => c.DrPerms != null
                      && c.DrPerms.TryGetValue(callerKey, out var permInt)
                      && perms.HasAnyPermission(permInt))
          .Select(c => new PatientOverview {
              uuid = c.Uuid,
              name = c.Name,
              cpr = Parser.convertToCpr(c.Birthdate, c.CprKey),
              pronouns = c.Pronouns,
              birthdate = c.Birthdate,
              pfp = c.Pfp
          }));

          return Ok(new
          {
            code = ErrorCodes.Success,
            Data = patientOverview
          });
    }
    
    // 3.4.1
    [HttpGet("calendar/sync/{uuid}")]

    public async Task<ActionResult> CalendarFetching(Guid uuid)
    {
      // The caller's own identity is authoritative, not the URL parameter,
      // so a doctor cannot sync another doctor's calendar by guessing their uuid.
      var callerUuid = GetUserUuid();
      if (callerUuid == null)
        return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

      // flatten JSON arrays
      List<CalenderData> data = new List<CalenderData>();

      // fetch all appointments JSON arrays
      var dataArr = await _context.Pr
        .Where(c => c.Doctor == callerUuid)
        .Select(c => c.Appointments)
        .ToListAsync();
      Console.WriteLine(dataArr);
      foreach (var items in dataArr)
      {
        foreach (var item in items)
        {
          if (item != null)
          {

            data.Add(item);
          }
        }
      }

      return Ok(new
      {
        code = ErrorCodes.Success,
        calendar = data
      });
    }
    //3.6.1
    [HttpGet("{uuid}/timeline/get")]
    public async Task<ActionResult> TimeLineFetching(Guid uuid)
    {
      // The caller's own identity is authoritative, not the URL parameter,
      // so a doctor cannot read another doctor's timeline by guessing their uuid.
      var callerUuid = GetUserUuid();
      if (callerUuid == null)
        return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

      var User = _context.Cur.Find(callerUuid.Value);
      if (User == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      return Ok(new
      {
        code = ErrorCodes.Success,
        timeline = User.Timeline
      });
    }  
    
    // 3.6.2
    [HttpPost("{uuid}/timeline/update")]
    public async Task<IActionResult> UpdateTimeline(Guid uuid, [FromBody] TimeLine request)
    {
      // The caller's own identity is authoritative, not the URL parameter,
      // so a doctor cannot write to another doctor's timeline by guessing their uuid.
      var callerUuid = GetUserUuid();
      if (callerUuid == null)
        return Unauthorized(new { code = ErrorCodes.Security.Unauthorised, message = "Unauthorized." });

      var user = await _context.Cur.FindAsync(callerUuid.Value);

      if (user == null)
        return NotFound();

      user.Timeline.Add(request);


      // IMPORTANT: force EF to detect change
      _context.Entry(user).Property(p => p.Timeline).IsModified = true;

      await _context.SaveChangesAsync();

      return Ok(new
      {
        code = ErrorCodes.Success,
        message = "Permissions updated",
      });
    }
  } 
}

