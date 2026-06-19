using System.Data;
using System.Security.Cryptography;
using System.Text;
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

        return Map(entity);
    }

    public async Task<CompleteTelegramPairingResult> CompletePairingAsync(string tokenHash, long chatId, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        await AcquireTokenLockAsync(tokenHash, transaction, ct);

        var token = await dbContext.TelegramPairingTokens
            .SingleOrDefaultAsync(pairingToken => pairingToken.TokenHash == tokenHash, ct)
            ?? throw new TelegramPairingTokenNotFoundException();

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

        dbContext.TelegramChatLinks.Add(chatLink);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CompleteTelegramPairingResult(chatId, token.HogarId, token.UsuarioId, now);
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

    private static TelegramPairingTokenResult Map(TelegramPairingToken entity)
        => new(
            entity.Id,
            entity.HogarId,
            entity.UsuarioId,
            entity.CreatedAt,
            entity.ExpiresAt,
            entity.ConsumedAt,
            entity.RevokedAt,
            (TelegramPairingStatus)entity.Status);

    private async Task AcquireTokenLockAsync(string tokenHash, IDbContextTransaction transaction, CancellationToken ct)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(@lock_key)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.DbType = DbType.Int64;
        parameter.Value = ComputeLockKey(tokenHash);
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(ct);
    }

    private static long ComputeLockKey(string tokenHash)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tokenHash));
        return BitConverter.ToInt64(hash, 0);
    }
}
