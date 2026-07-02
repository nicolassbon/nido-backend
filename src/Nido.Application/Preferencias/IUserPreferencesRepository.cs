namespace Nido.Application.Preferencias;

public interface IUserPreferencesRepository
{
    Task<UserPreferencesResult> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct);
    Task<UserPreferencesResult> UpdateAsync(Guid usuarioId, int? diasAlerta, string? temaPreferido, CancellationToken ct);
}
