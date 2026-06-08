using Nido.Application.Common.Storage;

namespace Nido.Application.Tests.Common.Storage;

public sealed class StorageKeyRulesTests
{
    [Theory]
    [InlineData("products/example.webp")]
    [InlineData("electrodomesticos/example.webp")]
    [InlineData("recipes/example.webp")]
    [InlineData("catalog/example.webp")]
    [InlineData("avatars/example.webp")]
    public void IsStorageKey_WhenPrefixIsAllowed_ReturnsTrue(string key)
    {
        Assert.True(StorageKeyRules.IsStorageKey(key));
    }

    [Theory]
    [InlineData("https://cdn.example.com/products/example.webp")]
    [InlineData("/products/example.webp")]
    [InlineData("../products/example.webp")]
    [InlineData("unknown/example.webp")]
    public void IsStorageKey_WhenValueIsNotManagedKey_ReturnsFalse(string key)
    {
        Assert.False(StorageKeyRules.IsStorageKey(key));
    }
}
