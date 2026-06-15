using Microsoft.Extensions.Options;
using Nido.Infrastructure.PublicAssets;
using Nido.Infrastructure.Storage;

namespace Nido.Infrastructure.Tests.PublicAssets;

public sealed class SpacesPublicAssetUrlResolverTests
{
    [Theory]
    [InlineData("https://cdn.example.com/products/a.webp")]
    [InlineData("/images/local.png")]
    public void Resolve_WhenValueIsAlreadyUrlOrRootRelative_PassesThroughUnchanged(string value)
    {
        var resolver = CreateResolver("https://nido-dev.nyc3.digitaloceanspaces.com");

        var result = resolver.Resolve(value);

        Assert.Equal(value, result);
    }

    [Fact]
    public void Resolve_WhenValueIsStorageKey_ReturnsPublicUrl()
    {
        var resolver = CreateResolver("https://nido-dev.nyc3.digitaloceanspaces.com/");

        var result = resolver.Resolve("products/image.webp");

        Assert.Equal("https://nido-dev.nyc3.digitaloceanspaces.com/products/image.webp", result);
    }

    [Fact]
    public void Resolve_WhenValueIsNullOrBlank_ReturnsNull()
    {
        var resolver = CreateResolver("https://nido-dev.nyc3.digitaloceanspaces.com");

        Assert.Null(resolver.Resolve(null));
        Assert.Null(resolver.Resolve("   "));
    }

    private static SpacesPublicAssetUrlResolver CreateResolver(string publicBaseUrl)
        => new(Options.Create(new SpacesOptions { PublicBaseUrl = publicBaseUrl }));
}
