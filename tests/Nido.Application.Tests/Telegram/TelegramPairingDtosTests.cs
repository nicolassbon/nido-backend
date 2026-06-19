using Nido.Application.Telegram;
using Nido.Application.Telegram.Pairing;
using Xunit;

namespace Nido.Application.Tests.Telegram;

public sealed class TelegramPairingDtosTests
{
    [Fact]
    public void TelegramPairingTokenResult_RoundTripsValues()
    {
        var id = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddMinutes(-2);
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var result = new TelegramPairingTokenResult(
            id, hogarId, usuarioId, createdAt, expiresAt, ConsumedAt: null, RevokedAt: null, TelegramPairingStatus.Pending);

        Assert.Equal(id, result.Id);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal(usuarioId, result.UsuarioId);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(expiresAt, result.ExpiresAt);
        Assert.Null(result.ConsumedAt);
        Assert.Null(result.RevokedAt);
        Assert.Equal(TelegramPairingStatus.Pending, result.Status);
    }

    [Fact]
    public void TelegramPairingCodeResult_RoundTripsValues()
    {
        var id = Guid.NewGuid();
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var result = new TelegramPairingCodeResult(
            id, hogarId, usuarioId, AttemptCount: 0, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10),
            ConsumedAt: null, RevokedAt: null, TelegramPairingStatus.Pending);

        Assert.Equal(id, result.Id);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal(usuarioId, result.UsuarioId);
        Assert.Equal(0, result.AttemptCount);
        Assert.Equal(TelegramPairingStatus.Pending, result.Status);
    }

    [Fact]
    public void ValidatePairingCodeRequest_RoundTripsValues()
    {
        var request = new ValidatePairingCodeRequest(ChatId: 123, SubmittedCode: "482917");

        Assert.Equal(123, request.ChatId);
        Assert.Equal("482917", request.SubmittedCode);
    }

    [Fact]
    public void StartTelegramPairingResult_RoundTripsValues()
    {
        var deepLink = "https://t.me/nido_bot?start=abc";
        var pairingCode = "123456";
        var tokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
        var codeExpiresAt = DateTime.UtcNow.AddMinutes(15);

        var result = new StartTelegramPairingResult(deepLink, pairingCode, tokenExpiresAt, codeExpiresAt);

        Assert.Equal(deepLink, result.DeepLinkUrl);
        Assert.Equal(pairingCode, result.PairingCode);
        Assert.Equal(tokenExpiresAt, result.TokenExpiresAt);
        Assert.Equal(codeExpiresAt, result.CodeExpiresAt);
        Assert.Equal(codeExpiresAt, result.ExpiresAt);
    }

    [Fact]
    public void CompleteTelegramPairingByCodeCommand_RoundTripsValues()
    {
        var command = new CompleteTelegramPairingByCodeCommand(123, "482917");

        Assert.Equal(123, command.ChatId);
        Assert.Equal("482917", command.Code);
    }

    [Fact]
    public void CompleteTelegramPairingResult_RoundTripsValues()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var pairedAt = DateTime.UtcNow;

        var result = new CompleteTelegramPairingResult(999, hogarId, usuarioId, pairedAt);

        Assert.Equal(999, result.ChatId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal(usuarioId, result.UsuarioId);
        Assert.Equal(pairedAt, result.PairedAt);
    }

    [Fact]
    public void UnlinkTelegramChatResult_RoundTripsValues()
    {
        var hogarId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var unpairedAt = DateTime.UtcNow;

        var result = new UnlinkTelegramChatResult(999, hogarId, usuarioId, unpairedAt);

        Assert.Equal(999, result.ChatId);
        Assert.Equal(hogarId, result.HogarId);
        Assert.Equal(usuarioId, result.UsuarioId);
        Assert.Equal(unpairedAt, result.UnpairedAt);
    }
}
