using Nido.Application.Onboarding;

namespace Nido.Application.Tests.Onboarding;

internal sealed class RecordingOnboardingRepository : IOnboardingRepository
{
    public Guid UsuarioId { get; } = Guid.NewGuid();
    public Guid HogarId { get; } = Guid.NewGuid();
    public List<Guid> RestriccionesGuardadas { get; } = [];
    public List<Guid> MetasGuardadas { get; } = [];
    public List<RepresentedMemberInput> MembersGuardados { get; } = [];
    public List<EquipmentInput> EquipmentsGuardados { get; } = [];
    public List<(int StepNumber, bool Skipped)> MarkedSteps { get; } = [];

    public Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken cancellationToken)
    {
        MembersGuardados.Clear();
        MembersGuardados.AddRange(members);
        return Task.CompletedTask;
    }

    public Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken cancellationToken)
    {
        EquipmentsGuardados.Clear();
        EquipmentsGuardados.AddRange(equipments);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RestriccionCatalogoResult>> GetRestriccionesCatalogoAsync(string? tipo, string? search, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RestriccionCatalogoResult>>([]);

    public Task<IReadOnlyList<MetaCatalogoResult>> GetMetasCatalogoAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MetaCatalogoResult>>([]);

    public Task ReplaceUserRestriccionesAsync(Guid usuarioId, IReadOnlyList<Guid> restriccionIds, CancellationToken cancellationToken)
    {
        RestriccionesGuardadas.Clear();
        RestriccionesGuardadas.AddRange(restriccionIds);
        return Task.CompletedTask;
    }

    public Task ReplaceHogarMetasAsync(Guid hogarId, IReadOnlyList<Guid> metaIds, CancellationToken cancellationToken)
    {
        MetasGuardadas.Clear();
        MetasGuardadas.AddRange(metaIds);
        return Task.CompletedTask;
    }

    public Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken cancellationToken)
    {
        MarkedSteps.Add((stepNumber, skipped));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetUserRestriccionesAsync(Guid usuarioId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Guid>>(RestriccionesGuardadas);

    public Task<IReadOnlyList<Guid>> GetHogarMetasAsync(Guid hogarId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Guid>>(MetasGuardadas);
}
