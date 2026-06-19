using System.Data;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Nido.Application.Common.Security;
using Nido.Application.Telegram;
using Nido.Application.Telegram.Exceptions;
using Nido.Application.Telegram.Pairing;
using Nido.Infrastructure.Persistence;
using Nido.Infrastructure.Persistence.Entities;

namespace Nido.Infrastructure.Telegram.Pairing;

public sealed class TelegramPairingRepository(
    NidoDbContext dbContext,
    IHouseholdMembershipService householdMembershipService) : ITelegramPairingRepository
{
    public async Task<TelegramPairingTokenResult> CreatePairingTokenAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime expiresAt,
        CancellationToken ct)
    {
        var entity = new TelegramPairingToken
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            Status = (int)TelegramPairingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        dbContext.TelegramPairingTokens.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return MapToken(entity);
    }

    public async Task<(TelegramPairingTokenResult Token, TelegramPairingCodeResult Code)> CreatePairingArtifactsAsync(
        Guid hogarId,
        Guid usuarioId,
        string tokenHash,
        DateTime tokenExpiresAt,
        string codeHash,
        DateTime codeExpiresAt,
        CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var createdAt = DateTime.UtcNow;

        var token = new TelegramPairingToken
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            UsuarioId = usuarioId,
            TokenHash = tokenHash,
            Status = (int)TelegramPairingStatus.Pending,
            CreatedAt = createdAt,
            ExpiresAt = tokenExpiresAt
        };

        var code = new TelegramPairingCode
        {
            Id = Guid.NewGuid(),
            HogarId = hogarId,
            UsuarioId = usuarioId,
            CodeHash = codeHash,
            Status = (int)TelegramPairingStatus.Pending,
            AttemptCount = 0,
            CreatedAt = createdAt,
            ExpiresAt = codeExpiresAt
        };

        try
        {
            dbContext.TelegramPairingTokens.Add(token);
            dbContext.TelegramPairingCodes.Add(code);
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsCodeHashUniqueViolation(ex))
        {
            throw new TelegramPairingCodeCollisionException();
        }

        return (MapToken(token), MapCode(code));
    }

    public async Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        var token = await dbContext.TelegramPairingTokens
            .SingleOrDefaultAsync(pairingToken => pairingToken.TokenHash == tokenHash, ct)
            ?? throw new TelegramPairingTokenNotFoundException();

        await AcquireIssuanceLockAsync(token.HogarId, token.UsuarioId, token.CreatedAt, transaction, ct);
        await dbContext.Entry(token).ReloadAsync(ct);

        var now = DateTime.UtcNow;

        if (token.RevokedAt.HasValue || token.Status == (int)TelegramPairingStatus.Revoked)
        {
            throw new TelegramPairingTokenRevokedException();
        }

        if (token.ConsumedAt.HasValue || token.Status == (int)TelegramPairingStatus.Consumed)
        {
            throw new TelegramPairingTokenAlreadyConsumedException();
        }

        if (token.ExpiresAt <= now || token.Status == (int)TelegramPairingStatus.Expired)
        {
            token.Status = (int)TelegramPairingStatus.Expired;
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new TelegramPairingTokenExpiredException();
        }

        try
        {
            await householdMembershipService.EnsureMemberAsync(
                token.UsuarioId,
                token.HogarId,
                static () => new TelegramHogarAccessDeniedException(),
                ct);
        }
        catch (TelegramHogarAccessDeniedException)
        {
            token.RevokedAt = now;
            token.Status = (int)TelegramPairingStatus.Revoked;
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw;
        }

        var activeLinks = await dbContext.TelegramChatLinks
            .Where(link => link.UnpairedAt == null
                && (link.ChatId == chatId
                    || (link.UsuarioId == token.UsuarioId && link.HogarId == token.HogarId)))
            .ToListAsync(ct);

        foreach (var activeLink in activeLinks)
        {
            activeLink.UnpairedAt = now;
        }

        var chatLink = new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UsuarioId = token.UsuarioId,
            HogarId = token.HogarId,
            PairedAt = now,
            UnpairedAt = null
        };

        token.ConsumedAt = now;
        token.Status = (int)TelegramPairingStatus.Consumed;
        await RevokeSiblingCodesAsync(token.HogarId, token.UsuarioId, token.CreatedAt, now, ct);

        dbContext.TelegramChatLinks.Add(chatLink);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CompleteTelegramPairingResult(chatId, token.HogarId, token.UsuarioId, now);
    }

    public async Task<CompleteTelegramPairingResult> CompletePairingByCodeAsync(string codeHash, long chatId, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        var code = await dbContext.TelegramPairingCodes
            .SingleOrDefaultAsync(pairingCode => pairingCode.CodeHash == codeHash, ct)
            ?? throw new TelegramPairingCodeNotFoundException();

        await AcquireIssuanceLockAsync(code.HogarId, code.UsuarioId, code.CreatedAt, transaction, ct);
        await dbContext.Entry(code).ReloadAsync(ct);

        var now = DateTime.UtcNow;

        if (code.Status == (int)TelegramPairingStatus.Revoked || code.RevokedAt.HasValue)
        {
            throw new TelegramPairingCodeRevokedException();
        }

        if (code.Status == (int)TelegramPairingStatus.Consumed || code.ConsumedAt.HasValue)
        {
            throw new TelegramPairingCodeRevokedException();
        }

        if (code.ExpiresAt <= now || code.Status == (int)TelegramPairingStatus.Expired)
        {
            code.Status = (int)TelegramPairingStatus.Expired;
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw new TelegramPairingCodeExpiredException();
        }

        try
        {
            await householdMembershipService.EnsureMemberAsync(
                code.UsuarioId,
                code.HogarId,
                static () => new TelegramHogarAccessDeniedException(),
                ct);
        }
        catch (TelegramHogarAccessDeniedException)
        {
            code.AttemptCount++;

            if (code.AttemptCount >= TelegramConstants.PairingCodeMaxAttempts)
            {
                code.RevokedAt = now;
                code.Status = (int)TelegramPairingStatus.Revoked;
                await dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                throw new TelegramPairingCodeRevokedException();
            }

            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            throw;
        }

        var activeLinks = await dbContext.TelegramChatLinks
            .Where(link => link.UnpairedAt == null
                && (link.ChatId == chatId
                    || (link.UsuarioId == code.UsuarioId && link.HogarId == code.HogarId)))
            .ToListAsync(ct);

        foreach (var activeLink in activeLinks)
        {
            activeLink.UnpairedAt = now;
        }

        var chatLink = new TelegramChatLink
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            UsuarioId = code.UsuarioId,
            HogarId = code.HogarId,
            PairedAt = now,
            UnpairedAt = null
        };

        code.ConsumedAt = now;
        code.Status = (int)TelegramPairingStatus.Consumed;
        await RevokeSiblingTokensAsync(code.HogarId, code.UsuarioId, code.CreatedAt, now, ct);

        dbContext.TelegramChatLinks.Add(chatLink);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CompleteTelegramPairingResult(chatId, code.HogarId, code.UsuarioId, now);
    }

    public async Task<UnlinkTelegramChatResult> UnlinkChatAsync(long chatId, CancellationToken ct)
    {
        var link = await dbContext.TelegramChatLinks
            .SingleOrDefaultAsync(currentLink => currentLink.ChatId == chatId && currentLink.UnpairedAt == null, ct)
            ?? throw new TelegramChatNotLinkedException();

        var now = DateTime.UtcNow;
        link.UnpairedAt = now;
        await dbContext.SaveChangesAsync(ct);

        return new UnlinkTelegramChatResult(link.ChatId, link.HogarId, link.UsuarioId, now);
    }

    public async Task<UnlinkTelegramChatResult> UnlinkActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
    {
        var link = await dbContext.TelegramChatLinks
            .SingleOrDefaultAsync(currentLink => currentLink.UsuarioId == usuarioId
                && currentLink.HogarId == hogarId
                && currentLink.UnpairedAt == null, ct)
            ?? throw new TelegramChatNotLinkedException();

        var now = DateTime.UtcNow;
        link.UnpairedAt = now;
        await dbContext.SaveChangesAsync(ct);

        return new UnlinkTelegramChatResult(link.ChatId, link.HogarId, link.UsuarioId, now);
    }

    public async Task<TelegramChatLinkResult?> GetActiveLinkAsync(Guid usuarioId, Guid hogarId, CancellationToken ct)
    {
        var link = await dbContext.TelegramChatLinks
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.UsuarioId == usuarioId && l.HogarId == hogarId && l.UnpairedAt == null, ct);

        return link is null
            ? null
            : new TelegramChatLinkResult(link.ChatId, link.UsuarioId, link.HogarId, link.PairedAt);
    }

    private static TelegramPairingTokenResult MapToken(TelegramPairingToken entity)
        => new(
            entity.Id,
            entity.HogarId,
            entity.UsuarioId,
            entity.CreatedAt,
            entity.ExpiresAt,
            entity.ConsumedAt,
            entity.RevokedAt,
            (TelegramPairingStatus)entity.Status);

    private static TelegramPairingCodeResult MapCode(TelegramPairingCode entity)
        => new(
            entity.Id,
            entity.HogarId,
            entity.UsuarioId,
            entity.AttemptCount,
            entity.CreatedAt,
            entity.ExpiresAt,
            entity.ConsumedAt,
            entity.RevokedAt,
            (TelegramPairingStatus)entity.Status);

    private async Task RevokeSiblingCodesAsync(Guid hogarId, Guid usuarioId, DateTime createdAt, DateTime revokedAt, CancellationToken ct)
    {
        var siblings = await dbContext.TelegramPairingCodes
            .Where(code => code.HogarId == hogarId
                && code.UsuarioId == usuarioId
                && code.CreatedAt == createdAt
                && code.ConsumedAt == null
                && code.RevokedAt == null
                && code.Status == (int)TelegramPairingStatus.Pending)
            .ToListAsync(ct);

        foreach (var sibling in siblings)
        {
            sibling.RevokedAt = revokedAt;
            sibling.Status = (int)TelegramPairingStatus.Revoked;
        }
    }

    private async Task RevokeSiblingTokensAsync(Guid hogarId, Guid usuarioId, DateTime createdAt, DateTime revokedAt, CancellationToken ct)
    {
        var siblings = await dbContext.TelegramPairingTokens
            .Where(token => token.HogarId == hogarId
                && token.UsuarioId == usuarioId
                && token.CreatedAt == createdAt
                && token.ConsumedAt == null
                && token.RevokedAt == null
                && token.Status == (int)TelegramPairingStatus.Pending)
            .ToListAsync(ct);

        foreach (var sibling in siblings)
        {
            sibling.RevokedAt = revokedAt;
            sibling.Status = (int)TelegramPairingStatus.Revoked;
        }
    }

    private async Task AcquireIssuanceLockAsync(
        Guid hogarId,
        Guid usuarioId,
        DateTime createdAt,
        IDbContextTransaction transaction,
        CancellationToken ct)
        => await AcquireLockAsync($"{hogarId:N}:{usuarioId:N}:{createdAt.Ticks}", transaction, ct);

    private async Task AcquireLockAsync(string hash, IDbContextTransaction transaction, CancellationToken ct)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(@lock_key)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.DbType = DbType.Int64;
        parameter.Value = ComputeLockKey(hash);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static long ComputeLockKey(string hash)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hash));
        return BitConverter.ToInt64(hashBytes, 0);
    }

    private static bool IsCodeHashUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == "uq_telegram_pairing_codes_hash";
}
