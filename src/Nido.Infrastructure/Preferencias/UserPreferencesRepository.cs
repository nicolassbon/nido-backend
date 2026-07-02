using Microsoft.EntityFrameworkCore;
using Nido.Application.Preferencias;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Preferencias;

public sealed class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly NidoDbContext _db;

    public UserPreferencesRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<UserPreferencesResult> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        return new UserPreferencesResult(
            usuario.AlertaVencimientoDias,
            string.IsNullOrWhiteSpace(usuario.TemaPreferido)
                ? UserThemeModes.System
                : usuario.TemaPreferido);
    }

    public async Task<UserPreferencesResult> UpdateAsync(Guid usuarioId, int? diasAlerta, string? temaPreferido, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (diasAlerta.HasValue)
        {
            usuario.AlertaVencimientoDias = diasAlerta.Value;
        }

        if (temaPreferido is not null)
        {
            usuario.TemaPreferido = temaPreferido;
        }

        await _db.SaveChangesAsync(ct);

        return new UserPreferencesResult(usuario.AlertaVencimientoDias, usuario.TemaPreferido);
    }
}
