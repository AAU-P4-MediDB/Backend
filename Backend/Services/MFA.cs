using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;


namespace Backend.Services;

public class MfaService
{
  private readonly DBcontext _context;
  private readonly IHttpClientFactory _httpFactory;
  private readonly IConfiguration _config;

  private static readonly Regex YubikeyOtpPattern = new(@"^[cbdefghijklnrtuv]{44}$", RegexOptions.Compiled);
  private static readonly Regex TotpPattern = new(@"^\d{6,8}$", RegexOptions.Compiled);

  public MfaService(DBcontext db, IHttpClientFactory httpFactory, IConfiguration config)
  {
    _context = db;
    _httpFactory = httpFactory;
    _config = config;
  }

  public async Task<bool> IsEnabled(Guid userUuid)
  {
    var mfa = await _context.UserMfa
      .FirstOrDefaultAsync(x => x.UserUuid == userUuid);

    return mfa != null &&
           (mfa.TotpEnabled || mfa.PasskeyEnabled || mfa.YubikeyEnabled);
  }

  public async Task<List<string>> GetMethods(Guid userUuid)
  {
    var mfa = await _context.UserMfa
      .FirstOrDefaultAsync(x => x.UserUuid == userUuid);

    if (mfa == null) return new List<string>();

    var methods = new List<string>();
    if (mfa.TotpEnabled) methods.Add("totp");
    if (mfa.YubikeyEnabled) methods.Add("yubikey");
    return methods;
  }

  public async Task<string> CreateSession(Guid userUuid)
  {
    var token = Convert.ToBase64String(
      RandomNumberGenerator.GetBytes(48));

    _context.MfaSessions.Add(new MfaSession
    {
      UserUuid = userUuid,
      SessionToken = token,
      Expires = DateTime.UtcNow.AddMinutes(5),
      Used = false
    });

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

  public async Task ConsumeSession(string token)
  {
    var session = await _context.MfaSessions
      .FirstOrDefaultAsync(x => x.SessionToken == token);

    if (session != null)
    {
      session.Used = true;
      await _context.SaveChangesAsync();
    }
  }

  public async Task<bool> VerifyTotp(Guid userUuid, string code)
  {
    var record = await _context.UserTotp
      .FirstOrDefaultAsync(x => x.UserUuid == userUuid);

    if (record == null) return false;

    var secretBytes = Base32Encoding.ToBytes(record.Secret);
    var totp = new Totp(secretBytes);

    return totp.VerifyTotp(code, out _, new VerificationWindow(2, 2));
  }

  public async Task<bool> VerifyYubikey(Guid userUuid, string otp)
  {
    if (!YubikeyOtpPattern.IsMatch(otp)) return false;

    var publicId = otp[..12];

    var registered = await _context.UserYubikeys
      .AnyAsync(x => x.UserUuid == userUuid && x.PublicId == publicId);

    if (!registered) return false;

    return await CallYubicoApi(otp);
  }

  public async Task<List<UserYubikey>> GetYubikeys(Guid userUuid)
  {
    return await _context.UserYubikeys
      .Where(x => x.UserUuid == userUuid)
      .ToListAsync();
  }

  public async Task<(bool success, string? error)> RegisterYubikey(Guid userUuid, string otp, string? label)
  {
    if (!YubikeyOtpPattern.IsMatch(otp))
      return (false, "Invalid OTP format.");

    var publicId = otp[..12];

    if (await _context.UserYubikeys.AnyAsync(x => x.UserUuid == userUuid && x.PublicId == publicId))
      return (false, "This YubiKey is already registered.");

    if (!await CallYubicoApi(otp))
      return (false, "YubiKey validation failed.");

    _context.UserYubikeys.Add(new UserYubikey
    {
      UserUuid = userUuid,
      PublicId = publicId,
      Label = label
    });

    var mfa = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserUuid == userUuid);
    if (mfa == null)
    {
      _context.UserMfa.Add(new UserMfa { UserUuid = userUuid, YubikeyEnabled = true });
    }
    else
    {
      mfa.YubikeyEnabled = true;
    }

    await _context.SaveChangesAsync();
    return (true, null);
  }

  public async Task<bool> RemoveYubikey(Guid userUuid, Guid keyUuid)
  {
    var key = await _context.UserYubikeys
      .FirstOrDefaultAsync(x => x.Uuid == keyUuid && x.UserUuid == userUuid);

    if (key == null) return false;

    _context.UserYubikeys.Remove(key);
    await _context.SaveChangesAsync();

    var hasMore = await _context.UserYubikeys.AnyAsync(x => x.UserUuid == userUuid);
    if (!hasMore)
    {
      var mfa = await _context.UserMfa.FirstOrDefaultAsync(x => x.UserUuid == userUuid);
      if (mfa != null)
      {
        mfa.YubikeyEnabled = false;
        await _context.SaveChangesAsync();
      }
    }

    return true;
  }

  public async Task<bool> VerifyRecovery(Guid userUuid, string code)
  {
    var codeHash = hashing.HashSHA3_512(code);
    var record = await _context.UserRecoveryCodes
      .FirstOrDefaultAsync(x =>
        x.UserUuid == userUuid &&
        x.CodeHash == codeHash &&
        !x.Used);

    if (record == null) return false;

    record.Used = true;
    await _context.SaveChangesAsync();
    return true;
  }

  public CodeType DetectCodeType(string code)
  {
    if (TotpPattern.IsMatch(code)) return CodeType.Totp;
    if (YubikeyOtpPattern.IsMatch(code)) return CodeType.Yubikey;
    return CodeType.Recovery;
  }

  private async Task<bool> CallYubicoApi(string otp)
  {
    var clientId = _config["Yubico:ClientId"];
    var apiKey = _config["Yubico:ApiKey"];

    if (string.IsNullOrEmpty(clientId)) return false;

    var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower();
    var queryParams = $"id={clientId}&nonce={nonce}&otp={otp}&sl=secure&timestamp=1";

    string url;
    if (!string.IsNullOrEmpty(apiKey))
    {
      var keyBytes = Convert.FromBase64String(apiKey);
      using var hmac = new HMACSHA1(keyBytes);
      var sig = Convert.ToBase64String(
        hmac.ComputeHash(System.Text.Encoding.ASCII.GetBytes(queryParams)));
      url = $"https://api.yubico.com/wsapi/2.0/verify?{queryParams}&h={Uri.EscapeDataString(sig)}";
    }
    else
    {
      url = $"https://api.yubico.com/wsapi/2.0/verify?{queryParams}";
    }

    try
    {
      var client = _httpFactory.CreateClient("yubico");
      var response = await client.GetStringAsync(url);
      var status = response
        .Split('\n')
        .FirstOrDefault(l => l.StartsWith("status="))
        ?.Split('=')[1]
        ?.Trim();
      return status == "OK";
    }
    catch
    {
      return false;
    }
  }
}

public enum CodeType { Totp, Yubikey, Recovery }
