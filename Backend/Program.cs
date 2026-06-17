using Microsoft.EntityFrameworkCore;
using System.Text;
using Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Models;
using Backend.Services;
using Npgsql;

using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode");
Console.WriteLine($"Connection string: {builder.Configuration.GetConnectionString("DefaultConnection")}");

// Database
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.MapEnum<PositionType>("position_type");
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<DBcontext>(options =>
    options.UseNpgsql(dataSource));

var aesKey = builder.Configuration["AES_KEY"] 
             ?? throw new InvalidOperationException("AES key not configured");

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] 
             ?? throw new InvalidOperationException("JWT key is not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer           = true,
      ValidateAudience         = true,
      ValidateLifetime         = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer              = builder.Configuration["Jwt:Issuer"],
      ValidAudience            = builder.Configuration["Jwt:Audience"],
      IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
      ClockSkew                = TimeSpan.Zero   // no grace period on expiry
    };
  });


builder.Services.AddScoped<TokenService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DoctorOnly", policy =>
        policy.RequireClaim("position", "Doctor"));
    
    options.AddPolicy("SecretaryOnly", policy =>
        policy.RequireClaim("position", "Secretary"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("position", "SystemAdministrator", "LocalAdministrator"));

    options.AddPolicy("ClinicStaff", policy =>
        policy.RequireClaim("position", "Doctor", "Nurse", "Secretary"));
});

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("https://medidb.voxvoltera.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//Global Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,        // max requests
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    // Specific Rate limit for login
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.RejectionStatusCode = 429;
});

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Test database connection
using (var scope = app.Services.CreateScope())
{
    
    var db = scope.ServiceProvider.GetRequiredService<DBcontext>();
    try
    {
        await db.Database.CanConnectAsync();
        Console.WriteLine("Database connection successful");
        
        //await Startup.RunAsync(db, aesKey);
        Console.WriteLine("Startup tasks complete");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}



app.Use(async (context, next) =>
{
    Console.WriteLine($"[{DateTime.Now}] Request: {context.Request.Method} {context.Request.Path}");
    await next();
});



app.UseRateLimiter(); // must be before MapControllers
app.UseCors("FrontendPolicy");

app.UseAuthentication();   // must be before UseAuthorization
app.UseAuthorization();

app.MapControllers();


app.Run();