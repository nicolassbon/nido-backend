using Microsoft.EntityFrameworkCore;
using Nido.Application.Alacena;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Alacena;

public sealed class CatalogoRepository
{
    private readonly NidoDbContext _db;

    public CatalogoRepository(NidoDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoriaResult>> GetCategoriasAsync(CancellationToken ct)
        => await _db.CategoriasProductos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaResult(c.Id, c.Nombre, c.TtlDias))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UnidadMedidaResult>> GetUnidadesMedidaAsync(CancellationToken ct)
        => await _db.UnidadesMedida
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new UnidadMedidaResult(u.Id, u.Codigo, u.Nombre))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UbicacionResult>> GetUbicacionesAsync(CancellationToken ct)
        => await _db.UbicacionesCatalogo
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new UbicacionResult(u.Id, u.Nombre, u.Icono, u.Color))
            .ToListAsync(ct);
}
