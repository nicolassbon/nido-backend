using Nido.Application.Preferencias;

namespace Nido.Application.Tests.Preferencias;

public sealed class PreferenciasHandlersTests
{
    [Fact]
    public async Task GetPreferencias_WithValidUsuarioId_ReturnsDiasAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeUserPreferencesRepository { Result = new UserPreferencesResult(14) };
        var handler = new GetUserPreferencesHandler(repo);

        var result = await handler.Handle(new GetUserPreferencesQuery(usuarioId), CancellationToken.None);

        Assert.Equal(14, result.DiasAlerta);
    }

    [Fact]
    public async Task GetPreferencias_WithEmptyUsuarioId_ThrowsArgumentException()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new GetUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new GetUserPreferencesQuery(Guid.Empty), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithValidDias_ReturnsUpdatedDiasAlerta()
    {
        var usuarioId = Guid.NewGuid();
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        var result = await handler.Handle(new UpdateUserPreferencesCommand(usuarioId, 30), CancellationToken.None);

        Assert.Equal(30, result.DiasAlerta);
    }

    [Fact]
    public async Task UpdatePreferencias_WithEmptyUsuarioId_ThrowsArgumentException()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.Empty, 7), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithZeroDias_ThrowsArgumentException()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 0), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_WithDiasSobreLimiteMaximo_ThrowsArgumentException()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 366), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePreferencias_ConLimitesValidos_NoLanzaExcepcion()
    {
        var repo = new FakeUserPreferencesRepository();
        var handler = new UpdateUserPreferencesHandler(repo);

        var min = await handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 1), CancellationToken.None);
        var max = await handler.Handle(new UpdateUserPreferencesCommand(Guid.NewGuid(), 365), CancellationToken.None);

        Assert.Equal(1, min.DiasAlerta);
        Assert.Equal(365, max.DiasAlerta);
    }

    private sealed class FakeUserPreferencesRepository : IUserPreferencesRepository
    {
        public UserPreferencesResult Result { get; set; } = new UserPreferencesResult(7);

        public Task<UserPreferencesResult> GetByUsuarioAsync(Guid usuarioId, CancellationToken ct)
            => Task.FromResult(Result);

        public Task<UserPreferencesResult> UpdateAsync(Guid usuarioId, int diasAlerta, CancellationToken ct)
            => Task.FromResult(new UserPreferencesResult(diasAlerta));
    }
}
