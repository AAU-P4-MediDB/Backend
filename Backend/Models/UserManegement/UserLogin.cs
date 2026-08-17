public class LoginRequest
{
  public string email { get; set; } = "";
  public string password { get; set; } = "";
}

public class MfaVerifyRequest
{
  public string MfaToken { get; set; } = "";
  public string Code { get; set; } = "";
}

public sealed class MfaChallenge
{
  public string MfaToken { get; init; } = "";
  public List<string> Methods { get; init; } = new();
}

public sealed class TokenResult
{
  public string Code { get; init; } = "";
  public string AccessToken { get; init; } = "";
  public string RefreshToken { get; init; } = "";
}

public class YubikeyRegistrationRequest
{
  public string Otp { get; set; } = "";
  public string? Label { get; set; }
}

public sealed class MfaPartialChallenge
{
  public string MfaToken { get; init; } = "";
  public List<string> RemainingMethods { get; init; } = new();
}

public class TotpConfirmRequest
{
  public string Secret { get; set; } = "";
  public string Code { get; set; } = "";
}