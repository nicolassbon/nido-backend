using Nido.Application.Auth;
using Nido.Infrastructure.Auth;

namespace Nido.Infrastructure.Tests.Auth;

public sealed class BcryptPasswordHasherTests
{
    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.Hash("Password123!");

        var result = hasher.Verify("Password123!", hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.Hash("Password123!");

        var result = hasher.Verify("WrongPassword", hash);

        Assert.False(result);
    }
}
