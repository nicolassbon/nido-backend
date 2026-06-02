using Microsoft.EntityFrameworkCore;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using DomainElectrodomesticoCatalogo = Nido.Domain.Electrodomesticos.ElectrodomesticoCatalogo;


using DomainElectrodomestico = Nido.Domain.Electrodomesticos.Electrodomestico;
using PersistenceElectrodomestico = Nido.Infrastructure.Persistence.Entities.Electrodomestico;

namespace Nido.Infrastructure.Electrodomesticos;

public sealed class ElectrodomesticoRepository : IElectrodomesticoRepository
{
    private readonly NidoDbContext _dbContext;

    public ElectrodomesticoRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HogarExisteAsync(Guid hogarId, CancellationToken cancellationToken)
    {
        return await _dbContext.Hogares
            .AnyAsync(hogar => hogar.Id == hogarId, cancellationToken);
    }

    public async Task SaveAsync(DomainElectrodomestico electrodomestico, CancellationToken cancellationToken)
    {
        var entity = new PersistenceElectrodomestico
        {
            Id = electrodomestico.Id,
            HogarId = electrodomestico.HogarId,
            CatalogoId = electrodomestico.CatalogoId,
            Nombre = electrodomestico.Nombre,
            Tipo = electrodomestico.Tipo,
            Estado = electrodomestico.Estado,
            Marca = electrodomestico.Marca,
            ImagenUrl = electrodomestico.ImagenUrl
        };

        _dbContext.Electrodomesticos.Add(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainElectrodomestico>> GetAllAsync(CancellationToken cancellationToken)
    {
        var electrodomesticos = await _dbContext.Electrodomesticos
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var catalogo = await _dbContext.ElectrodomesticosCatalogo
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return MapToDomain(electrodomesticos, catalogo);
    }

    public async Task<IReadOnlyList<DomainElectrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken)
    {
        var electrodomesticos = await _dbContext.Electrodomesticos
            .AsNoTracking()
            .Where(e => e.HogarId == hogarId)
            .ToListAsync(cancellationToken);

        var catalogo = await _dbContext.ElectrodomesticosCatalogo
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return MapToDomain(electrodomesticos, catalogo);
    }

    private static IReadOnlyList<DomainElectrodomestico> MapToDomain(
        List<PersistenceElectrodomestico> electrodomesticos,
        List<Persistence.Entities.ElectrodomesticoCatalogo> catalogo)
    {
        var catalogoById = catalogo.ToDictionary(c => c.Id);
        var catalogoByNombre = catalogo
            .GroupBy(c => c.Nombre)
            .ToDictionary(g => g.Key, g => g.First());

        return electrodomesticos
            .Select(e => new DomainElectrodomestico(
                e.Id,
                e.HogarId,
                e.CatalogoId,
                e.Nombre,
                e.Tipo,
                e.Estado,
                e.Marca,
                e.ImagenUrl
                    ?? (e.CatalogoId.HasValue && catalogoById.TryGetValue(e.CatalogoId.Value, out var byId)
                        ? byId.ImagenUrl
                        : null)
                    ?? (catalogoByNombre.TryGetValue(e.Nombre, out var byNombre)
                        ? byNombre.ImagenUrl
                        : null)))
            .ToList();
    }

public async Task<IReadOnlyList<DomainElectrodomesticoCatalogo>> GetCatalogoAsync(
    CancellationToken cancellationToken)
{
    return await _dbContext.ElectrodomesticosCatalogo
        .AsNoTracking()
        .Where(x => x.Activo)
        .OrderBy(x => x.Orden)
        .Select(x => new DomainElectrodomesticoCatalogo(
            x.Id,
            x.Nombre,
            x.Tipo,
            x.Icono,
            x.ImagenUrl,
            x.Orden,
            x.Activo))
        .ToListAsync(cancellationToken);
}
}
