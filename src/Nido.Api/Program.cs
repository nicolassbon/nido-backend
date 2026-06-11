using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Nido.Application.Auth;
using Nido.Application.Onboarding;
using Nido.Application.Hogares;
using Nido.Application.Electrodomesticos;
using Nido.Application.Alacena;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Application.Recetas;
using Nido.Application.Common.Security;
using Nido.Api.Errors;
using Nido.Api.Security;
using Nido.Infrastructure;
using Nido.Infrastructure.Persistence;
using Nido.Application.UsuariosPerfil;
using Nido.Application.Finanzas;
using Nido.Infrastructure.Auth;
using Nido.Api.OpenApi;
using Scalar.AspNetCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (environment == "Development")
{
    DotNetEnv.Env.TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<SecurityRequirementsOperationTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddNidoInfrastructure(builder.Configuration);

builder.Services.AddAuthModule();
builder.Services.AddOnboardingModule();
builder.Services.AddHogaresModule();
builder.Services.AddElectrodomesticosModule();
builder.Services.AddAlacenaModule();
builder.Services.AddProductosModule();
builder.Services.AddPreferenciasModule();
builder.Services.AddRecetasModule();
builder.Services.AddUsuariosPerfilModule();
builder.Services.AddFinanzasModule();



builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrEmpty(jwtOptions.Issuer),
            ValidateAudience = !string.IsNullOrEmpty(jwtOptions.Audience),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
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

if (!app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseCookiePolicy();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
