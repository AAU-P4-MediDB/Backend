using Microsoft.EntityFrameworkCore;
using Backend.Models;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode");

builder.Services.AddDbContext<DBContext>(options =>
  options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
}

app.Use(async (context, next) =>
{
  Console.WriteLine($"[{DateTime.Now}] Request: {context.Request}");
  await next();
});

app.MapControllers();

app.Run();