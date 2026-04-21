namespace Backend.Services;
using System.Security.Cryptography;
using System.Text;


public static class hashing
{
  
  public static string HashSHA3_512(string input)
  {
    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
    byte[] hashBytes = SHA3_512.HashData(inputBytes);
    return Convert.ToHexString(hashBytes).ToLower();
  }
  
  public static string GenerateSalt()
  {
    byte[] saltBytes = RandomNumberGenerator.GetBytes(32);
    return Convert.ToHexString(saltBytes).ToLower();
  }

  public static string HashPassword(string password, string salt)
  {
    return HashSHA3_512(password + salt);
  }

  public static bool VerifyPassword(string inputPassword, string salt, string storedHash)
  {
    string inputHash = HashPassword(inputPassword, salt);
    return CryptographicOperations.FixedTimeEquals(
      Convert.FromHexString(inputHash),
      Convert.FromHexString(storedHash)
    );
  }
  
}