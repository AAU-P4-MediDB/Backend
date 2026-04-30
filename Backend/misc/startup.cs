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

  }

  public async Task HashPasswordsAsync()
  {
    CUR[] users = _context.Cur
      .Where(c =>
        string.IsNullOrWhiteSpace(c.Salt) ||
        !c.Salt.Trim().StartsWith("$2")
      )
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

}