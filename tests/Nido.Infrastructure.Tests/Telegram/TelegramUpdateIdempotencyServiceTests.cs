using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nido.Application.Telegram.Idempotency;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Telegram.Idempotency;
using Nido.Tests.Shared;
using Npgsql;
using Xunit;

namespace Nido.Infrastructure.Tests.Telegram;

public sealed class TelegramUpdateIdempotencyServiceTests : IAsyncLifetime
{
    private readonly PostgresTestServer _server = PostgresTestServer.GetSharedAsync().GetAwaiter().GetResult();
    private PostgresTestDatabase _database = null!;
    private NidoDbContext _db = null!;
    private TelegramUpdateIdempotencyService _sut = null!;

    public async Task InitializeAsync()
    {
        _database = await _server.CreateDatabaseAsync("telegram_idempotency");

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _db = new NidoDbContext(options);
        await _db.Database.MigrateAsync();
        _sut = new TelegramUpdateIdempotencyService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task IsAlreadyProcessedAsync_ReturnsFalseForUnknownUpdateId()
    {
        var result = await _sut.IsAlreadyProcessedAsync(42L, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task TryReserveAsync_PersistsRow_ThenIsAlreadyProcessedReturnsTrue()
    {
        const long updateId = 1001L;

        var first = await _sut.TryReserveAsync(updateId, "hash-abc", CancellationToken.None);
        var second = await _sut.IsAlreadyProcessedAsync(updateId, CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
    }

    [Fact]
    public async Task TryReserveAsync_RaceForSameUpdateId_LoserReturnsFalse()
    {
        const long updateId = 2002L;

        var first = await _sut.TryReserveAsync(updateId, null, CancellationToken.None);
        var second = await _sut.TryReserveAsync(updateId, null, CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task TryReserveAsync_StampsProcessedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var recorded = await _sut.TryReserveAsync(3003L, null, CancellationToken.None);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.True(recorded);
        var row = await _db.ProcessedTelegramUpdates.AsNoTracking().SingleAsync();
        Assert.InRange(row.ProcessedAt, before, after);
        Assert.Null(row.UpdateHash);
    }

    [Fact]
    public async Task ReleaseReservationAsync_RemovesRow_AndRetryCanReserveAgain()
    {
        const long updateId = 4004L;

        Assert.True(await _sut.TryReserveAsync(updateId, null, CancellationToken.None));

        await _sut.ReleaseReservationAsync(updateId, CancellationToken.None);

        Assert.False(await _sut.IsAlreadyProcessedAsync(updateId, CancellationToken.None));
        Assert.True(await _sut.TryReserveAsync(updateId, null, CancellationToken.None));
    }

    [Fact]
    public void IsUpdateIdUniqueViolation_True_OnlyForMatchingConstraintAndSqlState()
    {
        var match = new DbUpdateException(
            "duplicate key",
            new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                detail: null,
                hint: null,
                position: 0,
                internalPosition: 0,
                internalQuery: null,
                where: null,
                schemaName: "public",
                tableName: "processed_telegram_updates",
                columnName: null,
                dataTypeName: null,
                constraintName: TelegramUpdateIdempotencyService.UpdateIdUniqueConstraint,
                file: "n/a",
                line: "0",
                routine: "n/a"));

        Assert.True(TelegramUpdateIdempotencyService.IsUpdateIdUniqueViolation(match));
    }

    [Fact]
    public void IsUpdateIdUniqueViolation_False_WhenConstraintNameDoesNotMatch()
    {
        var wrongConstraint = new DbUpdateException(
            "duplicate key",
            new PostgresException(
                messageText: "duplicate key value violates unique constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                detail: null,
                hint: null,
                position: 0,
                internalPosition: 0,
                internalQuery: null,
                where: null,
                schemaName: "public",
                tableName: "processed_telegram_updates",
                columnName: null,
                dataTypeName: null,
                constraintName: "uq_some_other_index",
                file: "n/a",
                line: "0",
                routine: "n/a"));

        Assert.False(TelegramUpdateIdempotencyService.IsUpdateIdUniqueViolation(wrongConstraint));
    }

    [Fact]
    public void IsUpdateIdUniqueViolation_False_WhenSqlStateIsNotUniqueViolation()
    {
        var notUnique = new DbUpdateException(
            "foreign key violation",
            new PostgresException(
                messageText: "insert or update on table violates foreign key constraint",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.ForeignKeyViolation,
                detail: null,
                hint: null,
                position: 0,
                internalPosition: 0,
                internalQuery: null,
                where: null,
                schemaName: "public",
                tableName: "processed_telegram_updates",
                columnName: null,
                dataTypeName: null,
                constraintName: TelegramUpdateIdempotencyService.UpdateIdUniqueConstraint,
                file: "n/a",
                line: "0",
                routine: "n/a"));

        Assert.False(TelegramUpdateIdempotencyService.IsUpdateIdUniqueViolation(notUnique));
    }

    [Fact]
    public void IsUpdateIdUniqueViolation_False_WhenInnerExceptionIsNotPostgres()
    {
        // Simulates a non-Npgsql DbUpdateException (e.g. provider-agnostic
        // connection failure surfaced through EF).
        var notPostgres = new DbUpdateException("connection lost", new InvalidOperationException("boom"));

        Assert.False(TelegramUpdateIdempotencyService.IsUpdateIdUniqueViolation(notPostgres));
    }

    [Fact]
    public async Task TryReserveAsync_BubblesNonUniqueViolationDbUpdateException()
    {
        var interceptor = new ThrowingSaveChangesInterceptor(
            new DbUpdateException(
                "simulated transient failure",
                new PostgresException(
                    messageText: "connection reset",
                    severity: "ERROR",
                    invariantSeverity: "ERROR",
                    sqlState: PostgresErrorCodes.ConnectionFailure,
                    detail: null,
                    hint: null,
                    position: 0,
                    internalPosition: 0,
                    internalQuery: null,
                    where: null,
                    schemaName: null,
                    tableName: null,
                    columnName: null,
                    dataTypeName: null,
                    constraintName: null,
                    file: "n/a",
                    line: "0",
                    routine: "n/a")));

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(interceptor)
            .Options;

        await using var failingDb = new NidoDbContext(options);
        var failingService = new TelegramUpdateIdempotencyService(failingDb);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(
            () => failingService.TryReserveAsync(9999L, null, CancellationToken.None));

        Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.ConnectionFailure, ((PostgresException)ex.InnerException!).SqlState);
    }

    [Fact]
    public async Task TryReserveAsync_BubblesDbUpdateExceptionWithNullInnerException()
    {
        var interceptor = new ThrowingSaveChangesInterceptor(
            new DbUpdateException("provider-level failure with no detail"));

        var options = new DbContextOptionsBuilder<NidoDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .AddInterceptors(interceptor)
            .Options;

        await using var failingDb = new NidoDbContext(options);
        var failingService = new TelegramUpdateIdempotencyService(failingDb);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => failingService.TryReserveAsync(8888L, null, CancellationToken.None));
    }

    private sealed class ThrowingSaveChangesInterceptor : ISaveChangesInterceptor
    {
        private readonly Exception _toThrow;

        public ThrowingSaveChangesInterceptor(Exception toThrow)
        {
            _toThrow = toThrow;
        }

        public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            throw _toThrow;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            throw _toThrow;
        }

        public int SavedChanges(SaveChangesCompletedEventData eventData, int result) => result;

        public void SaveChangesFailed(DbContextErrorEventData eventData) { }

        public Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(result);
    }
}
