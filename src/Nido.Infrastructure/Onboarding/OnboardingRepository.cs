using Microsoft.EntityFrameworkCore;
using Nido.Application.Onboarding;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Onboarding;

public sealed class OnboardingRepository : IOnboardingRepository
{
    private readonly NidoDbContext _dbContext;

    public OnboardingRepository(NidoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> IsUserHouseholdMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken)
        => _dbContext.MiembrosHogars.AnyAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId, cancellationToken);

    public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken cancellationToken)
        => _dbContext.MiembrosHogars.AnyAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId && x.Rol == "owner", cancellationToken);

    public async Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MiembrosHogars.Where(x => x.HogarId == hogarId && x.NombreRepresentado != null).ToListAsync(cancellationToken);
        _dbContext.MiembrosHogars.RemoveRange(existing);

        var ownerUserId = await _dbContext.MiembrosHogars
            .Where(x => x.HogarId == hogarId && x.Rol == "owner")
            .Select(x => x.UsuarioId)
            .FirstAsync(cancellationToken);

        foreach (var member in members)
        {
            _dbContext.MiembrosHogars.Add(new MiembrosHogar
            {
                Id = Guid.NewGuid(),
                HogarId = hogarId,
                UsuarioId = ownerUserId,
                Rol = string.IsNullOrWhiteSpace(member.Rol) ? member.Nombre : member.Rol,
                NombreRepresentado = member.Nombre,
                Puntos = 0
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Electrodomesticos.Where(x => x.HogarId == hogarId).ToListAsync(cancellationToken);
        _dbContext.Electrodomesticos.RemoveRange(existing);
        foreach (var equipment in equipments)
        {
            _dbContext.Electrodomesticos.Add(new Electrodomestico
            {
                Id = Guid.NewGuid(),
                HogarId = hogarId,
                Nombre = equipment.Nombre,
                Tipo = equipment.Tipo,
                Estado = equipment.Estado
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RestriccionCatalogoResult>> GetRestriccionesCatalogoAsync(string? tipo, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.RestriccionesCatalogo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(x => x.Tipo == tipo);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Nombre.ToLower().Contains(search.ToLower()));

        var results = await query
            .OrderBy(x => x.Nombre)
            .Select(x => new RestriccionCatalogoResult(x.Id, x.Nombre, x.Tipo))
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<MetaCatalogoResult>> GetMetasCatalogoAsync(CancellationToken cancellationToken)
    {
        var results = await _dbContext.MetasCatalogo
            .AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new MetaCatalogoResult(x.Id, x.Nombre))
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task ReplaceUserRestriccionesAsync(Guid usuarioId, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RestriccionesUsuarios.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        _dbContext.RestriccionesUsuarios.RemoveRange(existing);

        foreach (var restriccionId in restriccionIds)
        {
            _dbContext.RestriccionesUsuarios.Add(new RestriccionesUsuario
            {
                UsuarioId = usuarioId,
                RestriccionId = restriccionId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceHogarMetasAsync(Guid hogarId, IReadOnlyList<Guid> metaIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.HogarMetas.Where(x => x.HogarId == hogarId).ToListAsync(cancellationToken);
        _dbContext.HogarMetas.RemoveRange(existing);

        foreach (var metaId in metaIds)
        {
            _dbContext.HogarMetas.Add(new HogarMeta
            {
                HogarId = hogarId,
                MetaId = metaId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken cancellationToken)
    {
        var state = await _dbContext.OnboardingStates
            .SingleOrDefaultAsync(x => x.UsuarioId == usuarioId && x.HogarId == hogarId, cancellationToken);

        state ??= new OnboardingState { Id = Guid.NewGuid(), UsuarioId = usuarioId, HogarId = hogarId };

        var now = DateTime.UtcNow;
        if (stepNumber == 2) { state.Step2CompletedAt = now; state.Step2Skipped = skipped; }
        if (stepNumber == 3) { state.Step3CompletedAt = now; state.Step3Skipped = skipped; }
        if (stepNumber == 4) { state.Step4CompletedAt = now; state.Step4Skipped = skipped; }
        state.UpdatedAt = now;

        if (_dbContext.Entry(state).State == EntityState.Detached)
        {
            _dbContext.OnboardingStates.Add(state);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
