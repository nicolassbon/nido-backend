namespace Nido.Application.Common.Security;

public interface IHogarMembershipRepository
{
    Task<bool> IsOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task<bool> IsMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task<bool> IsInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct);
}
