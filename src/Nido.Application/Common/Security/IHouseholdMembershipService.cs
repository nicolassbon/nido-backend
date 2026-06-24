namespace Nido.Application.Common.Security;

public interface IHouseholdMembershipService
{
    Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct);
    Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct);
    Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct);
}
