using Microsoft.EntityFrameworkCore;
using Nido.Application.Hogares;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Hogares;

public sealed class HogarRepository : IHogarRepository
{
    private readonly NidoDbContext _db;

    public HogarRepository(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct)
    {
        return await _db.Hogares
            .AsNoTracking()
            .Where(h => h.Id == hogarId)
            .Select(h => new HogarInfo(h.Id, h.Nombre))
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct)
    {
        var hogar = await _db.Hogares.FindAsync([hogarId], ct);
        if (hogar is null) return;

        hogar.Nombre = nombre;
        await _db.SaveChangesAsync(ct);
    }
}
