using Nido.Application.Electrodomesticos;
using Nido.Application.Electrodomesticos.Exceptions;
using Nido.Domain.Electrodomesticos;

namespace Nido.Application.Tests.Electrodomesticos;

public sealed class CreateElectrodomesticoHandlerTests
{
    [Fact]
    public async Task Handle_WhenCatalogItemProvided_UsesCatalogMetadataAndIgnoresManualValues()
    {
        var hogarId = Guid.NewGuid();
        var catalogoId = Guid.NewGuid();
        var repository = new FakeElectrodomesticoRepository
        {
            ExistingHouseholdIds = [hogarId],
            CatalogItems =
            [
                new ElectrodomesticoCatalogo(
                    catalogoId,
                    "Licuadora",
                    "Preparacion",
                    "blender",
                    "https://cdn.test.local/catalog/licuadora.webp",
                    1,
                    activo: true)
            ]
        };

        var handler = new CreateElectrodomesticoHandler(repository);

        var result = await handler.Handle(
            new CreateElectrodomesticoCommand(
                hogarId,
                catalogoId,
                "Nombre cliente",
                "Tipo cliente",
                "Activo",
                "Marca X",
                "https://evil.example/image.png"),
            CancellationToken.None);

        Assert.NotNull(repository.SavedElectrodomestico);
        Assert.Equal(hogarId, repository.SavedElectrodomestico!.HogarId);
        Assert.Equal(catalogoId, repository.SavedElectrodomestico.CatalogoId);
        Assert.Equal("Licuadora", repository.SavedElectrodomestico.Nombre);
        Assert.Equal("Preparacion", repository.SavedElectrodomestico.Tipo);
        Assert.Equal("https://cdn.test.local/catalog/licuadora.webp", repository.SavedElectrodomestico.ImagenUrl);
        Assert.Equal("Licuadora", result.Nombre);
        Assert.Equal("Preparacion", result.Tipo);
        Assert.Equal("https://cdn.test.local/catalog/licuadora.webp", result.ImagenUrl);
    }

    [Fact]
    public async Task Handle_WhenCatalogItemIsMissing_ThrowsNotFoundAndDoesNotSave()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakeElectrodomesticoRepository
        {
            ExistingHouseholdIds = [hogarId],
            CatalogItems = []
        };

        var handler = new CreateElectrodomesticoHandler(repository);

        await Assert.ThrowsAsync<ApplianceCatalogItemNotFoundException>(() => handler.Handle(
            new CreateElectrodomesticoCommand(
                hogarId,
                Guid.NewGuid(),
                null,
                null,
                "Activo",
                null,
                null),
            CancellationToken.None));

        Assert.Null(repository.SavedElectrodomestico);
    }

    private sealed class FakeElectrodomesticoRepository : IElectrodomesticoRepository
    {
        public HashSet<Guid> ExistingHouseholdIds { get; init; } = [];
        public IReadOnlyList<ElectrodomesticoCatalogo> CatalogItems { get; init; } = [];
        public Electrodomestico? SavedElectrodomestico { get; private set; }

        public Task<bool> HogarExisteAsync(Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult(ExistingHouseholdIds.Contains(hogarId));

        public Task SaveAsync(Electrodomestico electrodomestico, CancellationToken cancellationToken)
        {
            SavedElectrodomestico = electrodomestico;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Electrodomestico>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Electrodomestico>>([]);

        public Task<IReadOnlyList<Electrodomestico>> GetByHogarIdAsync(Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Electrodomestico>>([]);

        public Task<IReadOnlyList<ElectrodomesticoCatalogo>> GetCatalogoAsync(CancellationToken cancellationToken)
            => Task.FromResult(CatalogItems);
    }
}
