using Microsoft.EntityFrameworkCore;
using Nido.Domain.Electrodomesticos;
using Nido.Infrastructure.Persistence;

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
        return await _dbContext.Households
            .AnyAsync(hogar => hogar.Id == hogarId, cancellationToken);
    }

    public async Task SaveAsync(Electrodomestico electrodomestico, CancellationToken cancellationToken)
    {
        _dbContext.Electrodomesticos.Add(electrodomestico);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Electrodomestico>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Electrodomesticos
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Electrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken)
    {
        return await _dbContext.Electrodomesticos
            .Where(e => e.HogarId == hogarId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}