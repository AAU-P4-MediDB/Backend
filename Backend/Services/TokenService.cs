using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;

namespace Backend.Services;

public class TokenService
{
  private readonly IConfiguration _config;

  public TokenService(IConfiguration config)
  {
    _config = config;
  }

  public string GenerateToken(CUR user)
  {
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
      new Claim(JwtRegisteredClaimNames.Sub, user.Uuid.ToString()),
      new Claim(JwtRegisteredClaimNames.Email, user.Email),
      new Claim("position", user.Position.ToString()),
      new Claim("clinic", user.Clinic.ToString() ?? ""),
      new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    var token = new JwtSecurityToken(
      issuer:             _config["Jwt:Issuer"],
      audience:           _config["Jwt:Audience"],
      claims:             claims,
      expires:            DateTime.UtcNow.AddMinutes(
        double.Parse(_config["Jwt:ExpiryMinutes"]!)),
      signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}