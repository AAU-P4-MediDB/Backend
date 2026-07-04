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