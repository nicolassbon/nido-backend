using Nido.Application.Auth.Register;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Common.Notifications;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Common.Storage;
using Nido.Application.Common.Security;
using Nido.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Nido.Api.IntegrationTests.Auth;

public sealed class RegisterEndpointTests : IClassFixture<NidoTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NidoTestWebAppFactory _factory;

    public RegisterEndpointTests(NidoTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsCreatedAndJwtClaims()
    {
        using var content = RegisterMultipartRequest.Create("Nico", "nico@test.com", "Password123!", "M");
        var response = await _client.PostAsync("/api/auth/register", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        Assert.Equal("free", body!.Plan);
        Assert.Equal("free", body.SubscriptionStatus);
        Assert.Null(body.TrialEndsAt);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == "usuarioId" && c.Value == body.UsuarioId.ToString());
        Assert.Contains(token.Claims, c => c.Type == "hogarId" && c.Value == body.HogarId.ToString());
        Assert.Contains(token.Claims, c => c.Type == "plan" && c.Value == "Básico");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsSilentSuccess()
    {
        using var firstContent = RegisterMultipartRequest.Create("Nico", "nico-dup@test.com", "Password123!", "M");
        using var secondContent = RegisterMultipartRequest.Create("Nico", "nico-dup@test.com", "Password123!", "M");

        var first = await _client.PostAsync("/api/auth/register", firstContent);
        var second = await _client.PostAsync("/api/auth/register", secondContent);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var body = await second.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        Assert.True(body!.IsSilentSuccess);
        Assert.Null(body.UsuarioId);
        Assert.Null(body.HogarId);
        Assert.Null(body.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(body.Message));
        Assert.False(second.Headers.TryGetValues("Set-Cookie", out var _));
    }

    [Fact]
    public async Task Register_DuplicateEmail_SendsNoticeEmail()
    {
        var spy = new SpyEmailService();
        var client = _factory.WithStorageOverride(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(spy);
        }).CreateClient();

        var email = $"register-dup-{Guid.NewGuid():N}@test.com";
        using var firstContent = RegisterMultipartRequest.Create("Nico", email, "Password123!", "M");
        using var secondContent = RegisterMultipartRequest.Create("Nico", email, "Password123!", "M");

        var first = await client.PostAsync("/api/auth/register", firstContent);
        var second = await client.PostAsync("/api/auth/register", secondContent);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains(email, spy.DuplicateSignupNoticeEmails);
    }

    [Fact]
    public async Task Register_MissingRequiredField_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent("missing-field@test.com"), "email" },
            { new StringContent("Password123!"), "password" },
            { new StringContent("F"), "sexo" }
        };
        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task Register_GoogleOnlyAccount_ReturnsSilentSuccess()
    {
        var email = $"google-add-pw-{Guid.NewGuid()}@test.com";

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            await repo.CreateUserWithGoogleAsync(new CreateOAuthUserData(Guid.NewGuid(), Guid.NewGuid(), "Google User", email, "google", "google-123"), CancellationToken.None);
        }

        using var registerContent = RegisterMultipartRequest.Create("Google User", email, "Password123!", "U");
        var response = await _client.PostAsync("/api/auth/register", registerContent);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        Assert.True(body!.IsSilentSuccess);
        Assert.Null(body.AccessToken);
        Assert.Null(body.UsuarioId);
        Assert.Null(body.HogarId);
    }

    [Fact]
    public async Task Onboarding_WhenUnexpectedExceptionIsThrown_ReturnsInternalServerErrorProblemDetails()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserContext>();
                services.AddScoped<ICurrentUserContext, ThrowingCurrentUserContext>();
            });
        }).CreateClient();

        using var registerContent = RegisterMultipartRequest.Create("Err User", "error-user@test.com", "Password123!", "M");
        var registerResponse = await client.PostAsync("/api/auth/register", registerContent);
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerBody!.AccessToken);

        var response = await client.PatchAsJsonAsync("/api/onboarding/step-2", new { skip = true });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem!.Status);
    }

    [Fact]
    public async Task Register_JsonPayload_ReturnsUnsupportedMediaType()
    {
        using var content = new StringContent("{\"nombre\":\"Nico\",\"email\":\"json@test.com\",\"password\":\"Password123!\",\"sexo\":\"M\"}", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/auth/register", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidImage_ReturnsCreated()
    {
        var storage = new CapturingFileStorageService();
        var client = CreateClientWithStorage(storage);
        var email = $"with-image-{Guid.NewGuid():N}@test.com";
        var image = await CreateValidPngAsync();

        using var content = RegisterMultipartRequest.Create("Img User", email, "Password123!", "F", image, "avatar.png", "image/png");
        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.UsuarioId);
        Assert.NotEqual(Guid.Empty, body.HogarId);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("image/webp", storage.LastContentType);
        Assert.NotNull(storage.LastContent);
        Assert.True(storage.LastContent!.Length > 0);
    }

    [Fact]
    public async Task Register_WithUnsupportedImageFormat_ReturnsBadRequest()
    {
        var email = $"unsupported-{Guid.NewGuid():N}@test.com";
        var gifHeader = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x00, 0x01 };

        using var content = RegisterMultipartRequest.Create("Bad Img", email, "Password123!", "M", gifHeader, "avatar.gif", "image/gif");
        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task Register_WithOversizeImage_ReturnsBadRequest()
    {
        var email = $"oversize-{Guid.NewGuid():N}@test.com";
        var oversize = new byte[5 * 1024 * 1024 + 1];
        Array.Fill<byte>(oversize, 0x01);

        using var content = RegisterMultipartRequest.Create("Big Img", email, "Password123!", "F", oversize, "avatar.png", "image/png");
        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task Register_WithCorruptImage_ReturnsBadRequest()
    {
        var email = $"corrupt-{Guid.NewGuid():N}@test.com";
        var notAnImage = Encoding.UTF8.GetBytes("this-is-not-a-valid-image-payload");

        using var content = RegisterMultipartRequest.Create("Corrupt Img", email, "Password123!", "M", notAnImage, "avatar.png", "image/png");
        var response = await _client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem!.Status);
    }

    [Fact]
    public async Task Register_StorageUploadFailure_ReturnsServerError()
    {
        var client = CreateClientWithStorage(new ThrowingFileStorageService(throwOnUpload: true));

        var email = $"storage-fail-{Guid.NewGuid():N}@test.com";
        var image = await CreateValidJpegAsync();
        using var content = RegisterMultipartRequest.Create("Storage Fail", email, "Password123!", "F", image, "avatar.jpg", "image/jpeg");

        var response = await client.PostAsync("/api/auth/register", content);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var exists = await db.Usuarios.AnyAsync(x => x.Email == email);
        Assert.False(exists);
    }

    [Fact]
    public async Task Register_ConcurrentDuplicateEmail_OnlyOneWins()
    {
        var email = $"race-{Guid.NewGuid():N}@test.com";

        using var firstContent = RegisterMultipartRequest.Create("Race User", email, "Password123!", "U");
        using var secondContent = RegisterMultipartRequest.Create("Race User", email, "Password123!", "U");

        var first = await _client.PostAsync("/api/auth/register", firstContent);
        var second = await _client.PostAsync("/api/auth/register", secondContent);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var duplicateBody = await second.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.NotNull(duplicateBody);
        Assert.True(duplicateBody!.IsSilentSuccess);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NidoDbContext>();
        var count = await db.Usuarios.CountAsync(x => x.Email == email);
        Assert.Equal(1, count);
    }

    private sealed record RegisterBody(
        Guid? UsuarioId,
        Guid? HogarId,
        string? AccessToken,
        string Message,
        bool IsSilentSuccess,
        string? Plan,
        string? SubscriptionStatus,
        DateTime? TrialEndsAt);
    private sealed record ProblemDetailsBody(int Status, string? Title, string? Detail);

    private sealed class ThrowingCurrentUserContext : ICurrentUserContext
    {
        public Guid UsuarioId => throw new Exception("boom");
        public Guid HogarId => throw new Exception("boom");
    }

    private sealed class ThrowingFileStorageService : IFileStorageService
    {
        private readonly bool _throwOnUpload;

        public ThrowingFileStorageService(bool throwOnUpload)
        {
            _throwOnUpload = throwOnUpload;
        }

        public Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            if (_throwOnUpload)
            {
                throw new Exception("Simulated storage upload failure");
            }

            return Task.FromResult(new FileStorageUploadResult(key, $"https://cdn.test.local/{key}"));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class SpyEmailService : IEmailService
    {
        public List<string> DuplicateSignupNoticeEmails { get; } = [];

        public Task SendInvitationEmailAsync(string toEmail, string hogarNombre, string invitadoPorNombre, string invitationToken, CancellationToken ct) => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken ct) => Task.CompletedTask;

        public Task SendGoogleOnlyInfoEmailAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;

        public Task SendDuplicateSignupNoticeEmailAsync(string toEmail, CancellationToken ct)
        {
            DuplicateSignupNoticeEmails.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingFileStorageService : IFileStorageService
    {
        public string? LastContentType { get; private set; }
        public byte[]? LastContent { get; private set; }

        public async Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            LastContentType = contentType;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            LastContent = ms.ToArray();
            return new FileStorageUploadResult(key, $"https://cdn.test.local/{key}");
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private HttpClient CreateClientWithStorage(IFileStorageService storage)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                NidoTestWebAppFactory.ReplaceFileStorageService(services, storage);
            });
        }).CreateClient();
    }

    private static async Task<byte[]> CreateValidPngAsync()
    {
        using var image = new Image<Rgba32>(4, 4);
        await using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static async Task<byte[]> CreateValidJpegAsync()
    {
        using var image = new Image<Rgba32>(4, 4);
        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder());
        return ms.ToArray();
    }
}
