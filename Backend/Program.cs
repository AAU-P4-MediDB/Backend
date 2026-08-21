using Microsoft.EntityFrameworkCore;
using System.Text;
using Backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Backend.Services;
using Npgsql;
using Fido2NetLib;
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
    options
        .UseNpgsql(dataSource)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        .LogTo(
            Console.WriteLine,
            LogLevel.Information
        ));

var aesKey = builder.Configuration["AES_KEY"] 
             ?? throw new InvalidOperationException("AES key not configured");

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

//passkey
builder.Services.AddScoped<PasskeyService>();
builder.Services.AddScoped<MfaService>();
builder.Services.AddScoped<RecoveryCodeService>();
builder.Services.AddSingleton(sp =>
{
    var config = new Fido2Configuration
    {
        ServerName = "MediDB",
        ServerDomain = "medidb.voxvoltera.com",
        Origins = new HashSet<string>
        {
            "https://medidb.voxvoltera.com"
        }
    };

    return new Fido2(config);
});

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

    // NFR-04: accept the accessToken cookie (HttpOnly/Secure/SameSite=Strict,
    // set by UserManagementController.SetAuthCookies) as a fallback when no
    // Authorization header is present, so the cookie is a real credential
    // rather than one nothing ever reads.
    options.Events = new JwtBearerEvents
    {
      OnMessageReceived = context =>
      {
        if (string.IsNullOrEmpty(context.Token) &&
            context.Request.Cookies.TryGetValue("accessToken", out var cookieToken))
        {
          context.Token = cookieToken;
        }
        return Task.CompletedTask;
      }
    };
  });


builder.Logging.ClearProviders();

builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

builder.Logging.AddDebug();

builder.Services.AddHttpClient("yubico", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DoctorOnly", policy =>
        policy.RequireClaim("position", "Doctor"));

    // Claim values must match PositionType.ToString() exactly (set at login
    // in TokenService.GenerateToken) — "secretary" is lowercase on the enum
    // itself, and the admin positions carry an underscore. Previously these
    // policies compared against values ("Secretary", "SystemAdministrator",
    // "LocalAdministrator") that never appear in a real token, so nobody —
    // including actual admins — could ever satisfy them.
    options.AddPolicy("SecretaryOnly", policy =>
        policy.RequireClaim("position", "secretary"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("position", "System_administrator", "Local_administrator"));

    // Spec (4.2): local-admin management must be restricted to sysadmins
    // specifically, not local admins too.
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireClaim("position", "System_administrator"));

    options.AddPolicy("ClinicStaff", policy =>
        policy.RequireClaim("position", "Doctor", "Nurse", "secretary", "Local_administrator", "System_administrator"));
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

// ============================================================
// GLOBAL RATE LIMITING - FIXED WITH RETRY-AFTER
// ============================================================
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

    // ✅ FIX: Add Retry-After header to 429 responses (NFR-08)
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Add Retry-After header (60 seconds for login rate limit window)
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        
        // Optional: Add a JSON response body
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests. Please try again later.\",\"retryAfter\":60}",
            cancellationToken
        );
        
        await ValueTask.CompletedTask;
    };
});


var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

startupLogger.LogInformation("====================================");
startupLogger.LogInformation("APPLICATION STARTED");
startupLogger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
startupLogger.LogInformation("====================================");

// ============================================================
// SECURITY HEADERS MIDDLEWARE - FIXED
// ============================================================
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
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("HTTP {Method} {Path}",
        context.Request.Method,
        context.Request.Path);

    await next();

    logger.LogInformation("Response {StatusCode}",
        context.Response.StatusCode);
});


app.UseRateLimiter(); // must be before MapControllers
app.UseCors("FrontendPolicy");


app.UseAuthentication();   // must be before UseAuthorization
app.UseAuthorization();
app.UseSession();

// ============================================================
// GLOBAL CACHE-CONTROL MIDDLEWARE FOR ALL API RESPONSES
// FIXES NFR-15: Patient data must not be cached
// ============================================================
app.Use(async (context, next) =>
{
    await next();
    
    // Add no-cache headers to ALL API responses
    // This ensures patient data is never cached by browsers or proxies
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        // Only add if not already set by the endpoint
        if (!context.Response.Headers.ContainsKey("Cache-Control"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
        }
    }
});

app.MapControllers();

app.Run();