using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;


[ApiController]
[Route("api/um/passkey")]
public class PasskeyController : ControllerBase
{

    private readonly PasskeyService _passkey;
    private readonly TokenService _token;



    public PasskeyController(
        PasskeyService passkey,
        TokenService token)
    {
        _passkey = passkey;
        _token = token;
    }
    
    // CREATE PASSKEY
    [HttpPost("register/options")]
    public async Task<IActionResult> RegisterOptions()
    {
        var userId =
            Guid.Parse(
                User.FindFirst("sub")!.Value);
        var options =
            _passkey.RegisterOptions(userId);
        
        HttpContext.Session
            .SetString(
                "passkey_register",
                options.ToJson());
        
        return Ok(options);
    }


    

    [HttpPost("register/verify")]
    public async Task<IActionResult> RegisterVerify(PasskeyRegisterVerifyRequest req)
    {
        var userId =
            Guid.Parse(
                User.FindFirst("sub")!.Value);


        var json =
            HttpContext.Session
            .GetString("passkey_register");


        var options =
            CredentialCreateOptions
            .FromJson(json!);



        var ok =
            await _passkey.RegisterVerify(
                userId,
                req.Response,
                options);



        return ok
            ? Ok()
            : BadRequest();
    }





    // LOGIN

    [HttpPost("options")]
    public async Task<IActionResult> LoginOptions(
        PasskeyLoginOptionsRequest req)
    {

        var options =
            await _passkey.LoginOptions(
                req.Email);


        HttpContext.Session
            .SetString(
                "passkey_login",
                options.ToJson());


        return Ok(options);
    }






    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        PasskeyLoginVerifyRequest req)
    {


        var json =
            HttpContext.Session
            .GetString("passkey_login");


        var options =
            AssertionOptions
            .FromJson(json!);



        var ok =
            await _passkey.LoginVerify(
                req.Email,
                req.Response,
                options);
        
        if(!ok)
            return Unauthorized();
        // issue JWT HERE
        return Ok();
    }
}