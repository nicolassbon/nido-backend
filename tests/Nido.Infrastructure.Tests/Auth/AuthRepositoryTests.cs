using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Auth;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Tests.Shared;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class AuthRepositoryTests : IAsyncLifetime
{
    private PostgresTestDatabase? _testDatabase;
    private NidoDbContext? _dbContext;
    private AuthRepository? _repository;

    private NidoDbContext DbContext => _dbContext ?? throw new InvalidOperationException("Test database not initialized.");
    private AuthRepository Repository => _repository ?? throw new InvalidOperationException("Repository not initialized.");

    public async Task InitializeAsync()
    {
        var server = await PostgresTestServer.GetSharedAsync();
        _testDatabase = await server.CreateDatabaseAsync(nameof(AuthRepositoryTests));

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_testDatabase.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _dbContext = new NidoDbContext(options);
        await _dbContext.Database.MigrateAsync();
        _repository = new AuthRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_testDatabase is not null)
        {
            await _testDatabase.DisposeAsync();
        }
    }

    private static Usuario CreateUsuario(string email, string? passwordHash = null, string? oauthProvider = null, string? oauthId = null)
    {
        return new Usuario
        {
            Id = Guid.NewGuid(),
            Nombre = "Test",
            Email = email,
            PasswordHash = passwordHash,
            OauthProvider = oauthProvider,
            OauthId = oauthId,
            Sexo = "M",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task FindByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var usuario = CreateUsuario("test@mail.com", "hashedpassword");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var result = await Repository.FindByEmailAsync("test@mail.com", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(usuario.Id, result.Id);
        Assert.Equal("test@mail.com", result.Email);
        Assert.Equal("hashedpassword", result.PasswordHash);
    }

    [Fact]
    public async Task FindByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        var result = await Repository.FindByEmailAsync("none@mail.com", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByGoogleIdAsync_Existing_ReturnsUser()
    {
        var usuario = CreateUsuario("google@mail.com", oauthProvider: "google", oauthId: "g123");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var result = await Repository.FindByGoogleIdAsync("g123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(usuario.Id, result.Id);
        Assert.Equal("google", result.OauthProvider);
        Assert.Equal("g123", result.OauthId);
    }

    [Fact]
    public async Task FindByGoogleIdAsync_NonExisting_ReturnsNull()
    {
        var result = await Repository.FindByGoogleIdAsync("none", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveChanges_DuplicateOAuthIdentity_ThrowsDbUpdateException()
    {
        DbContext.Usuarios.Add(CreateUsuario("google-1@mail.com", oauthProvider: "google", oauthId: "g123"));
        DbContext.Usuarios.Add(CreateUsuario("google-2@mail.com", oauthProvider: "google", oauthId: "g123"));

        await Assert.ThrowsAsync<DbUpdateException>(() => DbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AddRefreshTokenAsync_AddsToken()
    {
        var usuario = CreateUsuario("token@mail.com");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddDays(7);
        await Repository.AddRefreshTokenAsync(usuario.Id, "hash123", expiresAt, CancellationToken.None);

        var token = await DbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == "hash123");
        Assert.NotNull(token);
        Assert.Equal(usuario.Id, token.UsuarioId);
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task GetValidRefreshTokenAsync_ValidToken_ReturnsTokenInfo()
    {
        var usuario = CreateUsuario("valid@mail.com");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "validhash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        var result = await Repository.GetValidRefreshTokenAsync("validhash", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(refreshToken.Id, result.Id);
        Assert.Equal("validhash", result.TokenHash);
    }

    [Fact]
    public async Task GetValidRefreshTokenAsync_ExpiredToken_ReturnsNull()
    {
        var usuario = CreateUsuario("expired@mail.com");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "expiredhash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        var result = await Repository.GetValidRefreshTokenAsync("expiredhash", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRefreshTokenAsync_RemovesToken()
    {
        var usuario = CreateUsuario("remove@mail.com");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "removehash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        await Repository.RemoveRefreshTokenAsync("removehash", CancellationToken.None);
        await DbContext.SaveChangesAsync();

        var exists = await DbContext.RefreshTokens.AnyAsync(r => r.TokenHash == "removehash");
        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesFields()
    {
        var usuario = CreateUsuario("update@mail.com", oauthProvider: "google", oauthId: "g456");
        DbContext.Usuarios.Add(usuario);
        await DbContext.SaveChangesAsync();

        var user = new User(usuario.Id, "Test", "update@mail.com", "newpasswordhash", null, null);
        await Repository.UpdateUserAsync(user, CancellationToken.None);

        var updated = await DbContext.Usuarios.FindAsync(usuario.Id);
        Assert.NotNull(updated);
        Assert.Equal("newpasswordhash", updated.PasswordHash);
        Assert.Null(updated.OauthProvider);
        Assert.Null(updated.OauthId);
    }
}
