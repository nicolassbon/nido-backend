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
    public async Task CompletePairingAsync_WhenSameTokenCompletesConcurrently_IsIdempotentAndAvoidsDbFailure()
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

        Assert.Equal(2, outcomes.Count(static outcome => outcome is CompleteTelegramPairingResult));
        Assert.DoesNotContain(outcomes, static outcome => outcome is TelegramPairingTokenAlreadyConsumedException);
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

    [Fact]
    public async Task UnlinkActiveLinkAsync_WhenScopedActiveLinkExists_SetsOnlyThatLinkUnpaired()
    {
        var first = await SeedTokenAsync();
        var second = await SeedTokenAsync();
        await _sut.CompletePairingAsync(first.TokenHash, 5_001, CancellationToken.None);

        _db.TelegramChatLinks.Add(new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = 5_002,
            UsuarioId = second.UsuarioId,
            HogarId = second.HogarId,
            PairedAt = DateTime.UtcNow,
            UnpairedAt = null
        });
        await _db.SaveChangesAsync();

        var result = await _sut.UnlinkActiveLinkAsync(first.UsuarioId, first.HogarId, CancellationToken.None);

        Assert.Equal(5_001, result.ChatId);

        var links = await _db.TelegramChatLinks
            .Where(x => x.ChatId == 5_001 || x.ChatId == 5_002)
            .OrderBy(x => x.UsuarioId)
            .ToListAsync();

        var firstLink = Assert.Single(links, x => x.ChatId == 5_001 && x.UsuarioId == first.UsuarioId && x.HogarId == first.HogarId);
        var secondLink = Assert.Single(links, x => x.ChatId == 5_002 && x.UsuarioId == second.UsuarioId && x.HogarId == second.HogarId);
        Assert.NotNull(firstLink.UnpairedAt);
        Assert.Null(secondLink.UnpairedAt);
    }

    [Fact]
    public async Task UnlinkActiveLinkAsync_WhenScopedActiveLinkMissing_ThrowsTelegramChatNotLinkedException()
    {
        await Assert.ThrowsAsync<TelegramChatNotLinkedException>(() =>
            _sut.UnlinkActiveLinkAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetActiveLinkForCurrentMemberAsync_WhenMembershipMissing_UnpairsStaleLinkAndReturnsNull()
    {
        var seeded = await SeedTokenAsync();
        await _sut.CompletePairingAsync(seeded.TokenHash, 77_777, CancellationToken.None);

        var membership = await _db.MiembrosHogars.SingleAsync(x => x.UsuarioId == seeded.UsuarioId && x.HogarId == seeded.HogarId);
        _db.MiembrosHogars.Remove(membership);
        await _db.SaveChangesAsync();

        var result = await _sut.GetActiveLinkForCurrentMemberAsync(seeded.UsuarioId, seeded.HogarId, CancellationToken.None);

        Assert.Null(result);
        var link = await _db.TelegramChatLinks.SingleAsync(x => x.ChatId == 77_777);
        Assert.NotNull(link.UnpairedAt);
    }

    [Fact]
    public async Task CreatePairingArtifactsAsync_CreatesTokenAndCodeWithDistinctExpirations()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
        var codeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        var tokenHash = "token-hash-1";
        var codeHash = "code-hash-1";

        SeedUserAndHousehold(usuarioId, hogarId);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var (token, code) = await _sut.CreatePairingArtifactsAsync(
            hogarId,
            usuarioId,
            tokenHash,
            tokenExpiresAt,
            codeHash,
            codeExpiresAt,
            CancellationToken.None);

        Assert.Equal(hogarId, token.HogarId);
        Assert.Equal(usuarioId, token.UsuarioId);
        var storedToken = await _db.TelegramPairingTokens.SingleAsync(x => x.Id == token.Id);
        Assert.Equal(tokenHash, storedToken.TokenHash);
        Assert.Equal(tokenExpiresAt, storedToken.ExpiresAt);
        Assert.Equal(hogarId, code.HogarId);
        Assert.Equal(usuarioId, code.UsuarioId);
        var storedCode = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == code.Id);
        Assert.Equal(codeHash, storedCode.CodeHash);
        Assert.Equal(codeExpiresAt, storedCode.ExpiresAt);
        Assert.Equal(storedToken.CreatedAt, storedCode.CreatedAt);
    }

    [Fact]
    public async Task CreatePairingTokenAsync_CreatesPendingToken()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var tokenHash = "token-hash-single";

        SeedUserAndHousehold(usuarioId, hogarId);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var token = await _sut.CreatePairingTokenAsync(hogarId, usuarioId, tokenHash, expiresAt, CancellationToken.None);

        Assert.Equal(hogarId, token.HogarId);
        Assert.Equal(usuarioId, token.UsuarioId);
        Assert.Equal(TelegramPairingStatus.Pending, token.Status);

        var stored = await _db.TelegramPairingTokens.SingleAsync(x => x.Id == token.Id);
        Assert.Equal(tokenHash, stored.TokenHash);
        Assert.Equal(expiresAt, stored.ExpiresAt);
        Assert.Equal((int)TelegramPairingStatus.Pending, stored.Status);
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeValid_CreatesChatLinkAndConsumesCode()
    {
        var seeded = await SeedCodeAsync();

        var result = await _sut.CompletePairingByCodeAsync(seeded.CodeHash, 22222, CancellationToken.None);

        Assert.Equal(22222, result.ChatId);
        var code = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == seeded.CodeId);
        var link = await _db.TelegramChatLinks.SingleAsync(x => x.ChatId == 22222);
        Assert.NotNull(code.ConsumedAt);
        Assert.Equal((int)TelegramPairingStatus.Consumed, code.Status);
        Assert.Equal(seeded.UsuarioId, link.UsuarioId);
        Assert.Equal(seeded.HogarId, link.HogarId);
    }

    [Fact]
    public async Task CompletePairingAsync_WhenTokenConsumed_RevokesSiblingCodeFromSameIssuance()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenHash = "token-hash-sibling";
        var codeHash = "code-hash-sibling";

        SeedUserAndHousehold(usuarioId, hogarId);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var (_, code) = await _sut.CreatePairingArtifactsAsync(
            hogarId,
            usuarioId,
            tokenHash,
            DateTime.UtcNow.AddMinutes(10),
            codeHash,
            DateTime.UtcNow.AddMinutes(15),
            CancellationToken.None);

        await _sut.CompletePairingAsync(tokenHash, 22334, CancellationToken.None);

        var storedCode = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == code.Id);
        Assert.Equal((int)TelegramPairingStatus.Revoked, storedCode.Status);
        Assert.NotNull(storedCode.RevokedAt);
        await Assert.ThrowsAsync<TelegramPairingCodeRevokedException>(() =>
            _sut.CompletePairingByCodeAsync(codeHash, 22335, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeConsumed_RevokesSiblingTokenFromSameIssuance()
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var tokenHash = "token-hash-sibling-2";
        var codeHash = "code-hash-sibling-2";

        SeedUserAndHousehold(usuarioId, hogarId);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var (token, _) = await _sut.CreatePairingArtifactsAsync(
            hogarId,
            usuarioId,
            tokenHash,
            DateTime.UtcNow.AddMinutes(10),
            codeHash,
            DateTime.UtcNow.AddMinutes(15),
            CancellationToken.None);

        await _sut.CompletePairingByCodeAsync(codeHash, 32334, CancellationToken.None);

        var storedToken = await _db.TelegramPairingTokens.SingleAsync(x => x.Id == token.Id);
        Assert.Equal((int)TelegramPairingStatus.Revoked, storedToken.Status);
        Assert.NotNull(storedToken.RevokedAt);
        await Assert.ThrowsAsync<TelegramPairingTokenRevokedException>(() =>
            _sut.CompletePairingAsync(tokenHash, 32335, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeValid_UnpairsPreviousActiveLinksForChatAndUser()
    {
        var seeded = await SeedCodeAsync();
        var otherUsuarioId = Guid.NewGuid();
        var otherHogarId = Guid.NewGuid();
        SeedUserAndHousehold(otherUsuarioId, otherHogarId);
        var existingSameChat = new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = 90001,
            UsuarioId = otherUsuarioId,
            HogarId = otherHogarId,
            PairedAt = DateTime.UtcNow.AddMinutes(-20),
            UnpairedAt = null
        };
        var existingSameUser = new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = 90002,
            UsuarioId = seeded.UsuarioId,
            HogarId = seeded.HogarId,
            PairedAt = DateTime.UtcNow.AddMinutes(-10),
            UnpairedAt = null
        };
        _db.TelegramChatLinks.AddRange(existingSameChat, existingSameUser);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.CompletePairingByCodeAsync(seeded.CodeHash, 90001, CancellationToken.None);

        Assert.Equal(90001, result.ChatId);

        var sameChat = await _db.TelegramChatLinks.SingleAsync(x => x.Id == existingSameChat.Id);
        var sameUser = await _db.TelegramChatLinks.SingleAsync(x => x.Id == existingSameUser.Id);
        var activeLinks = await _db.TelegramChatLinks
            .Where(x => x.ChatId == 90001 && x.UnpairedAt == null)
            .ToListAsync();

        Assert.NotNull(sameChat.UnpairedAt);
        Assert.NotNull(sameUser.UnpairedAt);
        Assert.Single(activeLinks);
        Assert.Equal(seeded.UsuarioId, activeLinks[0].UsuarioId);
        Assert.Equal(seeded.HogarId, activeLinks[0].HogarId);
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeUnknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<TelegramPairingCodeNotFoundException>(() =>
            _sut.CompletePairingByCodeAsync("unknown-code-hash", 33333, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeUnknown_DoesNotIncrementOtherPendingCodeAttempts()
    {
        var seeded = await SeedCodeAsync(attemptCount: 3);

        await Assert.ThrowsAsync<TelegramPairingCodeNotFoundException>(() =>
            _sut.CompletePairingByCodeAsync("unknown-code-hash", 33334, CancellationToken.None));

        var untouched = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == seeded.CodeId);
        Assert.Equal(3, untouched.AttemptCount);
        Assert.Equal((int)TelegramPairingStatus.Pending, untouched.Status);
        Assert.Null(untouched.RevokedAt);
        Assert.Null(untouched.ConsumedAt);
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeExpired_ThrowsExpired()
    {
        var seeded = await SeedCodeAsync(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        await Assert.ThrowsAsync<TelegramPairingCodeExpiredException>(() =>
            _sut.CompletePairingByCodeAsync(seeded.CodeHash, 44444, CancellationToken.None));

        var code = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == seeded.CodeId);
        Assert.Equal(0, code.AttemptCount);
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeRevoked_ThrowsRevoked()
    {
        var seeded = await SeedCodeAsync(revokedAt: DateTime.UtcNow.AddMinutes(-1), status: TelegramPairingStatus.Revoked);

        await Assert.ThrowsAsync<TelegramPairingCodeRevokedException>(() =>
            _sut.CompletePairingByCodeAsync(seeded.CodeHash, 55555, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenCodeConsumed_ThrowsRevoked()
    {
        var seeded = await SeedCodeAsync(consumedAt: DateTime.UtcNow.AddMinutes(-1), status: TelegramPairingStatus.Consumed);

        await Assert.ThrowsAsync<TelegramPairingCodeRevokedException>(() =>
            _sut.CompletePairingByCodeAsync(seeded.CodeHash, 66666, CancellationToken.None));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenMembershipMissing_IncrementsAttempt()
    {
        var seeded = await SeedCodeAsync();
        var membership = await _db.MiembrosHogars.SingleAsync(x => x.UsuarioId == seeded.UsuarioId && x.HogarId == seeded.HogarId);
        _db.MiembrosHogars.Remove(membership);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<TelegramHogarAccessDeniedException>(() =>
            _sut.CompletePairingByCodeAsync(seeded.CodeHash, 77777, CancellationToken.None));

        var code = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == seeded.CodeId);
        Assert.Equal(1, code.AttemptCount);
        Assert.Equal((int)TelegramPairingStatus.Pending, code.Status);
        Assert.False(await _db.TelegramChatLinks.AnyAsync(x => x.ChatId == 77777));
    }

    [Fact]
    public async Task CompletePairingByCodeAsync_WhenFifthAttemptFails_RevokesCode()
    {
        var seeded = await SeedCodeAsync(attemptCount: 4);
        var membership = await _db.MiembrosHogars.SingleAsync(x => x.UsuarioId == seeded.UsuarioId && x.HogarId == seeded.HogarId);
        _db.MiembrosHogars.Remove(membership);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<TelegramPairingCodeRevokedException>(() =>
            _sut.CompletePairingByCodeAsync(seeded.CodeHash, 88888, CancellationToken.None));

        var code = await _db.TelegramPairingCodes.SingleAsync(x => x.Id == seeded.CodeId);
        Assert.Equal(5, code.AttemptCount);
        Assert.NotNull(code.RevokedAt);
        Assert.Equal((int)TelegramPairingStatus.Revoked, code.Status);
    }

    private async Task<(Guid CodeId, Guid UsuarioId, Guid HogarId, string CodeHash)> SeedCodeAsync(
        DateTime? expiresAt = null,
        DateTime? consumedAt = null,
        DateTime? revokedAt = null,
        TelegramPairingStatus status = TelegramPairingStatus.Pending,
        int attemptCount = 0)
    {
        var usuarioId = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var codeId = Guid.NewGuid();
        var codeHash = $"code-hash-{Guid.NewGuid():N}";

        SeedUserAndHousehold(usuarioId, hogarId);

        _db.TelegramPairingCodes.Add(new TelegramPairingCode
        {
            Id = codeId,
            HogarId = hogarId,
            UsuarioId = usuarioId,
            CodeHash = codeHash,
            Status = (int)status,
            AttemptCount = attemptCount,
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            ConsumedAt = consumedAt,
            RevokedAt = revokedAt
        });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return (codeId, usuarioId, hogarId, codeHash);
    }

    private void SeedUserAndHousehold(Guid usuarioId, Guid hogarId)
    {
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
