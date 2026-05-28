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

    public async Task ReplaceRestrictionsAsync(Guid usuarioId, IReadOnlyList<RestrictionInput> restrictions, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.RestriccionesUsuarios.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        _dbContext.RestriccionesUsuarios.RemoveRange(existing);
        foreach (var restriction in restrictions)
        {
            _dbContext.RestriccionesUsuarios.Add(new RestriccionesUsuario
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Tipo = restriction.Tipo,
                Descripcion = restriction.Descripcion
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceGoalsAsync(Guid hogarId, IReadOnlyList<HouseholdGoalInput> goals, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.OnboardingGoals.Where(x => x.HogarId == hogarId).ToListAsync(cancellationToken);
        _dbContext.OnboardingGoals.RemoveRange(existing);
        foreach (var goal in goals)
        {
            _dbContext.OnboardingGoals.Add(new OnboardingGoal
            {
                Id = Guid.NewGuid(),
                HogarId = hogarId,
                Titulo = goal.Titulo,
                Descripcion = goal.Descripcion
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
