using Nido.Application.Electrodomesticos;
using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Tests.Electrodomesticos;

public sealed class UpdateElectrodomesticoHandlerTests
{
    [Fact]
    public async Task Handle_WhenElectrodomesticoExists_ReturnsUpdatedTipoAndEstado()
    {
        var hogarId = Guid.NewGuid();
        var electrodomesticoId = Guid.NewGuid();
        var repository = new FakeElectrodomesticoRepository
        {
            ExistingElectrodomestico = new Electrodomestico(
                electrodomesticoId, hogarId, null, "Heladera", "Cocina", "activo", "Whirlpool", null)
        };

        var handler = new UpdateElectrodomesticoHandler(repository);

        var result = await handler.Handle(
            new UpdateElectrodomesticoCommand(electrodomesticoId, hogarId, "Lavadero", "fuera de servicio"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(electrodomesticoId, result!.Id);
        Assert.Equal("Lavadero", result.Tipo);
        Assert.Equal("fuera de servicio", result.Estado);
        Assert.Equal((electrodomesticoId, hogarId), repository.LastUpdateArgs);
    }

    [Fact]
    public async Task Handle_WhenElectrodomesticoDoesNotExist_ReturnsNull()
    {
        var repository = new FakeElectrodomesticoRepository { ExistingElectrodomestico = null };
        var handler = new UpdateElectrodomesticoHandler(repository);

        var result = await handler.Handle(
            new UpdateElectrodomesticoCommand(Guid.NewGuid(), Guid.NewGuid(), "Otro", "activo"),
            CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class FakeElectrodomesticoRepository : IElectrodomesticoRepository
    {
        public Electrodomestico? ExistingElectrodomestico { get; init; }
        public (Guid Id, Guid HogarId)? LastUpdateArgs { get; private set; }

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
        {
            LastUpdateArgs = (id, hogarId);

            if (ExistingElectrodomestico is null) return Task.FromResult<Electrodomestico?>(null);

            var updated = new Electrodomestico(
                ExistingElectrodomestico.Id,
                ExistingElectrodomestico.HogarId,
                ExistingElectrodomestico.CatalogoId,
                ExistingElectrodomestico.Nombre,
                tipo ?? ExistingElectrodomestico.Tipo,
                estado ?? ExistingElectrodomestico.Estado,
                ExistingElectrodomestico.Marca,
                ExistingElectrodomestico.ImagenUrl);

            return Task.FromResult<Electrodomestico?>(updated);
        }

        public Task<bool> DeleteAsync(Guid id, Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult(false);
    }
}
