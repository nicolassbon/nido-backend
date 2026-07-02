using Microsoft.Extensions.Options;
using Nido.Application.Common.ProfileImages;
using Nido.Infrastructure.ProfileImages;

namespace Nido.Infrastructure.Tests.ProfileImages;

public sealed class ConfigurableProfileImagePublicUrlResolverTests
{
    [Fact]
    public void Resolve_WhenStorageKeyIsNull_ReturnsNull()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://example.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve(null);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenStorageKeyIsEmpty_ReturnsNull()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://example.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WhenPublicBaseUrlIsMissing_ReturnsNull()
    {
        var options = Options.Create(new ProfileImageOptions());
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("usuarios/abc/profile/xyz.webp");

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_WithStorageKey_ReturnsFullUrl()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("usuarios/abc/profile/xyz.webp");

        Assert.Equal("https://nido-dev.nyc3.digitaloceanspaces.com/usuarios/abc/profile/xyz.webp", result);
    }

    [Fact]
    public void Resolve_WithPublicBaseUrlTrailingSlash_HandlesCorrectly()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com/" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("usuarios/abc/profile/xyz.webp");

        Assert.Equal("https://nido-dev.nyc3.digitaloceanspaces.com/usuarios/abc/profile/xyz.webp", result);
    }

    [Fact]
    public void Resolve_WithVersion_AppendsQueryParam()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);
        var version = new DateTimeOffset(2026, 6, 7, 22, 0, 0, TimeSpan.Zero);

        var result = resolver.Resolve("avatars/abc123.webp", version);

        Assert.StartsWith("https://nido-dev.nyc3.digitaloceanspaces.com/avatars/abc123.webp?v=", result);
    }

    [Fact]
    public void Resolve_WithoutVersion_DoesNotAppendQueryParam()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("avatars/abc123.webp");

        Assert.DoesNotContain("?v=", result);
    }

    [Fact]
    public void Resolve_WithNullVersion_DoesNotAppendQueryParam()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("avatars/abc123.webp", null);

        Assert.DoesNotContain("?v=", result);
    }

    [Fact]
    public void Resolve_WithHttpsExternalUrl_ReturnsTrimmedExternalUrl()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("  https://cdn.example.com/avatar.webp  ");

        Assert.Equal("https://cdn.example.com/avatar.webp", result);
    }

    [Fact]
    public void Resolve_WithHttpExternalUrl_ReturnsNull()
    {
        var options = Options.Create(new ProfileImageOptions { PublicBaseUrl = "https://nido-dev.nyc3.digitaloceanspaces.com" });
        var resolver = new ConfigurableProfileImagePublicUrlResolver(options);

        var result = resolver.Resolve("http://cdn.example.com/avatar.webp");

        Assert.Null(result);
    }
}
