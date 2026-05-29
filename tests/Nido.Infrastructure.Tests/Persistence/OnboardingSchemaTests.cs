using Microsoft.EntityFrameworkCore;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Tests.Persistence;

public sealed class OnboardingSchemaTests
{
    [Fact]
    public void Usuario_HasSexoAndFotoUrlColumns()
    {
        var options = new DbContextOptionsBuilder<NidoDbContext>().UseSqlite("DataSource=:memory:").Options;
        using var db = new NidoDbContext(options);
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.Usuario");

        Assert.NotNull(entity?.FindProperty("Sexo"));
        Assert.NotNull(entity?.FindProperty("FotoUrl"));
    }

    [Fact]
    public void Model_HasOnboardingStateEntity()
    {
        var options = new DbContextOptionsBuilder<NidoDbContext>().UseSqlite("DataSource=:memory:").Options;
        using var db = new NidoDbContext(options);

        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.OnboardingState"));
    }
}
