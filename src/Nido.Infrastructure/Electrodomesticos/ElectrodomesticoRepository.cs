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
            Nombre = electrodomestico.Nombre,
            Tipo = electrodomestico.Tipo,
            Estado = electrodomestico.Estado
        };

        _dbContext.Electrodomesticos.Add(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainElectrodomestico>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Electrodomesticos
            .AsNoTracking()
            .Select(electrodomestico => new DomainElectrodomestico(
                electrodomestico.Id,
                electrodomestico.HogarId,
                electrodomestico.Nombre,
                electrodomestico.Tipo,
                electrodomestico.Estado,
                electrodomestico.Marca,
                electrodomestico.ImagenUrl
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainElectrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken)
    {
        return await _dbContext.Electrodomesticos
            .AsNoTracking()
            .Where(electrodomestico => electrodomestico.HogarId == hogarId)
            .Select(electrodomestico => new DomainElectrodomestico(
                electrodomestico.Id,
                electrodomestico.HogarId,
                electrodomestico.Nombre,
                electrodomestico.Tipo,
                electrodomestico.Estado,
                electrodomestico.Marca,
                electrodomestico.ImagenUrl
            ))
            .ToListAsync(cancellationToken);
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
