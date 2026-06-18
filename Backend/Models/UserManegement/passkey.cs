using Fido2NetLib;

namespace Backend.Models;



public class PasskeyRegisterVerifyRequest
{
  public AuthenticatorAttestationRawResponse Response { get; set; } = null!;
}


public class PasskeyLoginOptionsRequest
{
  public string Email { get; set; } = "";
}


public class PasskeyLoginVerifyRequest
{
  public string Email { get; set; } = "";

  public AuthenticatorAssertionRawResponse Response { get; set; } = null!;
}