using Microsoft.EntityFrameworkCore;
using Nido.Application.Alacena;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Alacena;

public sealed class CatalogoRepository
{
    private readonly NidoDbContext _db;

    public CatalogoRepository(NidoDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoriaResult>> GetCategoriasAsync(CancellationToken ct)
    {
        var categorias = await _db.CategoriasProductos
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaResult(c.Id, c.Nombre, c.TtlDias))
            .ToListAsync(ct);

        return categorias
            .GroupBy(c => NormalizeCatalogName(c.Nombre))
            .Select(group => group.First())
            .OrderBy(c => c.Nombre)
            .ToList();
    }

    public async Task<IReadOnlyList<UnidadMedidaResult>> GetUnidadesMedidaAsync(CancellationToken ct)
    {
        var unidades = await _db.UnidadesMedida
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new UnidadMedidaResult(u.Id, u.Codigo, u.Nombre))
            .ToListAsync(ct);

        return unidades
            .GroupBy(u => NormalizeCatalogName(u.Codigo))
            .Select(group => group.First())
            .OrderBy(u => u.Nombre)
            .ToList();
    }

    public async Task<IReadOnlyList<UbicacionResult>> GetUbicacionesAsync(CancellationToken ct)
    {
        var ubicaciones = await _db.UbicacionesCatalogo
            .AsNoTracking()
            .OrderBy(u => u.Nombre)
            .Select(u => new UbicacionResult(u.Id, u.Nombre, u.Icono, u.Color))
            .ToListAsync(ct);

        return ubicaciones
            .GroupBy(u => NormalizeCatalogName(u.Nombre))
            .Select(group => group.First())
            .OrderBy(u => u.Nombre)
            .ToList();
    }

    private static string NormalizeCatalogName(string value)
        => value.Trim().ToLowerInvariant();
}
