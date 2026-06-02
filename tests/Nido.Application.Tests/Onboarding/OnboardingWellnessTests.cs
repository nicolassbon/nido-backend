using Nido.Application.Onboarding;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Tests.Onboarding;

public sealed class OnboardingWellnessTests
{


    [Fact]
    public async Task SaveWellness_CuandoSeEnvianRestriccionesYMetas_GuardaAmbosEnElRepositorio()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);
        var restriccionId = Guid.NewGuid();
        var metaId = Guid.NewGuid();

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            RestriccionIds: [restriccionId],
            MetaIds: [metaId],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Equal([restriccionId], repo.RestriccionesGuardadas);
        Assert.Equal([metaId], repo.MetasGuardadas);
    }

    [Fact]
    public async Task SaveWellness_CuandoSeMandanIdsForjadosPorElCliente_LanzaBoundaryViolation()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await Assert.ThrowsAsync<BoundaryViolationException>(() => handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            RestriccionIds: [],
            MetaIds: [],
            ClientUsuarioId: Guid.NewGuid(), // distinto al UsuarioId real → intento de forja
            ClientHogarId: null), CancellationToken.None));
    }

    [Fact]
    public async Task SaveWellness_CuandoSoloSeEnvianRestricciones_GuardaSoloRestricciones()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);
        var restriccionId = Guid.NewGuid();

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            RestriccionIds: [restriccionId],
            MetaIds: [],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Equal([restriccionId], repo.RestriccionesGuardadas);
        Assert.Empty(repo.MetasGuardadas);
    }

    [Fact]
    public async Task SaveWellness_CuandoSoloSeEnvianMetas_GuardaSoloMetas()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);
        var metaId = Guid.NewGuid();

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: false,
            RestriccionIds: [],
            MetaIds: [metaId],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Empty(repo.RestriccionesGuardadas);
        Assert.Equal([metaId], repo.MetasGuardadas);
    }

    [Fact]
    public async Task SaveWellness_CuandoSeSkipea_NoGuardaNadaYMarcaPasoComoSalteado()
    {
        var repo = new FakeOnboardingRepository();
        var handler = new SaveWellnessStepHandler(repo);

        await handler.Handle(new SaveWellnessStepCommand(
            repo.UsuarioId,
            repo.HogarId,
            Skip: true,
            RestriccionIds: [],
            MetaIds: [],
            ClientUsuarioId: null,
            ClientHogarId: null), CancellationToken.None);

        Assert.Empty(repo.RestriccionesGuardadas);
        Assert.Empty(repo.MetasGuardadas);
        Assert.True(repo.PasoMarcadoComoSalteado);
    }

    // ── Test fakes

    private sealed class FakeOnboardingRepository : IOnboardingRepository
    {
        public Guid UsuarioId { get; } = Guid.NewGuid();
        public Guid HogarId { get; } = Guid.NewGuid();
        public List<Guid> RestriccionesGuardadas { get; } = [];
        public List<Guid> MetasGuardadas { get; } = [];
        public bool PasoMarcadoComoSalteado { get; private set; }

        public Task<bool> IsUserHouseholdMemberAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(true);

        public Task<bool> IsUserHouseholdOwnerAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(true);

        public Task ReplaceUserRestriccionesAsync(Guid usuarioId, IReadOnlyList<Guid> restriccionIds, CancellationToken ct)
        {
            RestriccionesGuardadas.AddRange(restriccionIds);
            return Task.CompletedTask;
        }

        public Task ReplaceHogarMetasAsync(Guid hogarId, IReadOnlyList<Guid> metaIds, CancellationToken ct)
        {
            MetasGuardadas.AddRange(metaIds);
            return Task.CompletedTask;
        }

        public Task MarkStepAsync(Guid usuarioId, Guid hogarId, int stepNumber, bool skipped, CancellationToken ct)
        {
            PasoMarcadoComoSalteado = skipped;
            return Task.CompletedTask;
        }

        public Task ReplaceRepresentedMembersAsync(Guid hogarId, IReadOnlyList<RepresentedMemberInput> members, CancellationToken ct)
            => Task.CompletedTask;

        public Task ReplaceHouseholdEquipmentAsync(Guid hogarId, IReadOnlyList<EquipmentInput> equipments, CancellationToken ct)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RestriccionCatalogoResult>> GetRestriccionesCatalogoAsync(string? tipo, string? search, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RestriccionCatalogoResult>>([]);

        public Task<IReadOnlyList<MetaCatalogoResult>> GetMetasCatalogoAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<MetaCatalogoResult>>([]);
    }
}
