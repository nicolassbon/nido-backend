using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Tests.Persistence;

public sealed class OnboardingSchemaTests
{
    private static DbContextOptions<NidoDbContext> CreateOptions()
        => new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql("Host=localhost;Database=nido_model_only;Username=test;Password=test")
            .Options;

    [Fact]
    public void Usuario_HasSexoAndFotoStorageKeyColumns()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.Usuario");

        Assert.NotNull(entity?.FindProperty("Sexo"));
        Assert.NotNull(entity?.FindProperty("FotoStorageKey"));
    }

    [Fact]
    public void Model_HasOnboardingStateEntity()
    {
        using var db = new NidoDbContext(CreateOptions());

        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.OnboardingState"));
    }
}
