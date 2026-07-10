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
                CatalogoId = equipment.CatalogoId,
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

    public async Task<IReadOnlyList<Guid>> GetUserRestriccionesAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return await _dbContext.RestriccionesUsuarios
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .Select(x => x.RestriccionId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetHogarMetasAsync(Guid hogarId, CancellationToken cancellationToken)
    {
        return await _dbContext.HogarMetas
            .AsNoTracking()
            .Where(x => x.HogarId == hogarId)
            .Select(x => x.MetaId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TutorialUsuarioResult> GetTutorialUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var state = await GetOrCreateTutorialUsuarioAsync(usuarioId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToTutorialUsuarioResult(state);
    }

    public async Task<TutorialUsuarioResult> MarkTutorialCompletedAsync(Guid usuarioId, string module, CancellationToken cancellationToken)
    {
        var state = await GetOrCreateTutorialUsuarioAsync(usuarioId, cancellationToken);

        switch (NormalizeModule(module))
        {
            case "home": state.HomeCompletado = true; break;
            case "alacena": state.AlacenaCompletado = true; break;
            case "recetas": state.RecetasCompletado = true; break;
            case "lista-compras": state.ListaComprasCompletado = true; break;
            case "electrodomesticos": state.ElectrodomesticosCompletado = true; break;
            case "finanzas": state.FinanzasCompletado = true; break;
            case "planificador": state.PlanificadorCompletado = true; break;
            case "tareas": state.TareasCompletado = true; break;
            case "notificaciones": state.NotificacionesCompletado = true; break;
            case "perfil": state.PerfilCompletado = true; break;
            case "configuracion": state.ConfiguracionCompletado = true; break;
            default: throw new ArgumentOutOfRangeException(nameof(module), "Modulo de tutorial no soportado.");
        }

        state.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToTutorialUsuarioResult(state);
    }

    private async Task<TutorialUsuario> GetOrCreateTutorialUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.TutorialUsuarios
            .SingleOrDefaultAsync(x => x.UsuarioId == usuarioId, cancellationToken);

        if (state is not null)
        {
            return state;
        }

        var now = DateTime.UtcNow;
        state = new TutorialUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.TutorialUsuarios.Add(state);
        return state;
    }

    private static string NormalizeModule(string module)
    {
        return module.Trim().ToLowerInvariant().Replace("_", "-");
    }

    private static TutorialUsuarioResult ToTutorialUsuarioResult(TutorialUsuario state)
    {
        return new TutorialUsuarioResult(
            state.Id,
            state.UsuarioId,
            state.HomeCompletado,
            state.AlacenaCompletado,
            state.RecetasCompletado,
            state.ListaComprasCompletado,
            state.ElectrodomesticosCompletado,
            state.FinanzasCompletado,
            state.PlanificadorCompletado,
            state.TareasCompletado,
            state.NotificacionesCompletado,
            state.PerfilCompletado,
            state.ConfiguracionCompletado);
    }
}
