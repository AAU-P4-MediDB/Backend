using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;


namespace Backend.Controllers
{
  [ApiController]
  [Route("api/dpm")]
  public class DoctorPatientInterfaceController : ControllerBase
  {
    private readonly DBcontext _context;

    public DoctorPatientInterfaceController(DBcontext context)
    {
      _context = context;
    }
    
    //3.1.1
    [HttpPost("usrfet/vitals")]
    public async Task<ActionResult> VitalsFetching([FromBody] ptdataFetchingRequest request)
    {
      if (string.IsNullOrWhiteSpace(request.CPR_pt)) 
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });
      
      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      int bday = cpr[0];
      int day = bday / 10000; 
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);
      
      
      var user = _context.Pr.First(c => c.CprKey == cpr[1] &&  c.Birthdate == dateOnly);

      
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

      int bday = cpr[0];
      int day = bday / 10000; 
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);
      
      
      var user = _context.Pr.First(c => c.CprKey == cpr[1] &&  c.Birthdate == dateOnly);

      
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

      int bday = cpr[0];
      int day = bday / 10000;
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);


      var user = _context.Pr.First(c => c.CprKey == cpr[1] && c.Birthdate == dateOnly);


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

      int bday = cpr[0];
      int day = bday / 10000;
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);


      var user = _context.Pr.First(c => c.CprKey == cpr[1] && c.Birthdate == dateOnly);


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

      int bday = cpr[0];
      int day = bday / 10000;
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);


      var user = _context.Pr.First(c => c.CprKey == cpr[1] && c.Birthdate == dateOnly);


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
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField, message = "Missing required field." });

      int[] cpr = request.CPR_pt.Split('-').Select(int.Parse).ToArray();

      int bday = cpr[0];
      int day = bday / 10000;
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);


      var user = _context.Pr.First(c => c.CprKey == cpr[1] && c.Birthdate == dateOnly);


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

      int bday = cpr[0];
      int day = bday / 10000;
      int month = (bday / 100) % 100;
      int year = 2000 + (bday % 100);
      DateOnly dateOnly = new DateOnly(year, month, day);


      var user = _context.Pr.First(c => c.CprKey == cpr[1] && c.Birthdate == dateOnly);


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
    public async Task<ActionResult> Updateprescription(Guid uuid, [FromBody] PrescriptionUpdateRequest request)
    {
      if (request.prescriptions.ValueKind == JsonValueKind.Undefined)
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      List<JsonElement> prescriptionList;

      if (string.IsNullOrEmpty(user.Journal))
      {
        prescriptionList = new List<JsonElement>();
      }
      else
      {
        prescriptionList = JsonSerializer.Deserialize<List<JsonElement>>(user.Prescriptions);
      }

      // Add new unknown JSON object
      prescriptionList.Add(request.prescriptions);

      // Save back
      user.Prescriptions = JsonSerializer.Serialize(prescriptionList);

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.4 diagnosis
    [HttpPost("usrup/{uuid}/diagnosis")]
    public async Task<ActionResult> Updatediagnosis(Guid uuid, [FromBody] DiagnosesUpdateRequest request)
    {
      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      List<string> diagnosisList;

      if (user.Diagnosis == null || !user.Diagnosis.Any())
      {
        diagnosisList = new List<string>();
      }
      else
      {
        diagnosisList = user.Diagnosis;
      }

      // Add new unknown JSON object
      diagnosisList.Add(request.diagnoses);

      // Save back
      user.Diagnosis = diagnosisList;

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    
    //3.2.5 appointment
    [HttpPost("usrup/{uuid}/appointment")]
    public async Task<ActionResult> Updateappointment(Guid uuid, [FromBody] AppointmentUpdateRequest request)
    {
      if (request.appointment.ValueKind == JsonValueKind.Undefined)
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      List<JsonElement> List;

      if (string.IsNullOrEmpty(user.Appointments))
      {
        List = new List<JsonElement>();
      }
      else
      {
        List = JsonSerializer.Deserialize<List<JsonElement>>(user.Appointments);
      }

      // Add new unknown JSON object
      List.Add(request.appointment);

      // Save back
      user.Appointments = JsonSerializer.Serialize(List);

      await _context.SaveChangesAsync();

      return Ok(new { code = ErrorCodes.Success });
    }
    //3.2.7 labresult
    [HttpPost("usrup/{uuid}/labresult")]
    public async Task<ActionResult> Updatelabresult(Guid uuid, [FromBody] LabResultUpdateRequest request)
    {
      if (request.lab_result.ValueKind == JsonValueKind.Undefined)
        return BadRequest(new { code = ErrorCodes.User.MissingRequiredField });

      var user = await _context.Pr.FirstOrDefaultAsync(u => u.Uuid == uuid);

      if (user == null)
        return NotFound(new { code = ErrorCodes.User.UserNotFound });

      List<JsonElement> List;

      if (string.IsNullOrEmpty(user.LabResults))
      {
        List = new List<JsonElement>();
      }
      else
      {
        List = JsonSerializer.Deserialize<List<JsonElement>>(user.LabResults);
      }

      // Add new unknown JSON object
      List.Add(request.lab_result);

      // Save back
      user.LabResults = JsonSerializer.Serialize(List);

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
  } 
}

