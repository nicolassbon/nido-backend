namespace Nido.Application.Hogares;

public sealed record HogarInfo(Guid Id, string Nombre);
public sealed record HogarConRolInfo(Guid Id, string Nombre, string Rol);

public interface IHogarRepository
{
    Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct);
    Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct);
    Task<HogarInfo> CreateHogarAsync(Guid usuarioId, string nombre, CancellationToken ct);
    Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid usuarioId, CancellationToken ct);
    Task DeleteHogarAsync(Guid hogarId, CancellationToken ct);
}
