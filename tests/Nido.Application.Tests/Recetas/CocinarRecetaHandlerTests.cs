using Nido.Application.Recetas;

namespace Nido.Application.Tests.Recetas;

public sealed class CocinarRecetaHandlerTests
{
    [Fact]
    public async Task Handle_WhenRecetaIdEmpty_ReturnsNull()
    {
        var repo = new FakeRecetaRepository();
        var handler = new CocinarRecetaHandler(repo);

        var result = await handler.Handle(
            new CocinarRecetaCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repo.CocinarCalls);
    }

    [Fact]
    public async Task Handle_WhenHogarIdEmpty_ReturnsNull()
    {
        var repo = new FakeRecetaRepository();
        var handler = new CocinarRecetaHandler(repo);

        var result = await handler.Handle(
            new CocinarRecetaCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, repo.CocinarCalls);
    }

    [Fact]
    public async Task Handle_WhenRecetaNoExiste_ReturnsNull()
    {
        var repo = new FakeRecetaRepository { CocinarResult = null };
        var handler = new CocinarRecetaHandler(repo);

        var result = await handler.Handle(
            new CocinarRecetaCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, repo.CocinarCalls);
    }

    [Fact]
    public async Task Handle_WhenValido_DelegaAlRepositorioYDevuelveResultado()
    {
        var recetaId = Guid.NewGuid();
        var hogarId  = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repo = new FakeRecetaRepository { CocinarResult = new CocinarRecetaResult(recetaId, 1) };
        var handler = new CocinarRecetaHandler(repo);

        var result = await handler.Handle(
            new CocinarRecetaCommand(recetaId, hogarId, usuarioId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(recetaId, result!.RecetaId);
        Assert.Equal(1, result.VecesCocinada);
        Assert.Equal(1, repo.CocinarCalls);
    }

    [Fact]
    public async Task Handle_SegundaVez_DevuelveVecesCocinada2()
    {
        var recetaId = Guid.NewGuid();
        var repo = new FakeRecetaRepository { CocinarResult = new CocinarRecetaResult(recetaId, 2) };
        var handler = new CocinarRecetaHandler(repo);

        var result = await handler.Handle(
            new CocinarRecetaCommand(recetaId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.VecesCocinada);
    }

    [Fact]
    public async Task Handle_PasaElCommandoCorrectoAlRepositorio()
    {
        var recetaId  = Guid.NewGuid();
        var hogarId   = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var repo = new FakeRecetaRepository { CocinarResult = new CocinarRecetaResult(recetaId, 1) };
        var handler = new CocinarRecetaHandler(repo);

        await handler.Handle(new CocinarRecetaCommand(recetaId, hogarId, usuarioId), CancellationToken.None);

        Assert.NotNull(repo.LastCommand);
        Assert.Equal(recetaId,  repo.LastCommand!.RecetaId);
        Assert.Equal(hogarId,   repo.LastCommand.HogarId);
        Assert.Equal(usuarioId, repo.LastCommand.UsuarioId);
    }

    private sealed class FakeRecetaRepository : IRecetaRepository
    {
        public CocinarRecetaResult? CocinarResult { get; set; }
        public int CocinarCalls { get; private set; }
        public CocinarRecetaCommand? LastCommand { get; private set; }

        public Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RecetaResult>>(Array.Empty<RecetaResult>());

        public Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, CancellationToken ct)
            => Task.FromResult<RecetaResult?>(null);

        public Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct)
        {
            CocinarCalls++;
            LastCommand = command;
            return Task.FromResult(CocinarResult);
        }
    }
}
