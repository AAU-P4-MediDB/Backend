using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.Models
{
  public class DBcontext : DbContext
  {
    public DBcontext(DbContextOptions<DBcontext> options) : base(options){} //why is this empty?

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasPostgresEnum<PositionType>("position_type");
      modelBuilder.Entity<PR>()
        .Property(p => p.DrPerms)
        .HasColumnType("json");
    }

    public DbSet<CCR> Ccr { get; set; } = null!;
    public DbSet<CUR> Cur { get; set; } = null!;
    public DbSet<PR> Pr { get; set; } = null!;
    
  }
}