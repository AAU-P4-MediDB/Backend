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
    var userId = await _mfa.ValidateSession(req.MfaToken);

    if (userId == null)
      return null;

    var user = await _context.Cur.FirstAsync(x => x.Uuid == userId);

    var codeType = _mfa.DetectCodeType(req.Code);
    bool ok = codeType switch
    {
      CodeType.Totp    => await _mfa.VerifyTotp(userId.Value, req.Code),
      CodeType.Yubikey => await _mfa.VerifyYubikey(userId.Value, req.Code),
      _                => await _mfa.VerifyRecovery(userId.Value, req.Code),
    };

    if (!ok)
      return null;

    await _mfa.ConsumeSession(req.MfaToken);

    return IssueTokens(user);
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
