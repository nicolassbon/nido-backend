namespace Nido.Application.Common.Storage;

public sealed class StorageKeyFactory
{
    public string ForProduct() => GuidBased("products");
    public string ForElectrodomestico() => GuidBased("electrodomesticos");
    public string ForRecipe() => GuidBased("recipes");
    public string ForCatalog() => GuidBased("catalog");
    public string ForAvatar(Guid userId) => $"avatars/{userId}.webp";

    private static string GuidBased(string folder) => $"{folder}/{Guid.NewGuid()}.webp";
}
