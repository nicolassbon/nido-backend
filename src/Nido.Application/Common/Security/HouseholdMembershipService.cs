using Nido.Application.Hogares.Exceptions;

namespace Nido.Application.Common.Security;

public sealed class HouseholdMembershipService(IHogarMembershipRepository membershipRepository) : IHouseholdMembershipService
{
    public async Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
    {
        if (!await membershipRepository.IsOwnerAsync(usuarioId, hogarId, ct))
        {
            throw new NotHouseholdOwnerException();
        }
    }

    public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => EnsureMemberAsync(usuarioId, hogarId, static () => new NotHouseholdMemberException(), ct);

    public async Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct)
    {
        if (!await membershipRepository.IsMemberAsync(usuarioId, hogarId, ct))
        {
            throw deniedFactory();
        }
    }

    public async Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct)
    {
        if (!await membershipRepository.IsInAnyHouseholdAsync(usuarioId, ct))
        {
            throw new NotHouseholdMemberException();
        }
    }
}
