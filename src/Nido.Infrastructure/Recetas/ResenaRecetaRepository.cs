using Microsoft.EntityFrameworkCore;
using Nido.Application.Recetas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Recetas;

public sealed class ResenaRecetaRepository : IResenaRecetaRepository
{
    private readonly NidoDbContext _db;

    public ResenaRecetaRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<ResenaItem> UpsertAsync(Guid recetaId, Guid usuarioId, int puntuacion, string? comentario, CancellationToken ct)
    {
        var existing = await _db.ReseniasReceta
            .FirstOrDefaultAsync(r => r.RecetaId == recetaId && r.UsuarioId == usuarioId, ct);

        if (existing is null)
        {
            existing = new ReseniasRecetum
            {
                Id         = Guid.NewGuid(),
                RecetaId   = recetaId,
                UsuarioId  = usuarioId,
                Puntuacion = puntuacion,
                Comentario = comentario,
                CreatedAt  = DateTime.UtcNow,
            };
            _db.ReseniasReceta.Add(existing);
        }
        else
        {
            existing.Puntuacion = puntuacion;
            existing.Comentario = comentario;
            existing.UpdatedAt  = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(existing.Id, ct)
            ?? throw new InvalidOperationException("Reseña no encontrada después de upsert.");
    }

    public async Task<IReadOnlyList<ResenaItem>> GetByRecetaAsync(Guid recetaId, CancellationToken ct)
    {
        return await _db.ReseniasReceta
            .AsNoTracking()
            .Where(r => r.RecetaId == recetaId)
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Select(r => new ResenaItem(
                r.Id,
                r.RecetaId,
                r.UsuarioId,
                r.Usuario.Nombre,
                r.Usuario.FotoUrl,
                r.Puntuacion ?? 0,
                r.Comentario,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<ResenaItem?> GetByRecetaAndUsuarioAsync(Guid recetaId, Guid usuarioId, CancellationToken ct)
    {
        return await _db.ReseniasReceta
            .AsNoTracking()
            .Where(r => r.RecetaId == recetaId && r.UsuarioId == usuarioId)
            .Select(r => new ResenaItem(
                r.Id,
                r.RecetaId,
                r.UsuarioId,
                r.Usuario.Nombre,
                r.Usuario.FotoUrl,
                r.Puntuacion ?? 0,
                r.Comentario,
                r.CreatedAt,
                r.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task DeleteAsync(Guid recetaId, Guid usuarioId, CancellationToken ct)
    {
        var existing = await _db.ReseniasReceta
            .FirstOrDefaultAsync(r => r.RecetaId == recetaId && r.UsuarioId == usuarioId, ct);

        if (existing is null) return;

        _db.ReseniasReceta.Remove(existing);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ResenaResumen> GetResumenAsync(Guid recetaId, CancellationToken ct)
    {
        var stats = await _db.ReseniasReceta
            .AsNoTracking()
            .Where(r => r.RecetaId == recetaId && r.Puntuacion != null)
            .GroupBy(r => 1)
            .Select(g => new { Promedio = g.Average(r => (decimal)r.Puntuacion!.Value), Total = g.Count() })
            .FirstOrDefaultAsync(ct);

        return stats is null
            ? new ResenaResumen(0m, 0)
            : new ResenaResumen(Math.Round(stats.Promedio, 1), stats.Total);
    }

    public async Task<IReadOnlyDictionary<Guid, ResenaResumen>> GetResumenesAsync(IEnumerable<Guid> recetaIds, CancellationToken ct)
    {
        var ids = recetaIds.ToHashSet();
        if (ids.Count == 0) return new Dictionary<Guid, ResenaResumen>();

        var rows = await _db.ReseniasReceta
            .AsNoTracking()
            .Where(r => ids.Contains(r.RecetaId) && r.Puntuacion != null)
            .GroupBy(r => r.RecetaId)
            .Select(g => new
            {
                RecetaId = g.Key,
                Promedio = g.Average(r => (decimal)r.Puntuacion!.Value),
                Total    = g.Count(),
            })
            .ToListAsync(ct);

        return rows.ToDictionary(
            r => r.RecetaId,
            r => new ResenaResumen(Math.Round(r.Promedio, 1), r.Total));
    }

    private async Task<ResenaItem?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.ReseniasReceta
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ResenaItem(
                r.Id,
                r.RecetaId,
                r.UsuarioId,
                r.Usuario.Nombre,
                r.Usuario.FotoUrl,
                r.Puntuacion ?? 0,
                r.Comentario,
                r.CreatedAt,
                r.UpdatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
