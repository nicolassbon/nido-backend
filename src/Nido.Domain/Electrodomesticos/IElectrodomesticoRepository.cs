namespace Nido.Domain.Electrodomesticos;

public interface IElectrodomesticoRepository
{
    Task<bool> HogarExisteAsync(Guid hogarId, CancellationToken cancellationToken);

    Task SaveAsync(Electrodomestico electrodomestico, CancellationToken cancellationToken);
    Task<IReadOnlyList<Electrodomestico>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Electrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ElectrodomesticoCatalogo>> GetCatalogoAsync(CancellationToken cancellationToken);
    Task<Electrodomestico?> UpdateAsync(Guid id, Guid hogarId, string? tipo, string? estado, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken cancellationToken);

}
