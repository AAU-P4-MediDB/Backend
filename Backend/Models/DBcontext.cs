using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.Models
{
  public class DBcontext : Dbcontext
  {
    public DBcontext(DbContextOptions<DBcontext> options) : base(options)
    {
    }

    public DbSet<CCR> Ccr { get; set; } = null!;
    public DbSet<CUR> Cur { get; set; } = null!;
    public DbSet<PR> Pr { get; set; } = null!;
    
  }
}