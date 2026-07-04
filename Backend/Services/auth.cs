using Backend.Models;
using Microsoft.EntityFrameworkCore;


namespace Backend.Services;

public class AuthService
{
  private readonly DBcontext _context;
  private readonly MfaService _mfa;
  private readonly TokenService _token;

  public AuthService(DBcontext db, MfaService mfa, TokenService token)
  {
    _context = db;
    _mfa = mfa;
    _token = token;
  }

  public async Task<object?> Login(LoginRequest req)
  {
    var user = await _context.Cur
      .FirstOrDefaultAsync(x => x.Email == req.email);

    if (user == null)
      return null;

    if (!hashing.VerifyPassword(req.password, user.Salt, user.Password))
      return null;

    if (!await _mfa.IsEnabled(user.Uuid))
      return IssueTokens(user);

    var sessionToken = await _mfa.CreateSession(user.Uuid);
    var methods = await _mfa.GetMethods(user.Uuid);

    return new MfaChallenge
    {
      MfaToken = sessionToken,
      Methods = methods
    };
  }

  public async Task<object?> VerifyMfa(MfaVerifyRequest req)
  {
    var session = await _mfa.GetSession(req.MfaToken);
    if (session == null) return null;

    var user = await _context.Cur.FirstAsync(x => x.Uuid == session.UserUuid);

    var codeType = _mfa.DetectCodeType(req.Code);
    bool ok = codeType switch
    {
      CodeType.Totp    => await _mfa.VerifyTotp(session.UserUuid, req.Code),
      CodeType.Yubikey => await _mfa.VerifyYubikey(session.UserUuid, req.Code),
      _                => await _mfa.VerifyRecovery(session.UserUuid, req.Code),
    };

    if (!ok) return null;

    // Recovery codes bypass all factor requirements immediately
    if (codeType == CodeType.Recovery)
    {
      await _mfa.ConsumeSession(req.MfaToken);
      return IssueTokens(user);
    }

    var methodName = codeType == CodeType.Totp ? "totp" : "yubikey";

    var verified = (session.VerifiedMethods ?? "")
      .Split(',', StringSplitOptions.RemoveEmptyEntries)
      .ToHashSet();
    verified.Add(methodName);

    var mfaRecord = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserUuid == session.UserUuid);
    var required = new HashSet<string>();
    if (mfaRecord?.TotpEnabled == true)    required.Add("totp");
    if (mfaRecord?.YubikeyEnabled == true) required.Add("yubikey");

    if (required.IsSubsetOf(verified))
    {
      await _mfa.ConsumeSession(req.MfaToken);
      return IssueTokens(user);
    }

    await _mfa.UpdateVerifiedMethods(req.MfaToken, string.Join(',', verified));

    return new MfaPartialChallenge
    {
      MfaToken = req.MfaToken,
      RemainingMethods = required.Except(verified).ToList()
    };
  }

  private object IssueTokens(CUR user)
  {
    return new
    {
      code = ErrorCodes.Success,
      accessToken = _token.GenerateToken(user),
      refreshToken = _token.GenerateRefreshToken(user)
    };
  }
}
