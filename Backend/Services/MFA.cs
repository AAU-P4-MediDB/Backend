using System.Security.Cryptography;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;


namespace Backend.Services;
public class MfaService
{
  private readonly DBcontext _context;

  public MfaService(DBcontext db)
  {
    _context = db;
  }

  public async Task<bool> IsEnabled(Guid userUuid)
  {
    var mfa = await _context.UserMfa
      .FirstOrDefaultAsync(x => x.UserUuid == userUuid);

    return mfa != null &&
           (mfa.TotpEnabled || mfa.PasskeyEnabled);
  }

  public async Task<string> CreateSession(Guid userUuid)
  {
    var token = Convert.ToBase64String(
      RandomNumberGenerator.GetBytes(48));

    var session = new MfaSession
    {
      UserUuid = userUuid,
      SessionToken = token,
      Expires = DateTime.UtcNow.AddMinutes(5),
      Used = false
    };

    _context.MfaSessions.Add(session);
    await _context.SaveChangesAsync();

    return token;
  }

  public async Task<Guid?> ValidateSession(string token)
  {
    var session = await _context.MfaSessions
      .FirstOrDefaultAsync(x =>
        x.SessionToken == token &&
        !x.Used &&
        x.Expires > DateTime.UtcNow);

    return session?.UserUuid;
  }

  public async Task<bool> VerifyTotp(Guid userUuid, string code)
  {
    var record = await _context.UserTotp
      .FirstOrDefaultAsync(x => x.UserUuid == userUuid);

    if (record == null)
      return false;

    var secretBytes = Base32Encoding.ToBytes(record.Secret);
    var totp = new Totp(secretBytes);

    return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
  }

  public async Task<bool> VerifyRecovery(Guid userUuid, string codeHash)
  {
    var record = await _context.UserRecoveryCodes
      .FirstOrDefaultAsync(x =>
        x.UserUuid == userUuid &&
        x.CodeHash == codeHash &&
        !x.Used);

    if (record == null)
      return false;

    record.Used = true;
    await _context.SaveChangesAsync();

    return true;
  }
}