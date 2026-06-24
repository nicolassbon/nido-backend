using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Nido.Application.Auth;
using Nido.Application.Onboarding;
using Nido.Application.Hogares;
using Nido.Application.Electrodomesticos;
using Nido.Application.Alacena;
using Nido.Application.Productos;
using Nido.Application.Preferencias;
using Nido.Application.Recetas;
using Nido.Application.Estadisticas;
using Nido.Application.Insights;
using Nido.Application.ListaCompras;
using Nido.Application.Common.Security;
using Nido.Api.Errors;
using Nido.Api.Middleware;
using Nido.Api.Security;
using Nido.Infrastructure;
using Nido.Infrastructure.Persistence;
using Nido.Application.UsuariosPerfil;
using Nido.Application.Finanzas;
using Nido.Application.Tareas;
using Nido.Application.Notificaciones;
using Nido.Application.Telegram;
using Nido.Infrastructure.Auth;
using Nido.Api.OpenApi;
using Nido.Api.Controllers;
using Scalar.AspNetCore;
using Nido.Application.Tickets;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
if (environment == "Development")
{
    DotNetEnv.Env.TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new Nido.Api.Serialization.DateTimeUtcJsonConverter());
    });
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<SecurityRequirementsOperationTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddNidoInfrastructure(builder.Configuration);
builder.Services.AddCommonSecurityModule();

builder.Services.AddAuthModule();
builder.Services.AddOnboardingModule();
builder.Services.AddHogaresModule();
builder.Services.AddElectrodomesticosModule();
builder.Services.AddAlacenaModule();
builder.Services.AddProductosModule();
builder.Services.AddPreferenciasModule();
builder.Services.AddRecetasModule();
builder.Services.AddTicketsModule();
builder.Services.AddUsuariosPerfilModule();
builder.Services.AddEstadisticasModule();
builder.Services.AddInsightsModule();
builder.Services.AddListaComprasModule();
builder.Services.AddFinanzasModule();
builder.Services.AddTareasModule();
builder.Services.AddNotificacionesModule();
builder.Services.AddTelegramWebhook(builder.Configuration);
builder.Services.AddTelegramSenderWorker(builder.Configuration);


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

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(TelegramWebhookController.RateLimitPolicyName, httpContext =>
    {
        var telegramOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<TelegramOptions>>().Value;
        var permitLimit = telegramOptions.WebhookRateLimitPermitPerWindow;
        var windowSeconds = telegramOptions.WebhookRateLimitWindowSeconds;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.OnRejected = async (context, ct) =>
    {
        var telemetry = context.HttpContext.RequestServices
            .GetService<Nido.Infrastructure.Telegram.Webhook.ITelegramWebhookTelemetry>();

        var sw = context.HttpContext.Items
            .TryGetValue(Nido.Api.Middleware.TelegramWebhookSecretMiddleware.StopwatchItemKey, out var raw)
            ? raw as System.Diagnostics.Stopwatch
            : null;
        sw?.Stop();
        var elapsedMs = sw?.Elapsed.TotalMilliseconds ?? 0d;
        telemetry?.RecordThrottled(elapsedMs);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
            ? ra
            : TimeSpan.FromSeconds(1);
        context.HttpContext.Response.Headers.RetryAfter =
            ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        await context.HttpContext.Response.WriteAsync(string.Empty, ct);
    };
});

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

builder.Services.AddHttpClient();

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

app.MapGet("/health", async (NidoDbContext dbContext, CancellationToken ct) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(ct);
    return canConnect
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.UseStaticFiles();
app.UseCors("Frontend");
app.UseCookiePolicy();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TelegramWebhookSecretMiddleware>();
app.UseMiddleware<TelegramWebhookPayloadSizeMiddleware>();
app.UseRateLimiter();
app.MapControllers();

app.Run();

public partial class Program { }
