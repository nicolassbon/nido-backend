using Nido.Application.Hogares;

namespace Nido.Application.Tests.Hogares;

public sealed class GetHogaresHandlerTests
{
    [Fact]
    public async Task Handle_RetornaHogaresDelUsuario()
    {
        var usuarioId = Guid.NewGuid();
        var esperados = new List<HogarConRolInfo>
        {
            new(Guid.NewGuid(), "Hogar principal", "owner"),
            new(Guid.NewGuid(), "Casa de verano",  "owner"),
        };
        var repo    = new FakeHogarRepository(usuarioId, esperados);
        var handler = new GetHogaresHandler(repo);

        var result = await handler.Handle(new GetHogaresQuery(usuarioId), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Hogar principal", result[0].Nombre);
        Assert.Equal("Casa de verano",  result[1].Nombre);
    }

    [Fact]
    public async Task Handle_SinHogares_RetornaListaVacia()
    {
        var repo    = new FakeHogarRepository(Guid.NewGuid(), []);
        var handler = new GetHogaresHandler(repo);

        var result = await handler.Handle(new GetHogaresQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    private sealed class FakeHogarRepository(Guid usuarioId, List<HogarConRolInfo> hogares) : IHogarRepository
    {
        public Task<List<HogarConRolInfo>> GetUserHogaresAsync(Guid uid, CancellationToken ct)
            => Task.FromResult(uid == usuarioId ? hogares : []);

        public Task<HogarInfo?> GetByIdAsync(Guid hogarId, CancellationToken ct) => Task.FromResult<HogarInfo?>(null);
        public Task UpdateNombreAsync(Guid hogarId, string nombre, CancellationToken ct) => Task.CompletedTask;
        public Task<HogarInfo> CreateHogarAsync(Guid uid, string nombre, CancellationToken ct)
            => Task.FromResult(new HogarInfo(Guid.NewGuid(), nombre));
        public Task DeleteHogarAsync(Guid hogarId, CancellationToken ct) => Task.CompletedTask;
    }
}
