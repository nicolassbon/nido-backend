using Nido.Application.Payments;

namespace Nido.Application.Tests.Payments;

public sealed class MercadoPagoWebhookSignatureVerifierTests
{
    [Fact]
    public void Verify_ValidMercadoPagoSignature_ReturnsTrue()
    {
        var verifier = new MercadoPagoWebhookSignatureVerifier();
        var signature = MercadoPagoSignatureTestHelper.BuildSignature("payment-123", "request-456", "1700000000", "secret");

        var result = verifier.Verify("payment-123", "request-456", signature, "secret");

        Assert.True(result);
    }

    [Theory]
    [InlineData("payment-123", "request-456", "ts=1700000000,v1=invalid")]
    [InlineData("payment-123", "", "ts=1700000000,v1=invalid")]
    [InlineData("", "request-456", "ts=1700000000,v1=invalid")]
    public void Verify_InvalidSignatureOrRequiredParts_ReturnsFalse(string dataId, string requestId, string signature)
    {
        var verifier = new MercadoPagoWebhookSignatureVerifier();

        var result = verifier.Verify(dataId, requestId, signature, "secret");

        Assert.False(result);
    }

    [Fact]
    public void Verify_DuplicateSignatureKeys_ReturnsFalse()
    {
        var verifier = new MercadoPagoWebhookSignatureVerifier();

        var result = verifier.Verify(
            "payment-123",
            "request-456",
            "ts=1700000000,ts=1700000001,v1=abc",
            "secret");

        Assert.False(result);
    }
}
