using Nido.Application.Electrodomesticos;
using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Tests.Electrodomesticos;

public sealed class DeleteElectrodomesticoHandlerTests
{
    [Fact]
    public async Task Handle_WhenElectrodomesticoExists_ReturnsTrue()
    {
        var electrodomesticoId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var repository = new FakeElectrodomesticoRepository { DeleteResult = true };
        var handler = new DeleteElectrodomesticoHandler(repository);

        var result = await handler.Handle(
            new DeleteElectrodomesticoCommand(electrodomesticoId, hogarId), CancellationToken.None);

        Assert.True(result);
        Assert.Equal((electrodomesticoId, hogarId), repository.LastDeleteArgs);
    }

    [Fact]
    public async Task Handle_WhenElectrodomesticoDoesNotExist_ReturnsFalse()
    {
        var repository = new FakeElectrodomesticoRepository { DeleteResult = false };
        var handler = new DeleteElectrodomesticoHandler(repository);

        var result = await handler.Handle(
            new DeleteElectrodomesticoCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result);
    }

    private sealed class FakeElectrodomesticoRepository : IElectrodomesticoRepository
    {
        public bool DeleteResult { get; init; }
        public (Guid Id, Guid HogarId)? LastDeleteArgs { get; private set; }

        public Task<bool> HogarExisteAsync(Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task SaveAsync(Electrodomestico electrodomestico, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Electrodomestico>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Electrodomestico>>([]);

        public Task<IReadOnlyList<Electrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Electrodomestico>>([]);

        public Task<IReadOnlyList<ElectrodomesticoCatalogo>> GetCatalogoAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ElectrodomesticoCatalogo>>([]);

        public Task<Electrodomestico?> UpdateAsync(Guid id, Guid hogarId, string? tipo, string? estado, CancellationToken cancellationToken)
            => Task.FromResult<Electrodomestico?>(null);

        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken cancellationToken)
        {
            LastDeleteArgs = (id, hogarId);
            return Task.FromResult(DeleteResult);
        }
    }
}
