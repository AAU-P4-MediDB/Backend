using Microsoft.EntityFrameworkCore;
using Backend.Models;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode");

Console.WriteLine($"Connection string: {builder.Configuration.GetConnectionString("DefaultConnection")}");

builder.Services.AddDbContext<DBcontext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var app = builder.Build();

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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection failed: {ex.Message}");
    }
}

app.Use(async (context, next) =>
{
  Console.WriteLine($"[{DateTime.Now}] Request: {context.Request}");
  await next();
});

app.MapControllers();

app.Run();