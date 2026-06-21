namespace Nido.Application.Hogares;

public record InvitacionInfo(Guid HogarId, string HogarNombre, string? EmailInvitado, string? Estado, DateTime? ExpiraEn);

public record MiembroInfo(
    Guid UsuarioId,
    string Nombre,
    string? Email,
    string? Rol,
    string? FotoUrl,
    IReadOnlyList<string> Alergias);

public interface IInvitacionRepository
{
    Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct);
    Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct);
    Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct);
    Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct);
    Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task AddUserToHouseholdAsync(Guid usuarioId, Guid toHogarId, string token, CancellationToken ct);
    Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct);
    Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct);
    Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct);
}
