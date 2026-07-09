using Microsoft.EntityFrameworkCore;
using Nido.Application.CatalogoElectrodomesticos.UploadCatalogImage;
using Nido.Application.Common.Assets;
using Nido.Application.Electrodomesticos.UploadElectrodomesticoImage;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using DomainElectrodomesticoCatalogo = Nido.Domain.Electrodomesticos.ElectrodomesticoCatalogo;


using DomainElectrodomestico = Nido.Domain.Electrodomesticos.Electrodomestico;
using PersistenceElectrodomestico = Nido.Infrastructure.Persistence.Entities.Electrodomestico;

namespace Nido.Infrastructure.Electrodomesticos;

public sealed class ElectrodomesticoRepository : IElectrodomesticoRepository, IElectrodomesticoImageRepository, ICatalogImageRepository
{
    private readonly NidoDbContext _dbContext;
    private readonly IPublicAssetUrlResolver _assetUrlResolver;

    public ElectrodomesticoRepository(NidoDbContext dbContext, IPublicAssetUrlResolver assetUrlResolver)
    {
        _dbContext = dbContext;
        _assetUrlResolver = assetUrlResolver;
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

    public async Task<DomainElectrodomestico?> UpdateAsync(Guid id, Guid hogarId, string? tipo, string? estado, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Electrodomesticos
            .FirstOrDefaultAsync(e => e.Id == id && e.HogarId == hogarId, cancellationToken);

        if (entity is null) return null;

        if (tipo is not null) entity.Tipo = tipo;
        if (estado is not null) entity.Estado = estado;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var catalogo = await _dbContext.ElectrodomesticosCatalogo.AsNoTracking().ToListAsync(cancellationToken);
        return MapToDomain([entity], catalogo).First();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Electrodomesticos
            .FirstOrDefaultAsync(e => e.Id == id && e.HogarId == hogarId, cancellationToken);

        if (entity is null) return false;

        _dbContext.Electrodomesticos.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IReadOnlyList<DomainElectrodomestico> MapToDomain(
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
                _assetUrlResolver.Resolve(e.ImagenUrl)
                    ?? (e.CatalogoId.HasValue && catalogoById.TryGetValue(e.CatalogoId.Value, out var byId)
                        ? _assetUrlResolver.Resolve(byId.ImagenUrl)
                        : null)
                    ?? (catalogoByNombre.TryGetValue(e.Nombre, out var byNombre)
                        ? _assetUrlResolver.Resolve(byNombre.ImagenUrl)
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
                _assetUrlResolver.Resolve(x.ImagenUrl),
                x.Orden,
                x.Activo))
            .ToListAsync(cancellationToken);
    }

    async Task<ElectrodomesticoImageTarget?> IElectrodomesticoImageRepository.GetImageTargetAsync(Guid electrodomesticoId, Guid hogarId, CancellationToken cancellationToken)
        => await _dbContext.Electrodomesticos
            .AsNoTracking()
            .Where(x => x.Id == electrodomesticoId && x.HogarId == hogarId)
            .Select(x => new ElectrodomesticoImageTarget(x.Id, x.ImagenUrl))
            .FirstOrDefaultAsync(cancellationToken);

    async Task IElectrodomesticoImageRepository.UpdateImageKeyAsync(Guid electrodomesticoId, Guid hogarId, string storageKey, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Electrodomesticos.FirstOrDefaultAsync(x => x.Id == electrodomesticoId && x.HogarId == hogarId, cancellationToken)
            ?? throw new ElectrodomesticoImageTargetNotFoundException();
        entity.ImagenUrl = storageKey;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    async Task<CatalogImageTarget?> ICatalogImageRepository.GetImageTargetAsync(Guid catalogItemId, CancellationToken cancellationToken)
        => await _dbContext.ElectrodomesticosCatalogo
            .AsNoTracking()
            .Where(x => x.Id == catalogItemId)
            .Select(x => new CatalogImageTarget(x.Id, x.ImagenUrl))
            .FirstOrDefaultAsync(cancellationToken);

    async Task ICatalogImageRepository.UpdateImageKeyAsync(Guid catalogItemId, string storageKey, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ElectrodomesticosCatalogo.FirstOrDefaultAsync(x => x.Id == catalogItemId, cancellationToken)
            ?? throw new CatalogImageTargetNotFoundException();
        entity.ImagenUrl = storageKey;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
