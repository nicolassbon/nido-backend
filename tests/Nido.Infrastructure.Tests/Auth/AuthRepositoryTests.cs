using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.Interfaces;
using Nido.Application.Auth.RefreshToken;
using Nido.Infrastructure.Auth;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class AuthRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly NidoDbContext _dbContext;
    private readonly AuthRepository _repository;

    public AuthRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _connection.CreateFunction<string>("uuid_generate_v4", () => Guid.NewGuid().ToString());
        _connection.CreateFunction<string>("now", () => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new NidoDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new AuthRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
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
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.FindByEmailAsync("test@mail.com", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(usuario.Id, result.Id);
        Assert.Equal("test@mail.com", result.Email);
        Assert.Equal("hashedpassword", result.PasswordHash);
    }

    [Fact]
    public async Task FindByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        var result = await _repository.FindByEmailAsync("none@mail.com", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByGoogleIdAsync_Existing_ReturnsUser()
    {
        var usuario = CreateUsuario("google@mail.com", oauthProvider: "google", oauthId: "g123");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.FindByGoogleIdAsync("g123", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(usuario.Id, result.Id);
        Assert.Equal("google", result.OauthProvider);
        Assert.Equal("g123", result.OauthId);
    }

    [Fact]
    public async Task FindByGoogleIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _repository.FindByGoogleIdAsync("none", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveChanges_DuplicateOAuthIdentity_ThrowsDbUpdateException()
    {
        _dbContext.Usuarios.Add(CreateUsuario("google-1@mail.com", oauthProvider: "google", oauthId: "g123"));
        _dbContext.Usuarios.Add(CreateUsuario("google-2@mail.com", oauthProvider: "google", oauthId: "g123"));

        await Assert.ThrowsAsync<DbUpdateException>(() => _dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task AddRefreshTokenAsync_AddsToken()
    {
        var usuario = CreateUsuario("token@mail.com");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var expiresAt = DateTime.UtcNow.AddDays(7);
        await _repository.AddRefreshTokenAsync(usuario.Id, "hash123", expiresAt, CancellationToken.None);

        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == "hash123");
        Assert.NotNull(token);
        Assert.Equal(usuario.Id, token.UsuarioId);
        Assert.True(token.ExpiresAt > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task GetValidRefreshTokenAsync_ValidToken_ReturnsTokenInfo()
    {
        var usuario = CreateUsuario("valid@mail.com");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "validhash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetValidRefreshTokenAsync("validhash", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(refreshToken.Id, result.Id);
        Assert.Equal("validhash", result.TokenHash);
    }

    [Fact]
    public async Task GetValidRefreshTokenAsync_ExpiredToken_ReturnsNull()
    {
        var usuario = CreateUsuario("expired@mail.com");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "expiredhash",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetValidRefreshTokenAsync("expiredhash", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRefreshTokenAsync_RemovesToken()
    {
        var usuario = CreateUsuario("remove@mail.com");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = "removehash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        await _repository.RemoveRefreshTokenAsync("removehash", CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var exists = await _dbContext.RefreshTokens.AnyAsync(r => r.TokenHash == "removehash");
        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateUserAsync_UpdatesFields()
    {
        var usuario = CreateUsuario("update@mail.com", oauthProvider: "google", oauthId: "g456");
        _dbContext.Usuarios.Add(usuario);
        await _dbContext.SaveChangesAsync();

        var user = new User(usuario.Id, "Test", "update@mail.com", "newpasswordhash", null, null);
        await _repository.UpdateUserAsync(user, CancellationToken.None);

        var updated = await _dbContext.Usuarios.FindAsync(usuario.Id);
        Assert.NotNull(updated);
        Assert.Equal("newpasswordhash", updated.PasswordHash);
        Assert.Null(updated.OauthProvider);
        Assert.Null(updated.OauthId);
    }
}
