using Backend.Services;

namespace Backend.Models;

public  class startup
{

  startup(DBcontext context, string aeskey)
  {
    hashPasswords(context, aeskey);
  }
  public async void hashPasswords(DBcontext context, string aeskey)
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