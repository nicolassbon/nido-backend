using Nido.Application.Common.Storage;

namespace Nido.Application.Tests.Common.Storage;

public sealed class StorageKeyFactoryTests
{
    [Theory]
    [InlineData("product", "products/")]
    [InlineData("electrodomestico", "electrodomesticos/")]
    [InlineData("recipe", "recipes/")]
    [InlineData("catalog", "catalog/")]
    public void GenerateKey_ForKnownFolder_ReturnsWebpKeyWithExpectedPrefix(string kind, string expectedPrefix)
    {
        var factory = new StorageKeyFactory();

        var key = kind switch
        {
            "product" => factory.ForProduct(),
            "recipe" => factory.ForRecipe(),
            "electrodomestico" => factory.ForElectrodomestico(),
            "catalog" => factory.ForCatalog(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        Assert.StartsWith(expectedPrefix, key, StringComparison.Ordinal);
        Assert.EndsWith(".webp", key, StringComparison.Ordinal);
        Assert.True(Guid.TryParse(key[expectedPrefix.Length..^5], out _));
    }

    [Fact]
    public void ForProduct_WhenCalledTwice_ReturnsUniqueKeys()
    {
        var factory = new StorageKeyFactory();

        var first = factory.ForProduct();
        var second = factory.ForProduct();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ForAvatar_ReturnsUserIdBasedKey()
    {
        var factory = new StorageKeyFactory();
        var userId = Guid.NewGuid();

        var key = factory.ForAvatar(userId);

        Assert.Equal($"avatars/{userId}.webp", key);
    }

    [Fact]
    public void ForAvatar_WithSameUserId_ReturnsSameKey()
    {
        var factory = new StorageKeyFactory();
        var userId = Guid.NewGuid();

        var first = factory.ForAvatar(userId);
        var second = factory.ForAvatar(userId);

        Assert.Equal(first, second);
    }
}
