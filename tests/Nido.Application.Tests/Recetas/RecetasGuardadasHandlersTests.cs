using Nido.Application.Recetas;

namespace Nido.Application.Tests.Recetas;

public sealed class RecetasGuardadasHandlersTests
{
    [Fact]
    public async Task SaveReceta_CallsRepositoryWithHouseholdScope()
    {
        var repo = new FakeRecetaRepository();
        var handler = new SaveRecetaHandler(repo);
        var recetaId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var saved = await handler.Handle(recetaId, hogarId, usuarioId, CancellationToken.None);

        Assert.True(saved);
        Assert.Equal(recetaId, repo.SavedRecetaId);
        Assert.Equal(hogarId, repo.SavedHogarId);
        Assert.Equal(usuarioId, repo.SavedUsuarioId);
    }

    [Fact]
    public async Task GetGuardadas_ReturnsOnlySavedRecipes()
    {
        var receta = new RecetaResult(Guid.NewGuid(), "Tarta", null, null, null, null, null, null, null, null, null, null, [], [], [], 0, 0, 0, false, null, null, [], true);
        var repo = new FakeRecetaRepository { Saved = [receta] };
        var handler = new GetRecetasGuardadasHandler(repo);

        var result = await handler.Handle(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var item = Assert.Single(result);
        Assert.True(item.Guardada);
    }

    private sealed class FakeRecetaRepository : IRecetaRepository
    {
        public IReadOnlyList<RecetaResult> Saved { get; init; } = [];
        public Guid SavedRecetaId { get; private set; }
        public Guid SavedHogarId { get; private set; }
        public Guid SavedUsuarioId { get; private set; }

        public Task<IReadOnlyList<RecetaResult>> GetAllAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RecetaResult>>(Array.Empty<RecetaResult>());

        public Task<IReadOnlyList<RecetaResult>> GetSavedAsync(Guid hogarId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult(Saved);

        public Task<RecetaResult?> GetByIdAsync(Guid id, Guid hogarId, Guid usuarioId, CancellationToken ct)
            => Task.FromResult<RecetaResult?>(null);

        public Task<bool> SaveAsync(Guid recetaId, Guid hogarId, Guid usuarioId, CancellationToken ct)
        {
            SavedRecetaId = recetaId;
            SavedHogarId = hogarId;
            SavedUsuarioId = usuarioId;
            return Task.FromResult(true);
        }

        public Task<bool> UnsaveAsync(Guid recetaId, Guid hogarId, CancellationToken ct)
            => Task.FromResult(true);

        public Task<CocinarRecetaResult?> CocinarAsync(CocinarRecetaCommand command, CancellationToken ct)
            => Task.FromResult<CocinarRecetaResult?>(null);
    }
}
