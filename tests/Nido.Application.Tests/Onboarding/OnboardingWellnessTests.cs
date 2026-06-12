using Nido.Application.Onboarding;
using Nido.Application.Onboarding.Exceptions;

namespace Nido.Application.Tests.Onboarding;

public sealed class OnboardingWellnessTests
{
    [Fact]
    public async Task SaveWellness_CuandoSeEnvianRestriccionesYMetas_GuardaAmbosEnElRepositorio()
    {
        var repo = new RecordingOnboardingRepository();
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
        var repo = new RecordingOnboardingRepository();
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
        var repo = new RecordingOnboardingRepository();
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
        var repo = new RecordingOnboardingRepository();
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
        var repo = new RecordingOnboardingRepository();
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
        Assert.Equal([(4, true)], repo.MarkedSteps);
    }
}
