using Backend.Services;

namespace Backend.Models;

public static class startup
{
  public static async void hashPasswords(DBcontext context)
  {
    CUR[] users = context.Cur.Where(c => c.Salt != "").ToArray();
    foreach (CUR user in users)
    {
      string salt = hashing.GenerateSalt();
      user.Salt = salt;
      user.Password = hashing.HashPassword(user.Password, salt);
      context.Entry(user).Property(p => p.Salt).IsModified = true;
      context.Entry(user).Property(p => p.Password).IsModified = true;
    }
    await context.SaveChangesAsync();
  }
  
}