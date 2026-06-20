using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Nido.Infrastructure.Persistence;

namespace Nido.Infrastructure.Tests.Persistence;

public sealed class TelegramSchemaTests
{
    private static DbContextOptions<NidoDbContext> CreateOptions()
        => new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql("Host=localhost;Database=nido_model_only;Username=test;Password=test")
            .Options;

    [Fact]
    public void Model_HasTelegramChatLinkEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramChatLink"));
    }

    [Fact]
    public void Model_HasProcessedTelegramUpdateEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.ProcessedTelegramUpdate"));
    }

    [Fact]
    public void Model_HasTelegramOutboxMessageEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramOutboxMessage"));
    }

    [Fact]
    public void Model_HasTelegramBatchEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramBatch"));
    }

    [Fact]
    public void Model_HasTelegramPairingTokenEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingToken"));
    }

    [Fact]
    public void Model_HasTelegramPairingCodeEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingCode"));
    }

    [Fact]
    public void Model_HasTelegramConversationStateEntity()
    {
        using var db = new NidoDbContext(CreateOptions());
        Assert.NotNull(db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramConversationStateEntity"));
    }

    [Fact]
    public void TelegramChatLink_HasUniqueActiveChatIdIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramChatLink");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == "ChatId");
        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramChatLink_HasUniqueActiveUsuarioHogarIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramChatLink");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 2 &&
            i.Properties.Any(p => p.Name == "UsuarioId") &&
            i.Properties.Any(p => p.Name == "HogarId"));
        Assert.NotNull(index);
    }

    [Fact]
    public void ProcessedTelegramUpdate_HasUniqueUpdateIdIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.ProcessedTelegramUpdate");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == "UpdateId");
        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramOutboxMessage_HasPartialUniquePendingIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramOutboxMessage");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i =>
            i.IsUnique &&
            i.Properties.Count == 3 &&
            i.Properties.Any(p => p.Name == "HogarId") &&
            i.Properties.Any(p => p.Name == "ChatId") &&
            i.Properties.Any(p => p.Name == "MessageType"));
        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramChatLink_HasForeignKeysToUsuarioAndHogar()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramChatLink");
        Assert.NotNull(entity);

        var fkUsuario = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Usuario");
        var fkHogar = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Hogare");

        Assert.NotNull(fkUsuario);
        Assert.NotNull(fkHogar);
    }

    [Fact]
    public void TelegramOutboxMessage_HasForeignKeyToTelegramBatch()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramOutboxMessage");
        Assert.NotNull(entity);

        var fkBatch = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.TelegramBatch");

        Assert.NotNull(fkBatch);
    }

    [Fact]
    public void TelegramConversationState_HasPrimaryKeyOnChatId()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramConversationStateEntity");
        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();
        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);
        Assert.Equal("ChatId", primaryKey.Properties[0].Name);
    }

    [Fact]
    public void TelegramConversationState_HasExpiresAtIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramConversationStateEntity");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            i.Properties[0].Name == "ExpiresAtUtc");

        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramPairingToken_HasUniqueTokenHashIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingToken");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == "TokenHash");
        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramPairingToken_HasForeignKeysToUsuarioAndHogar()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingToken");
        Assert.NotNull(entity);

        var fkUsuario = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Usuario");
        var fkHogar = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Hogare");

        Assert.NotNull(fkUsuario);
        Assert.NotNull(fkHogar);
    }

    [Fact]
    public void TelegramPairingCode_HasUniqueCodeHashIndex()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingCode");
        Assert.NotNull(entity);

        var index = entity.GetIndexes().FirstOrDefault(i => i.IsUnique && i.Properties.Count == 1 && i.Properties[0].Name == "CodeHash");
        Assert.NotNull(index);
    }

    [Fact]
    public void TelegramPairingCode_HasForeignKeysToUsuarioAndHogar()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingCode");
        Assert.NotNull(entity);

        var fkUsuario = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Usuario");
        var fkHogar = entity.GetForeignKeys().FirstOrDefault(fk =>
            fk.PrincipalEntityType.Name == "Nido.Infrastructure.Persistence.Entities.Hogare");

        Assert.NotNull(fkUsuario);
        Assert.NotNull(fkHogar);
    }

    [Fact]
    public void TelegramPairingCode_HasAttemptCountWithDefaultZero()
    {
        using var db = new NidoDbContext(CreateOptions());
        var entity = db.Model.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingCode");
        Assert.NotNull(entity);

        var property = entity.FindProperty("AttemptCount");
        Assert.NotNull(property);
        Assert.Equal(0, property.GetDefaultValue());
    }

    [Fact]
    public void TelegramPairingCode_HasAttemptCountCheckConstraint()
    {
        using var db = new NidoDbContext(CreateOptions());
        var designTimeModel = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramPairingCode");
        Assert.NotNull(entity);

        var constraint = entity.GetCheckConstraints()
            .FirstOrDefault(c => c.Name == "ck_telegram_pairing_codes_attempt_count");

        Assert.NotNull(constraint);
        Assert.Equal("attempt_count <= 5", constraint.Sql);
    }

    [Fact]
    public void TelegramBatch_HasStatusCheckConstraint()
    {
        using var db = new NidoDbContext(CreateOptions());
        var designTimeModel = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType("Nido.Infrastructure.Persistence.Entities.TelegramBatch");
        Assert.NotNull(entity);

        var constraint = entity.GetCheckConstraints()
            .FirstOrDefault(c => c.Name == "ck_telegram_batches_status");

        Assert.NotNull(constraint);
        Assert.Equal("status >= 0 AND status <= 4", constraint.Sql);
    }
}
