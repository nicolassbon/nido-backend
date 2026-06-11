using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nido.Api.IntegrationTests.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Application.Common.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Nido.Api.IntegrationTests.ImageUploads;

public sealed class ImageUploadAuthorizationEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly NidoTestWebAppFactory _factory;

    public ImageUploadAuthorizationEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProductUpload_WhenProductBelongsToCurrentHousehold_ReturnsOkAndPersistsStorageKey()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        var user = await RegisterAndAuthenticateAsync(client, "product-upload-owner");

        var productId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productId,
                Nombre = "Yerba",
                ImagenUrl = "products/old.webp"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = Guid.NewGuid(),
                HogarId = user.HogarId,
                ProductoId = productId,
                CargadoPor = user.UsuarioId,
                UpdatedBy = user.UsuarioId,
                CreatedAt = DateTime.UtcNow,
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0
            });
            await db.SaveChangesAsync();
        }

        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/productos/{productId}/imagen", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("products/", storage.UploadedKey, StringComparison.Ordinal);
        Assert.Equal("products/old.webp", storage.DeletedKey);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var savedKey = await verificationDb.Productos
            .Where(x => x.Id == productId)
            .Select(x => x.ImagenUrl)
            .SingleAsync();

        Assert.Equal(storage.UploadedKey, savedKey);
    }

    [Fact]
    public async Task ProductUpload_WhenProductBelongsToAnotherHousehold_ReturnsNotFoundAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        var owner = await RegisterAsync(client, "product-upload-owner-other");
        var attacker = await RegisterAsync(client, "product-upload-attacker");

        var productId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Productos.Add(new Producto
            {
                Id = productId,
                Nombre = "Cafe"
            });
            db.StockHogars.Add(new StockHogar
            {
                Id = Guid.NewGuid(),
                HogarId = owner.HogarId,
                ProductoId = productId,
                CargadoPor = owner.UsuarioId,
                UpdatedBy = owner.UsuarioId,
                CreatedAt = DateTime.UtcNow,
                Ubicacion = "Alacena",
                EstaAbierto = false,
                PorcentajeConsumido = 0
            });
            await db.SaveChangesAsync();
        }

        Authenticate(client, attacker.AccessToken);
        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/productos/{productId}/imagen", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task ElectrodomesticoUpload_WhenBelongsToCurrentHousehold_ReturnsOkAndPersistsStorageKey()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        var user = await RegisterAndAuthenticateAsync(client, "electro-upload-owner");

        var electrodomesticoId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Electrodomesticos.Add(new Electrodomestico
            {
                Id = electrodomesticoId,
                HogarId = user.HogarId,
                Nombre = "Heladera",
                Estado = "Activo",
                Tipo = "Cocina",
                ImagenUrl = "electrodomesticos/old.webp"
            });
            await db.SaveChangesAsync();
        }

        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/electrodomesticos/{electrodomesticoId}/imagen", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("electrodomesticos/", storage.UploadedKey, StringComparison.Ordinal);
        Assert.Equal("electrodomesticos/old.webp", storage.DeletedKey);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var savedKey = await verificationDb.Electrodomesticos
            .Where(x => x.Id == electrodomesticoId)
            .Select(x => x.ImagenUrl)
            .SingleAsync();

        Assert.Equal(storage.UploadedKey, savedKey);
    }

    [Fact]
    public async Task ElectrodomesticoUpload_WhenBelongsToAnotherHousehold_ReturnsNotFoundAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        var owner = await RegisterAsync(client, "electro-upload-owner-other");
        var attacker = await RegisterAsync(client, "electro-upload-attacker");

        var electrodomesticoId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
            db.Electrodomesticos.Add(new Electrodomestico
            {
                Id = electrodomesticoId,
                HogarId = owner.HogarId,
                Nombre = "Lavarropas",
                Estado = "Activo",
                Tipo = "Lavado",
                ImagenUrl = "electrodomesticos/old.webp"
            });
            await db.SaveChangesAsync();
        }

        Authenticate(client, attacker.AccessToken);
        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/electrodomesticos/{electrodomesticoId}/imagen", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task ProductUpload_WhenImageIsMissing_ReturnsBadRequestAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, "product-upload-missing-image");

        using var content = new MultipartFormDataContent();
        var response = await client.PostAsync($"/api/productos/{Guid.NewGuid()}/imagen", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task ProductUpload_WhenImageTypeIsUnsupported_ReturnsBadRequestAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, "product-upload-bad-type");

        using var content = CreateImageContent([0x01, 0x02], "text/plain", "upload.txt");
        var response = await client.PostAsync($"/api/productos/{Guid.NewGuid()}/imagen", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task ProductUpload_WhenImageIsOversized_ReturnsPayloadTooLargeAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, "product-upload-oversized");

        var oversized = new byte[5 * 1024 * 1024 + 1];
        Array.Fill<byte>(oversized, 0x01);
        using var content = CreateImageContent(oversized, "image/png", "upload.png");
        var response = await client.PostAsync($"/api/productos/{Guid.NewGuid()}/imagen", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task ProductUpload_WhenProductDoesNotExist_ReturnsNotFoundAndDoesNotUpload()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, "product-upload-not-found");

        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/productos/{Guid.NewGuid()}/imagen", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    [Fact]
    public async Task RecipeUpload_WhenAuthenticatedUser_ReturnsForbidden()
    {
        var storage = new CapturingFileStorageService();
        var factory = _factory.WithStorageOverride(services => NidoTestWebAppFactory.ReplaceFileStorageService(services, storage));
        var client = factory.CreateClient();
        await RegisterAndAuthenticateAsync(client, "recipe-upload-user");

        using var content = await CreateImageContentAsync();
        var response = await client.PostAsync($"/api/recetas/{Guid.NewGuid()}/imagen", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(storage.UploadedKey);
    }

    private static async Task<MultipartFormDataContent> CreateImageContentAsync()
    {
        var bytes = await CreateValidPngAsync();
        return CreateImageContent(bytes, "image/png", "upload.png");
    }

    private static MultipartFormDataContent CreateImageContent(byte[] bytes, string contentType, string fileName)
    {
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(bytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(imageContent, "imagen", fileName);
        return content;
    }

    private static async Task<byte[]> CreateValidPngAsync()
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 0, 0, 255));
        await using var stream = new MemoryStream();
        await image.SaveAsync(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static async Task<RegisterBody> RegisterAndAuthenticateAsync(HttpClient client, string prefix)
    {
        var user = await RegisterAsync(client, prefix);
        Authenticate(client, user.AccessToken);
        return user;
    }

    private static async Task<RegisterBody> RegisterAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@test.com";
        using var registerContent = RegisterMultipartRequest.Create("Test User", email, "Password123!", "U");
        var response = await client.PostAsync("/api/auth/register", registerContent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        return body!;
    }

    private static void Authenticate(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private sealed record RegisterBody(Guid UsuarioId, Guid HogarId, string AccessToken);

    private sealed class CapturingFileStorageService : IFileStorageService
    {
        public string? UploadedKey { get; private set; }
        public string? DeletedKey { get; private set; }

        public Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            UploadedKey = key;
            return Task.FromResult(new FileStorageUploadResult(key, $"https://assets.test/{key}"));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKey = key;
            return Task.CompletedTask;
        }
    }
}
