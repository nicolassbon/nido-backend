using Microsoft.EntityFrameworkCore;
using Nido.Application.Electrodomesticos;
using Nido.Infrastructure;
using Nido.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddNidoInfrastructure(builder.Configuration);

builder.Services.AddScoped<CreateElectrodomesticoHandler>();
builder.Services.AddScoped<GetElectrodomesticosHandler>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("Frontend");

app.MapControllers();

app.MapGet("/hello", () => Results.Ok(new { message = "Bienvenido a Nido!" }));

app.Run();

public partial class Program { }