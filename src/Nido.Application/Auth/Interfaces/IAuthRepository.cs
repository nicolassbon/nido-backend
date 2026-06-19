using Nido.Application.Auth;
using Nido.Application.Auth.Helpers;
using Nido.Application.Auth.RefreshToken;
using Nido.Application.Auth.ResetPassword;
namespace Nido.Application.Auth.Interfaces;

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
        string? fotoStorageKey,
        bool aceptaTerminos,
        CancellationToken cancellationToken);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindByGoogleIdAsync(string googleId, CancellationToken cancellationToken);
    Task AddRefreshTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task SavePasswordResetTokenAsync(Guid usuarioId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken);
    Task<RefreshTokenInfo?> GetValidRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task<PasswordResetTokenInfo?> GetValidPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task RemoveRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task ConsumePasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task UpdateUserPasswordAsync(Guid usuarioId, string passwordHash, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task<Guid?> GetUserHogarIdAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
