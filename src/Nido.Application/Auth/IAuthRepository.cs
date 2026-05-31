using Nido.Application.Common.ProfileImages;

namespace Nido.Application.Auth;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithGoogleAsync(
        CreateOAuthUserData data,
        CancellationToken cancellationToken);
    Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithPasswordAsync(
        Guid usuarioId,
        Guid hogarId,
        string nombre,
        string email,
        string passwordHash,
        string sexo,
        UserProfileImageMetadata? profileImage,
        CancellationToken cancellationToken);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
