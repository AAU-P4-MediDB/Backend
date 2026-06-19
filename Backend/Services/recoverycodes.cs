using System.Security.Cryptography;
using System.Text;
using Backend.Models;

namespace Backend.Services;

public class RecoveryCodeService
{
  private readonly DBcontext _context;

  public RecoveryCodeService(DBcontext db)
  {
    _context = db;
  }


  public async Task<List<string>> Generate(
    Guid userUuid,
    int amount = 10)
  {
    var codes = new List<string>();

    for (int i = 0; i < amount; i++)
    {
      var code = CreateCode();

      codes.Add(code);

      _context.UserRecoveryCodes.Add(new UserRecoveryCode
      {
        Uuid = Guid.NewGuid(),

        UserUuid = userUuid,

        // store hash only
        CodeHash = hashing.HashSHA3_512(code),

        Used = false
      });
    }

    await _context.SaveChangesAsync();

    return codes;
  }


  private string CreateCode()
  {
    var bytes = RandomNumberGenerator.GetBytes(10);

    return Convert.ToHexString(bytes);
  }
}