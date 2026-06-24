using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.Security;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Hogares;

public sealed class HogarMembershipRepository(NidoDbContext dbContext) : IHogarMembershipRepository
{
    public Task<bool> IsOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => dbContext.MiembrosHogars.AnyAsync(
            member => member.UsuarioId == usuarioId
                && member.HogarId == hogarId
                && member.NombreRepresentado == null
                && member.Rol == "owner",
            ct);

    public Task<bool> IsMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => dbContext.MiembrosHogars.AnyAsync(
            member => member.UsuarioId == usuarioId
                && member.HogarId == hogarId
                && member.NombreRepresentado == null,
            ct);

    public Task<bool> IsInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct)
        => dbContext.MiembrosHogars.AnyAsync(
            member => member.UsuarioId == usuarioId
                && member.NombreRepresentado == null,
            ct);
}
