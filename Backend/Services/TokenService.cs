using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Backend.Models;

namespace Backend.Services;

public class TokenService
{
  private readonly IConfiguration _config;
  private readonly DBcontext _context;

  public TokenService(IConfiguration config, DBcontext context)
  {
    _config = config;
    _context = context;
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
      new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
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
  
  public string GenerateRefreshToken(CUR user)
  {
    var bytes = RandomNumberGenerator.GetBytes(64);
    
    var hash = HashRefreshToken(Convert.ToBase64String(bytes));
    _context.RefreshTokens.Add(new RefreshToken
    {
      UserUuid = user.Uuid,
      TokenHash = hash,
      Expires = DateTime.UtcNow.AddDays(7)
    });
    return HashRefreshToken(Convert.ToBase64String(bytes));
  }

  public string HashRefreshToken(string token)
  {
    return hashing.HashSHA3_512(token);
  }
}