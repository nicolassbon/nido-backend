using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Common.Security;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Pairing;
using Nido.Infrastructure.Hogares;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Nido.Infrastructure.Telegram.Pairing;
using Nido.Tests.Shared;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramPairingRepositoryTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private TelegramPairingRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_pairing_repo");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();

        var membershipRepository = new HogarMembershipRepository(_db);
        var membershipService = new HouseholdMembershipService(membershipRepository);
        _sut = new TelegramPairingRepository(_db, membershipService);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenValid_CreatesChatLinkAndConsumesToken()
    {
        var seeded = await SeedTokenAsync();

        var result = await _sut.CompletePairingAsync(seeded.TokenHash, 12345, CancellationToken.None);

        Assert.Equal(12345, result.ChatId);

        var token = await _db.TelegramPairingTokens.SingleAsync(x => x.Id == seeded.TokenId);
        var link = await _db.TelegramChatLinks.SingleAsync(x => x.ChatId == 12345);
        Assert.NotNull(token.ConsumedAt);
        Assert.Equal((int)TelegramPairingStatus.Consumed, token.Status);
        Assert.Equal(seeded.UsuarioId, link.UsuarioId);
        Assert.Equal(seeded.HogarId, link.HogarId);
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenExpired_ThrowsAndDoesNotCreateLink()
    {
        var seeded = await SeedTokenAsync(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<TelegramPairingTokenExpiredException>(() =>
            _sut.CompletePairingAsync(seeded.TokenHash, 444, CancellationToken.None));

        Assert.False(await _db.TelegramChatLinks.AnyAsync(x => x.ChatId == 444));
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenConsumed_Throws()
    {
        var seeded = await SeedTokenAsync(consumedAt: DateTime.UtcNow.AddMinutes(-1), status: TelegramPairingStatus.Consumed);

        await Assert.ThrowsAsync<TelegramPairingTokenAlreadyConsumedException>(() =>
            _sut.CompletePairingAsync(seeded.TokenHash, 555, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenRevoked_Throws()
    {
        var seeded = await SeedTokenAsync(revokedAt: DateTime.UtcNow.AddMinutes(-1), status: TelegramPairingStatus.Revoked);

        await Assert.ThrowsAsync<TelegramPairingTokenRevokedException>(() =>
            _sut.CompletePairingAsync(seeded.TokenHash, 666, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenNotFound_ThrowsTelegramPairingTokenNotFoundException()
    {
        var unknownHash = $"hash-{Guid.NewGuid():N}";

        await Assert.ThrowsAsync<TelegramPairingTokenNotFoundException>(() =>
            _sut.CompletePairingAsync(unknownHash, 333, CancellationToken.None));

        Assert.False(await _db.TelegramChatLinks.AnyAsync(x => x.ChatId == 333));
    }

    [Fact]
    public async Task CompletePairingAsync_WhenMembershipMissing_RevokesTokenAndDoesNotCreateLink()
    {
        var seeded = await SeedTokenAsync();
        var membership = await _db.MiembrosHogars.SingleAsync(x => x.UsuarioId == seeded.UsuarioId && x.HogarId == seeded.HogarId);
        _db.MiembrosHogars.Remove(membership);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<TelegramHogarAccessDeniedException>(() =>
            _sut.CompletePairingAsync(seeded.TokenHash, 777, CancellationToken.None));

        var token = await _db.TelegramPairingTokens.SingleAsync(x => x.Id == seeded.TokenId);
        Assert.NotNull(token.RevokedAt);
        Assert.Equal((int)TelegramPairingStatus.Revoked, token.Status);
        Assert.False(await _db.TelegramChatLinks.AnyAsync(x => x.ChatId == 777));
    }

    [Fact]
    public async Task CompletePairingAsync_WhenSameTokenCompletesConcurrently_ReturnsAlreadyConsumedInsteadOfDbFailure()
    {
        var seeded = await SeedTokenAsync();
        await using var db1 = CreateDbContext();
        await using var db2 = CreateDbContext();

        var repo1 = CreateRepository(db1);
        var repo2 = CreateRepository(db2);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<object> AttemptAsync(TelegramPairingRepository repository)
        {
            await start.Task;

            try
            {
                return await repository.CompletePairingAsync(seeded.TokenHash, 9_999, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var firstTask = Task.Run(() => AttemptAsync(repo1));
        var secondTask = Task.Run(() => AttemptAsync(repo2));

        start.SetResult();

        var outcomes = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(1, outcomes.Count(static outcome => outcome is CompleteTelegramPairingResult));
        Assert.Equal(1, outcomes.Count(static outcome => outcome is TelegramPairingTokenAlreadyConsumedException));
        Assert.DoesNotContain(outcomes, static outcome => outcome is DbUpdateException);

        var activeLinks = await _db.TelegramChatLinks.CountAsync(x => x.ChatId == 9_999 && x.UnpairedAt == null);
        Assert.Equal(1, activeLinks);
    }

    [Fact]
    public async Task UnlinkChatAsync_WhenActiveLinkExists_SetsUnpairedAt()
    {
        var seeded = await SeedTokenAsync();
        await _sut.CompletePairingAsync(seeded.TokenHash, 888, CancellationToken.None);

        var result = await _sut.UnlinkChatAsync(888, CancellationToken.None);

        Assert.Equal(888, result.ChatId);
        var link = await _db.TelegramChatLinks.SingleAsync(x => x.ChatId == 888);
        Assert.NotNull(link.UnpairedAt);
    }

    private async Task<(Guid TokenId, Guid UsuarioId, Guid HogarId, string TokenHash)> SeedTokenAsync(
        DateTime? expiresAt = null,
        DateTime? consumedAt = null,
        DateTime? revokedAt = null,
        TelegramPairingStatus status = TelegramPairingStatus.Pending)
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var tokenHash = $"hash-{Guid.NewGuid():N}";

        _db.Usuarios.Add(new Usuario
        {
            Id = usuarioId,
            Nombre = "Pair User",
            Email = $"{Guid.NewGuid():N}@test.local",
            Sexo = "U",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.Hogares.Add(new Hogare
        {
            Id = hogarId,
            Nombre = "Pair Hogar",
            CreatedAt = DateTime.UtcNow
        });

        _db.MiembrosHogars.Add(new MiembrosHogar
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            HogarId = hogarId,
            Rol = "owner",
            Puntos = 0
        });

        _db.TelegramPairingTokens.Add(new TelegramPairingToken
        {
            Id = tokenId,
            HogarId = hogarId,
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            Status = (int)status,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = consumedAt,
            RevokedAt = revokedAt
        });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return (tokenId, usuarioId, hogarId, tokenHash);
    }

    private NidoDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new NidoDbContext(options);
    }

    private static TelegramPairingRepository CreateRepository(NidoDbContext dbContext)
    {
        var membershipRepository = new HogarMembershipRepository(dbContext);
        var membershipService = new HouseholdMembershipService(membershipRepository);
        return new TelegramPairingRepository(dbContext, membershipService);
    }
}
