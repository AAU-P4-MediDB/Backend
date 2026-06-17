public class LoginRequest
{
  public string email { get; set; } = "";
  public string password { get; set; } = "";
}

public class MfaVerifyRequest
{
  public string MfaSession { get; set; } = "";
  public string? TotpCode { get; set; }
  public string? RecoveryCode { get; set; }
}