namespace Nido.Application.Hogares;

public sealed record HogarInfo(Guid Id, string Nombre);

public interface IHogarRepository
{
    Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct);

    Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct);
}
