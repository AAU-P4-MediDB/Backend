using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;


namespace Backend.Controllers
{
  
  [Authorize(Policy = "DoctorOnly")]
  [ApiController]
  [Route("api/dpm")]
  public class DoctorPatientInterfaceController : ControllerBase
  {
    private readonly DBcontext _context;
    private readonly string _aesKey;

    public DoctorPatientInterfaceController(DBcontext context, IConfiguration config)
    {
      _context = context;
      _aesKey = config["AES_KEY"] 
                ?? throw new InvalidOperationException("AES key not configured");
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

      return Ok(new
      {
        code = ErrorCodes.Success,
        uuid = user.Uuid,
        name = AesEncryption.Decrypt(user.Name, _aesKey),
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

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.3
    [HttpPost("usrup/{uuid}/prescription")]
    public async Task<ActionResult> Updateprescription(Guid uuid, [FromBody] presrciptions request)
    {

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

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

      List<diagnosis> diagnosisList;

      if (user.Diagnosis == null || !user.Diagnosis.Any())
      {
        diagnosisList = new List<diagnosis>();
      }
      else
      {
        diagnosisList = user.Diagnosis;
      }



      // Add new unknown JSON object
      diagnosisList.AddRange(request);

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
        
        List<PatientOverview> patientOverview = await _context.Pr
          .Where(c => c.Doctor == doctor_uuid)
          .Select(c => new PatientOverview {
              name = c.Name,
              cpr = Parser.convertToCpr(c.Birthdate, c.CprKey),
              pronouns = c.Pronouns,
              birthdate = c.Birthdate,
              pfp = c.Pfp
              })
              .ToListAsync();

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
      // flatten JSON arrays
      List<CalenderData> data = new List<CalenderData>();
      
      // fetch all appointments JSON arrays
      var dataArr = await _context.Pr
        .Where(c => c.Doctor == uuid)
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
      var User = _context.Cur.Find(uuid);

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
      var user = await _context.Cur.FindAsync(uuid);

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

