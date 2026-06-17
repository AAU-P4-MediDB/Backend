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

    if (!hashing.VerifyPassword(req.password,user.Salt, user.Password))
      return null;

    if (!await _mfa.IsEnabled(user.Uuid))
    {
      return IssueTokens(user);
    }

    var session = await _mfa.CreateSession(user.Uuid);

    return new
    {
      code = ErrorCodes.Success,
      mfaRequired = true,
      mfaSession = session
    };
  }

  public async Task<object?> VerifyMfa(MfaVerifyRequest req)
  {
    var userId = await _mfa.ValidateSession(req.MfaSession);

    if (userId == null)
      return null;

    var user = await _context.Cur.FirstAsync(x => x.Uuid == userId);

    bool ok = false;

    if (!string.IsNullOrEmpty(req.TotpCode))
      ok = await _mfa.VerifyTotp(userId.Value, req.TotpCode);

    if (!ok && !string.IsNullOrEmpty(req.RecoveryCode))
      ok = await _mfa.VerifyRecovery(userId.Value, req.RecoveryCode);

    if (!ok)
      return null;

    return IssueTokens(user);
  }

  private object IssueTokens(CUR user)
  {
    return new
    {
      code = ErrorCodes.Success,
      accessToken = _token.GenerateToken(user),
      refreshToken = _token.GenerateRefreshToken()
    };
  }
}