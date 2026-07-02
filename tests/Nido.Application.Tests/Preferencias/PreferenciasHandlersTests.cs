using Nido.Application.Preferencias;
using Nido.Application.Preferencias.Exceptions;

namespace Nido.Application.Tests.Preferencias;

public sealed class PreferenciasHandlersTests
{
    [Fact]
    public async Task GetPreferencias_WithValidUsuarioId_ReturnsDiasAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeUserPreferencesRepository { Result = new UserPreferencesResult(14, UserThemeModes.Dark) };
        var handler = new GetUserPreferencesHandler(repo);

        var result = await handler.Handle(new GetUserPreferencesQuery(usuarioId), CancellationToken.None);

        Assert.Equal(14, result.DiasAlerta);
        Assert.Equal(UserThemeModes.Dark, result.TemaPreferido);
    }

    [Fact]
    public async Task GetPreferencias_WithEmptyUsuarioId_ThrowsMissingPreferenceField()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new GetUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<MissingPreferenceFieldException>(() =>
            handler.Handle(new GetUserPreferencesQuery(Guid.Empty), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithValidDias_ReturnsUpdatedDiasAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        var result = await handler.Handle(new UpdateUserPreferencesCommand(usuarioId, 30, null), CancellationToken.None);

        Assert.Equal(30, result.DiasAlerta);
        Assert.Equal(UserThemeModes.System, result.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_WithValidTema_ReturnsUpdatedTemaPreferido()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        var result = await handler.Handle(new UpdateUserPreferencesCommand(usuarioId, null, UserThemeModes.Dark), CancellationToken.None);

        Assert.Equal(7, result.DiasAlerta);
        Assert.Equal(UserThemeModes.Dark, result.TemaPreferido);
    }

    [Fact]
    public async Task UpdatePreferencias_WithEmptyUsuarioId_ThrowsMissingPreferenceField()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<MissingPreferenceFieldException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.Empty, 7, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithZeroDias_ThrowsInvalidPreferenceRange()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<InvalidPreferenceRangeException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 0, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithDiasSobreLimiteMaximo_ThrowsInvalidPreferenceRange()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<InvalidPreferenceRangeException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 366, null), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithInvalidTema_ThrowsInvalidThemeMode()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<InvalidThemeModeException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), null, "sepia"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_ConLimitesValidos_NoLanzaExcepcion()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        var min = await handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 1, null), CancellationToken.None);
        var max = await handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 365, null), CancellationToken.None);

        Assert.Equal(1, min.DiasAlerta);
        Assert.Equal(365, max.DiasAlerta);
    }

    private sealed class FakeUserPreferencesRepository : IUserPreferencesRepository
    {
        public UserPreferencesResult Result { get; set; } = new UserPreferencesResult(7, UserThemeModes.System);

        public Task<UserPreferencesResult> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(Result);

        public Task<UserPreferencesResult> UpdateAsync(Guid usuarioId, int? diasAlerta, string? temaPreferido, CancellationToken ct)
            => Task.FromResult(new UserPreferencesResult(
                diasAlerta ?? Result.DiasAlerta,
                temaPreferido ?? Result.TemaPreferido));
    }
}
