using Nido.Application.Common.Security;

namespace Nido.Application.Tests.Common.Security;

internal sealed class RecordingHouseholdMembershipService : IHouseholdMembershipService
{
    public Exception? OwnerExceptionToThrow { get; set; }
    public Exception? MemberExceptionToThrow { get; set; }
    public Exception? AnyMembershipExceptionToThrow { get; set; }
    public List<(Guid UsuarioId, Guid HogarId)> OwnerChecks { get; } = [];
    public List<(Guid UsuarioId, Guid HogarId)> MemberChecks { get; } = [];
    public List<Guid> AnyMembershipChecks { get; } = [];

    public Task EnsureOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
    {
        OwnerChecks.Add((usuarioId, hogarId));
        return OwnerExceptionToThrow is null ? Task.CompletedTask : Task.FromException(OwnerExceptionToThrow);
    }

    public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
    {
        MemberChecks.Add((usuarioId, hogarId));
        return MemberExceptionToThrow is null ? Task.CompletedTask : Task.FromException(MemberExceptionToThrow);
    }

    public Task EnsureMemberAsync(Guid usuarioId, Guid hogarId, Func<Exception> deniedFactory, CancellationToken ct)
    {
        MemberChecks.Add((usuarioId, hogarId));
        return MemberExceptionToThrow is null ? Task.CompletedTask : Task.FromException(MemberExceptionToThrow);
    }

    public Task EnsureAnyMembershipAsync(Guid usuarioId, CancellationToken ct)
    {
        AnyMembershipChecks.Add(usuarioId);
        return AnyMembershipExceptionToThrow is null ? Task.CompletedTask : Task.FromException(AnyMembershipExceptionToThrow);
    }
}

internal sealed class FakeHogarMembershipRepository : IHogarMembershipRepository
{
    public bool IsOwner { get; set; } = true;

    public Task<bool> IsOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(IsOwner);
    public Task<bool> IsMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> IsInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct) => Task.FromResult(true);
}
