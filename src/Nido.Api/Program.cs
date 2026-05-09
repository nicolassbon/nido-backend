using Nido.Application.Households;
using Nido.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.TraversePath().Load();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddNidoInfrastructure(builder.Configuration);
builder.Services.AddScoped<CreateHouseholdHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
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
