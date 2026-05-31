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

        return new UserPreferencesResult(usuario.AlertaVencimientoDias);
    }

    public async Task<UserPreferencesResult> UpdateAsync(Guid usuarioId, int diasAlerta, CancellationToken ct)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        usuario.AlertaVencimientoDias = diasAlerta;
        await _db.SaveChangesAsync(ct);

        return new UserPreferencesResult(usuario.AlertaVencimientoDias);
    }
}
