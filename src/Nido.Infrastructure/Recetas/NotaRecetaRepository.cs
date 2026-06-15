using Microsoft.EntityFrameworkCore;
using Nido.Application.Recetas;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Recetas;

public sealed class NotaRecetaRepository : INotaRecetaRepository
{
    private readonly NidoDbContext _db;

    public NotaRecetaRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<NotaItem> AddAsync(Guid recetaId, Guid hogarId, Guid usuarioId, string texto, CancellationToken ct)
    {
        var nota = new NotaReceta
        {
            Id        = Guid.NewGuid(),
            RecetaId  = recetaId,
            HogarId   = hogarId,
            UsuarioId = usuarioId,
            Texto     = texto,
            CreatedAt = DateTime.UtcNow,
        };

        _db.NotasReceta.Add(nota);
        await _db.SaveChangesAsync(ct);

        return await _db.NotasReceta
            .AsNoTracking()
            .Where(n => n.Id == nota.Id)
            .Select(n => new NotaItem(
                n.Id, n.RecetaId, n.HogarId, n.UsuarioId,
                n.Usuario.Nombre, n.Usuario.FotoUrl,
                n.Texto, n.CreatedAt))
            .FirstAsync(ct);
    }

    public async Task<IReadOnlyList<NotaItem>> GetByRecetaAndHogarAsync(Guid recetaId, Guid hogarId, CancellationToken ct)
    {
        return await _db.NotasReceta
            .AsNoTracking()
            .Where(n => n.RecetaId == recetaId && n.HogarId == hogarId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotaItem(
                n.Id, n.RecetaId, n.HogarId, n.UsuarioId,
                n.Usuario.Nombre, n.Usuario.FotoUrl,
                n.Texto, n.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid notaId, Guid hogarId, Guid usuarioId, CancellationToken ct)
    {
        var nota = await _db.NotasReceta
            .FirstOrDefaultAsync(n => n.Id == notaId && n.HogarId == hogarId && n.UsuarioId == usuarioId, ct);

        if (nota is null) return;

        _db.NotasReceta.Remove(nota);
        await _db.SaveChangesAsync(ct);
    }
}
