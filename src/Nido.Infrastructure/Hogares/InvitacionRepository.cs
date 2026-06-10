using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.ProfileImages;
using Nido.Application.Hogares;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Hogares;

public sealed class InvitacionRepository : IInvitacionRepository
{
    private readonly NidoDbContext _db;
    private readonly IProfileImagePublicUrlResolver _profileImagePublicUrlResolver;

    public InvitacionRepository(NidoDbContext db, IProfileImagePublicUrlResolver profileImagePublicUrlResolver)
    {
        _db = db;
        _profileImagePublicUrlResolver = profileImagePublicUrlResolver;
    }

    public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => _db.MiembrosHogars.AnyAsync(m => m.UsuarioId == usuarioId && m.HogarId == hogarId && m.Rol == "owner", ct);

    public Task<int> CountRealMembersAsync(Guid hogarId, CancellationToken ct)
        => _db.MiembrosHogars.CountAsync(m => m.HogarId == hogarId && m.NombreRepresentado == null, ct);

    public async Task<string> CreateInvitacionAsync(Guid hogarId, Guid invitadoPor, string emailInvitado, DateTime expiresAt, CancellationToken ct)
    {
        var token = Guid.NewGuid().ToString("N");

        _db.InvitacionesHogars.Add(new InvitacionesHogar
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            InvitadoPor = invitadoPor,
            EmailInvitado = emailInvitado,
            Token = token,
            Estado = "pendiente",
            ExpiraEn = expiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<InvitacionInfo?> GetInvitacionByTokenAsync(string token, CancellationToken ct)
    {
        var inv = await _db.InvitacionesHogars
            .Include(i => i.Hogar)
            .FirstOrDefaultAsync(i => i.Token == token, ct);

        if (inv is null) return null;

        return new InvitacionInfo(inv.HogarId, inv.Hogar.Nombre, inv.EmailInvitado, inv.Estado, inv.ExpiraEn);
    }

    public Task<bool> IsUserInAnyHouseholdAsync(Guid usuarioId, CancellationToken ct)
        => _db.MiembrosHogars.AnyAsync(m => m.UsuarioId == usuarioId && m.NombreRepresentado == null, ct);

    public async Task<bool> IsUserSoleOwnerAsync(Guid usuarioId, CancellationToken ct)
    {
        var hogarId = await _db.MiembrosHogars
            .Where(m => m.UsuarioId == usuarioId && m.NombreRepresentado == null)
            .Select(m => m.HogarId)
            .FirstOrDefaultAsync(ct);

        if (hogarId == Guid.Empty) return true;


        var isOwner = await _db.MiembrosHogars.AnyAsync(m => m.HogarId == hogarId && m.UsuarioId == usuarioId && m.Rol == "owner", ct);
        if (!isOwner) return false;

        var realMemberCount = await _db.MiembrosHogars.CountAsync(m => m.HogarId == hogarId && m.NombreRepresentado == null, ct);
        return realMemberCount == 1;
    }

    public async Task<Guid> GetUserCurrentHogarIdAsync(Guid usuarioId, CancellationToken ct)
    {
        return await _db.MiembrosHogars
            .Where(m => m.UsuarioId == usuarioId && m.NombreRepresentado == null)
            .Select(m => m.HogarId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task MoveUserToHouseholdAsync(Guid usuarioId, Guid fromHogarId, Guid toHogarId, string token, CancellationToken ct)
    {

        var oldMemberships = await _db.MiembrosHogars
            .Where(m => m.HogarId == fromHogarId)
            .ToListAsync(ct);
        _db.MiembrosHogars.RemoveRange(oldMemberships);


        _db.MiembrosHogars.Add(new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            HogarId = toHogarId,
            Rol = "conviviente",
            Puntos = 0
        });


        var inv = await _db.InvitacionesHogars.FirstOrDefaultAsync(i => i.Token == token, ct);
        if (inv is not null)
            inv.Estado = "aceptada";


        var existingState = await _db.OnboardingStates
            .AnyAsync(s => s.UsuarioId == usuarioId && s.HogarId == toHogarId, ct);
        if (!existingState)
        {
            _db.OnboardingStates.Add(new OnboardingState
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                HogarId = toHogarId,
                Step1CompletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<MiembroInfo>> GetMiembrosAsync(Guid hogarId, CancellationToken ct)
    {
        var members = await _db.MiembrosHogars
            .Where(m => m.HogarId == hogarId && m.NombreRepresentado == null)
            .Join(_db.Usuarios,
                m => m.UsuarioId,
                u => u.Id,
                (m, u) => new { u.Id, u.Nombre, u.Email, m.Rol, u.FotoStorageKey, u.FotoUrl })
            .ToListAsync(ct);

        var userIds = members.Select(x => x.Id).ToList();
        var alergias = await _db.RestriccionesUsuarios
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UsuarioId)
                && (x.Restriccion.Tipo == "alergia" || x.Restriccion.Tipo == "restriccion_alimentaria"))
            .Select(x => new { x.UsuarioId, x.Restriccion.Nombre })
            .ToListAsync(ct);

        var alergiasByUser = alergias
            .GroupBy(x => x.UsuarioId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(x => x.Nombre).OrderBy(nombre => nombre).ToList());

        return members
            .Select(x => new MiembroInfo(
                x.Id,
                x.Nombre,
                x.Email,
                x.Rol,
                !string.IsNullOrWhiteSpace(x.FotoStorageKey)
                    ? _profileImagePublicUrlResolver.Resolve(x.FotoStorageKey)
                    : x.FotoUrl,
                alergiasByUser.GetValueOrDefault(x.Id, [])))
            .ToList();
    }

    public async Task<(string Email, string Nombre)> GetUsuarioInfoAsync(Guid usuarioId, CancellationToken ct)
    {
        var user = await _db.Usuarios
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Email, u.Nombre })
            .FirstAsync(ct);
        return (user.Email, user.Nombre);
    }

    public Task<bool> IsMemberOfHouseholdAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
        => _db.MiembrosHogars.AnyAsync(m => m.UsuarioId == usuarioId && m.HogarId == hogarId && m.NombreRepresentado == null, ct);

    public async Task RemoveMiembroAsync(Guid hogarId, Guid targetUsuarioId, CancellationToken ct)
    {
        var membership = await _db.MiembrosHogars
            .FirstOrDefaultAsync(m => m.HogarId == hogarId && m.UsuarioId == targetUsuarioId && m.NombreRepresentado == null, ct);

        if (membership is not null)
            _db.MiembrosHogars.Remove(membership);

        var userNombre = await _db.Usuarios
            .Where(u => u.Id == targetUsuarioId)
            .Select(u => u.Nombre)
            .FirstAsync(ct);

        var nuevoHogar = new Persistence.Entities.Hogare
        {
            Id = Guid.NewGuid(),
            Nombre = $"Hogar de {userNombre}",
            CreatedAt = DateTime.UtcNow
        };
        _db.Hogares.Add(nuevoHogar);
        _db.MiembrosHogars.Add(new Persistence.Entities.MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = targetUsuarioId,
            HogarId = nuevoHogar.Id,
            Rol = "owner",
            Puntos = 0
        });

        await _db.SaveChangesAsync(ct);
    }
}
