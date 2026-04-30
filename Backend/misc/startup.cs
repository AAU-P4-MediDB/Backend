using Backend.Services;
using Backend.Models;

namespace Backend.Models;

public class Startup
{
  private readonly DBcontext _context;
  private readonly string _aesKey;

  public Startup(DBcontext context, string aesKey)
  {
    _context = context;
    _aesKey = aesKey;
  }

  // Static factory so you can await it properly
  public static async Task RunAsync(DBcontext context, string aesKey)
  {
    var startup = new Startup(context, aesKey);
    await startup.HashPasswordsAsync();
    await startup.Encript_patient_data_Async();
  }

  public async Task HashPasswordsAsync()
  {
    // Salt == "" means not yet hashed — fix the condition
    CUR[] users = _context.Cur
      .Where(c => string.IsNullOrWhiteSpace(c.Salt) || !c.Salt.StartsWith("$2"))  // was != which would re-hash already hashed passwords
      .ToArray();

    foreach (CUR user in users)
    {
      string salt = hashing.GenerateSalt();
      user.Salt     = salt;
      user.Password = hashing.HashPassword(user.Password, salt);

      _context.Entry(user).Property(p => p.Salt).IsModified     = true;
      _context.Entry(user).Property(p => p.Password).IsModified = true;
    }

    await _context.SaveChangesAsync();
  }

  public async Task Encript_patient_data_Async()
  {
    PR[] users = _context.Pr.ToArray();

    foreach (PR pr in users)
    {
      pr.Name = AesEncryption.Encrypt(pr.Name,_aesKey);
      pr.Diagnosis = AesEncryption.EncryptList(pr.Diagnosis,_aesKey);
      
      _context.Entry(pr).Property(p => p.Name).IsModified     = true;
      _context.Entry(pr).Property(p => p.Diagnosis).IsModified = true;
    }
    
    await _context.SaveChangesAsync();
  }
}