using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Nido.Application.Electrodomesticos;
using Nido.Application.Auth;
using Nido.Application.Onboarding;
using Nido.Application.Hogares;
using Nido.Application.Common.Security;
using Nido.Application.Alacena;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Api.Errors;
using Nido.Api.Security;
using Nido.Infrastructure;
using Nido.Infrastructure.Persistence;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (environment == "Development")
{
    DotNetEnv.Env.TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddNidoInfrastructure(builder.Configuration);

builder.Services.AddScoped<CreateElectrodomesticoHandler>();
builder.Services.AddScoped<GetElectrodomesticosHandler>();
builder.Services.AddScoped<RegisterUserHandler>();
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<GoogleLoginHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<LinkGoogleHandler>();
builder.Services.AddScoped<SaveHouseholdStepHandler>();
builder.Services.AddScoped<SaveEquipmentStepHandler>();
builder.Services.AddScoped<SaveWellnessStepHandler>();
builder.Services.AddScoped<GetPreferenciasAlimentariasHandler>();
builder.Services.AddScoped<GetAlergiasHandler>();
builder.Services.AddScoped<GetMetasHandler>();
builder.Services.AddScoped<InvitarConviventeHandler>();
builder.Services.AddScoped<AceptarInvitacionHandler>();
builder.Services.AddScoped<GetMiembrosHandler>();
builder.Services.AddScoped<GetProductByBarcodeHandler>();
builder.Services.AddScoped<GetStockItemsHandler>();
builder.Services.AddScoped<CreateStockItemHandler>();
builder.Services.AddScoped<UpdateStockItemHandler>();
builder.Services.AddScoped<DeleteStockItemHandler>();
builder.Services.AddScoped<GetUserPreferencesHandler>();
builder.Services.AddScoped<UpdateUserPreferencesHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "nido-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "nido-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

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
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.Secure = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors("Frontend");
app.UseCookiePolicy();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/hello", () => Results.Ok(new { message = "Bienvenido a Nido!" }));

app.Run();

public partial class Program { }
