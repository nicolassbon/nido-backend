namespace Nido.Application.Auth;

public interface IAuthRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<(Guid UsuarioId, Guid HogarId)> CreateUserWithDefaultHouseholdAsync(
        string nombre,
        string email,
        string passwordHash,
        string sexo,
        string? fotoUrl,
        CancellationToken cancellationToken);
}
