using Microsoft.EntityFrameworkCore;
using Nido.Application.Auth;
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Auth;

public sealed class AuthRepository : IAuthRepository
{
    private readonly NidoDbContext _dbContext;

    public AuthRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => _dbContext.Usuarios.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(
        CreateOAuthUserData data,
        CancellationToken cancellationToken)
        => CreateUserInternalAsync(
            data.UsuarioId, data.HogarId, data.Nombre, data.Email,
            string.Empty, "U", null,
            data.OauthProvider, data.OauthId,
            cancellationToken);

    public Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(
        Guid usuarioId,
        Guid hogarId,
        string nombre,
        string email,
        string passwordHash,
        string sexo,
        UserProfileImageMetadata? profileImage,
        CancellationToken cancellationToken)
        => CreateUserInternalAsync(
            usuarioId, hogarId, nombre, email,
            passwordHash, sexo, profileImage,
            null, null,
            cancellationToken);

    private async Task<(Guid UsuarioId, Guid HogarId)> CreateUserInternalAsync(
        Guid usuarioId,
        Guid hogarId,
        string nombre,
        string email,
        string passwordHash,
        string sexo,
        UserProfileImageMetadata? profileImage,
        string? oauthProvider,
        string? oauthId,
        CancellationToken cancellationToken)
    {
        var usuario = new Usuario
        {
            Id = usuarioId,
            Nombre = nombre,
            Email = email,
            PasswordHash = passwordHash,
            Sexo = sexo,
            FotoUrl = null,
            FotoStorageKey = profileImage?.StorageKey,
            FotoContentType = profileImage?.ContentType,
            FotoWidth = profileImage?.Width,
            FotoHeight = profileImage?.Height,
            FotoSizeBytes = profileImage?.Length,
            OauthProvider = oauthProvider,
            OauthId = oauthId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var hogar = new Hogare
        {
            Id = hogarId,
            Nombre = $"Hogar de {nombre}",
            CreatedAt = DateTime.UtcNow
        };

        var membership = new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            HogarId = hogar.Id,
            Rol = "owner",
            Puntos = 0
        };

        var state = new OnboardingState
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            HogarId = hogar.Id,
            Step1CompletedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Usuarios.Add(usuario);
        _dbContext.Hogares.Add(hogar);
        _dbContext.MiembrosHogars.Add(membership);
        _dbContext.OnboardingStates.Add(state);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("usuarios_email_key", StringComparison.OrdinalIgnoreCase) == true ||
                                          ex.InnerException?.Message.Contains("UNIQUE constraint failed: Usuarios.Email", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new InvalidOperationException("Email already exists.", ex);
        }

        return (usuario.Id, hogar.Id);
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var usuario = await _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (usuario is null) return null;
        return new User(usuario.Id, usuario.Nombre, usuario.Email, usuario.PasswordHash, usuario.OauthProvider, usuario.OauthId);
    }

    public async Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken)
    {
        var usuario = await _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OauthProvider == "google" && x.OauthId == googleId, cancellationToken);
        if (usuario is null) return null;
        return new User(usuario.Id, usuario.Nombre, usuario.Email, usuario.PasswordHash, usuario.OauthProvider, usuario.OauthId);
    }

    public async Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.RefreshTokens.Add(token);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await _dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (token is null) return null;
        return new RefreshTokenInfo(token.Id, token.UsuarioId, token.TokenHash, token.ExpiresAt);
    }

    public async Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (token is not null)
        {
            _dbContext.RefreshTokens.Remove(token);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        var usuario = await _dbContext.Usuarios.FindAsync(new object[] { user.Id }, cancellationToken);
        if (usuario is null) return;
        usuario.Email = user.Email;
        usuario.PasswordHash = user.PasswordHash;
        usuario.OauthProvider = user.OauthProvider;
        usuario.OauthId = user.OauthId;
        usuario.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var miembro = await _dbContext.MiembrosHogars
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId, cancellationToken);
        return miembro?.HogarId;
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _dbContext.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (usuario is null) return null;
        return new User(usuario.Id, usuario.Nombre, usuario.Email, usuario.PasswordHash, usuario.OauthProvider, usuario.OauthId);
    }
}
