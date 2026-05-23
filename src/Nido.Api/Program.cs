using Nido.Application.Households;
using Nido.Infrastructure;
using Nido.Application.Electrodomesticos;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddNidoInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateHouseholdHandler>();
builder.Services.AddScoped<CreateElectrodomesticoHandler>();

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

app.UseCors("Frontend");

app.MapControllers();

app.MapGet("/hello", () => Results.Ok(new { message = "Bienvenido a Nido!" }));

app.Run();

/// <summary>
/// Exposed for integration tests via WebApplicationFactory.
/// </summary>
public partial class Program { }
