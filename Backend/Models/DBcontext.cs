using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Backend.Services;
using Microsoft.Extensions.Configuration;

namespace Backend.Models
{
  public class DBcontext : DbContext
  {
    
    private readonly string _aesKey;
    
    public DBcontext(DbContextOptions<DBcontext> options, IConfiguration configuration) : base(options)
    {
      _aesKey = configuration["AES_KEY"]
                ?? throw new InvalidOperationException("AES key not configured");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasPostgresEnum<PositionType>("position_type");
      modelBuilder.Entity<PR>()
        .Property(p => p.DrPerms)
        .HasColumnType("json");
      modelBuilder.Entity<PR>()
        .Property(p => p.Appointments)
        .HasColumnType("json");
      modelBuilder.Entity<PR>()
        .Property(p => p.DrPermRequests)
        .HasColumnType("json");
      modelBuilder.Entity<PR>()
        .Property(p => p.LabResults)
        .HasColumnType("json");
      modelBuilder.Entity<PR>()
        .Property(p => p.Prescriptions)
        .HasColumnType("json");
      modelBuilder.Entity<PR>()
        .Property(p => p.Diagnosis)
        .HasColumnType("json");
      modelBuilder.Entity<CUR>()
        .Property(p => p.Timeline)
        .HasColumnType("json");

      
      var birthdateConverter = new ValueConverter<DateOnly, string>(
        v => AesEncryption.Encrypt(v.ToString("yyyy-MM-dd"), _aesKey),
        v => DateOnly.Parse(AesEncryption.Decrypt(v, _aesKey))
      );
      var intEncryptConverter = new ValueConverter<int, string>(
        v => AesEncryption.Encrypt(v.ToString(), _aesKey),
        v => int.Parse(AesEncryption.Decrypt(v, _aesKey))
      );
      
      var EncryptConverter = new ValueConverter<string, string>(
        v => AesEncryption.Encrypt(v, _aesKey),
        v => AesEncryption.Decrypt(v, _aesKey)
      );
      
      modelBuilder.Entity<PR>()
        .Property(p => p.CprKey)
        .HasConversion(intEncryptConverter);
      
      modelBuilder.Entity<PR>()
        .Property(p => p.Birthdate)
        .HasConversion(birthdateConverter);
      
      modelBuilder.Entity<PR>()
        .Property(p => p.Name)
        .HasConversion(EncryptConverter);
      
      modelBuilder.Entity<UserTotp>()
        .Property(p => p.Secret)
        .HasConversion(EncryptConverter);
      
    }

    public DbSet<CCR> Ccr { get; set; } = null!;
    public DbSet<CUR> Cur { get; set; } = null!;
    public DbSet<PR> Pr { get; set; } = null!;
    
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public DbSet<UserMfa> UserMfa { get; set; }
    public DbSet<UserTotp> UserTotp { get; set; }
    public DbSet<UserRecoveryCode> UserRecoveryCodes { get; set; }
    public DbSet<MfaSession> MfaSessions { get; set; }
    public DbSet<Passkey> UserPasskeys { get; set; }
    
  }
}