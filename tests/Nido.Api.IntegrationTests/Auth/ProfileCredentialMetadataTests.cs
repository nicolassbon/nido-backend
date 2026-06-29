using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class ProfileCredentialMetadataTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public ProfileCredentialMetadataTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task PerfilEndpoint_ReturnsCredentialFlagsMatrix(bool hasPassword, bool hasGoogleLinked)
    {
        var client = _factory.CreateClient();
        var email = $"perfil-{Guid.NewGuid()}@test.com";
        string token;
        const string seedPassword = "Password123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            Guid userId;
            Guid hogarId;
            if (hasGoogleLinked)
            {
                (userId, hogarId) = await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Perfil User", email, "google", Guid.NewGuid().ToString("N")), CancellationToken.None);
                if (hasPassword)
                {
                    await repo.UpdateUserPasswordAsync(userId, hasher.Hash(seedPassword), CancellationToken.None);
                }
            }
            else
            {
                (userId, hogarId) = await repo.CreateUserWithPasswordAsync(Guid.NewGuid(), Guid.NewGuid(), "Perfil User", email, hasher.Hash(seedPassword), "M", null, true, CancellationToken.None);
            }

            token = tokenService.CreateToken(userId, hogarId, email, "Perfil User");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/perfiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PerfilBody>();
        Assert.NotNull(body);
        Assert.Equal(hasPassword, body!.HasPassword);
        Assert.Equal(hasGoogleLinked, body.HasGoogleLinked);
    }

    [Fact]
    public async Task PerfilEndpoint_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/perfiles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePerfil_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Perfil Anonimo"), "nombre" },
            { new StringContent("Otro"), "sexo" }
        };

        var response = await client.PutAsync("/api/perfiles", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRestricciones_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/perfiles/restricciones", new
        {
            tipo = "alergia",
            restriccionIds = Array.Empty<Guid>()
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePerfil_RemovesGoogleProfilePicture()
    {
        var client = _factory.CreateClient();
        var email = $"perfil-remove-photo-{Guid.NewGuid()}@test.com";
        const string googlePicture = "https://lh3.googleusercontent.com/a/remove-me";
        string token;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var (userId, hogarId) = await repo.CreateUserWithGoogleAsync(
                new CreateOAuthUserData(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Perfil Sin Foto",
                    email,
                    "google",
                    Guid.NewGuid().ToString("N"),
                    googlePicture),
                CancellationToken.None);

            token = tokenService.CreateToken(userId, hogarId, email, "Perfil Sin Foto");
        }

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Perfil Sin Foto"), "nombre" },
            { new StringContent("Otro"), "sexo" },
            { new StringContent("true"), "removeFoto" }
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var updateResponse = await client.PutAsync("/api/perfiles", form);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var profileResponse = await client.GetFromJsonAsync<PerfilBody>("/api/perfiles");

        Assert.NotNull(profileResponse);
        Assert.Null(profileResponse!.FotoUrl);
    }

    [Fact]
    public async Task PerfilEndpoint_ReturnsRealProfileStats()
    {
        var client = _factory.CreateClient();
        var email = $"perfil-stats-{Guid.NewGuid()}@test.com";
        string token;
        Guid userId;
        Guid hogarId;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();

            (userId, hogarId) = await repo.CreateUserWithPasswordAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Perfil Stats",
                email,
                hasher.Hash("Password123!"),
                "Otro",
                null,
                true,
                CancellationToken.None);

            var productoEscaneadoId = Guid.NewGuid();
            var productoManualId = Guid.NewGuid();
            var logroId = Guid.NewGuid();

            db.Tareas.AddRange(
                new Tarea
                {
                    Id = Guid.NewGuid(),
                    HogarId = hogarId,
                    CreadoPor = userId,
                    Titulo = "Limpiar cocina",
                    Estado = "completada",
                    CompletadoPor = userId,
                    FechaCompletado = DateTime.UtcNow
                },
                new Tarea
                {
                    Id = Guid.NewGuid(),
                    HogarId = hogarId,
                    CreadoPor = userId,
                    Titulo = "Sacar residuos",
                    Estado = "pendiente"
                });
            db.Productos.AddRange(
                new Producto { Id = productoEscaneadoId, Nombre = "Yerba", CodigoBarras = "779123" },
                new Producto { Id = productoManualId, Nombre = "Manzana" });
            db.StockHogars.AddRange(
                new StockHogar
                {
                    Id = Guid.NewGuid(),
                    HogarId = hogarId,
                    ProductoId = productoEscaneadoId,
                    CargadoPor = userId,
                    UpdatedBy = userId,
                    CantidadActual = 1,
                    UnidadMedida = "unidad",
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0
                },
                new StockHogar
                {
                    Id = Guid.NewGuid(),
                    HogarId = hogarId,
                    ProductoId = productoManualId,
                    CargadoPor = userId,
                    UpdatedBy = userId,
                    CantidadActual = 1,
                    UnidadMedida = "unidad",
                    Ubicacion = "Alacena",
                    EstaAbierto = false,
                    PorcentajeConsumido = 0
                });
            db.Logros.Add(new Logro
            {
                Id = logroId,
                Nombre = "Primer logro",
                Descripcion = "Logro de prueba"
            });
            db.LogrosUsuarios.Add(new LogrosUsuario
            {
                Id = Guid.NewGuid(),
                UsuarioId = userId,
                LogroId = logroId,
                FechaObtenido = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            token = tokenService.CreateToken(userId, hogarId, email, "Perfil Stats");
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/api/perfiles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PerfilBody>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.TareasCompletadas);
        Assert.Equal(1, body.ProductosEscaneados);
        Assert.Equal(1, body.Logros);
    }

    private sealed record PerfilBody(
        bool HasPassword,
        bool HasGoogleLinked,
        string? FotoUrl,
        int TareasCompletadas,
        int ProductosEscaneados,
        int Logros);
}
