using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Telegram.Idempotency;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;
using Npgsql;

namespace Nido.Infrastructure.Telegram.Idempotency;

public sealed class TelegramUpdateIdempotencyService : ITelegramUpdateIdempotencyService
{
    public const string UpdateIdUniqueConstraint = "uq_processed_telegram_updates_update_id";

    private readonly NidoDbContext _db;

    public TelegramUpdateIdempotencyService(NidoDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsAlreadyProcessedAsync(long updateId, CancellationToken ct)
    {
        return await _db.ProcessedTelegramUpdates
            .AsNoTracking()
            .AnyAsync(p => p.UpdateId == updateId, ct);
    }

    public async Task<bool> TryReserveAsync(long updateId, string? updateHash, CancellationToken ct)
    {
        var entity = new ProcessedTelegramUpdate
        {
            Id = Guid.NewGuid(),
            UpdateId = updateId,
            UpdateHash = updateHash,
            ProcessedAt = DateTime.UtcNow
        };

        _db.ProcessedTelegramUpdates.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUpdateIdUniqueViolation(ex))
        {
            _db.Entry(entity).State = EntityState.Detached;
            return false;
        }
    }

    public async Task ReleaseReservationAsync(long updateId, CancellationToken ct)
    {
        var entity = await _db.ProcessedTelegramUpdates
            .SingleOrDefaultAsync(p => p.UpdateId == updateId, ct);

        if (entity is null)
        {
            return;
        }

        _db.ProcessedTelegramUpdates.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    internal static bool IsUpdateIdUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && string.Equals(
                pg.ConstraintName,
                UpdateIdUniqueConstraint,
                StringComparison.Ordinal);
    }
}
