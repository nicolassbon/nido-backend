using Nido.Application.Common.Images;

namespace Nido.Application.CatalogoElectrodomesticos.UploadCatalogImage;

public sealed record UploadCatalogImageCommand(Guid CatalogItemId, ImageUpload Image);
public sealed record UploadCatalogImageResult(string ImagenUrl);
public sealed record CatalogImageTarget(Guid Id, string? CurrentStorageKey);

public interface ICatalogImageRepository
{
    Task<CatalogImageTarget?> GetImageTargetAsync(Guid catalogItemId, CancellationToken cancellationToken);
    Task UpdateImageKeyAsync(Guid catalogItemId, string storageKey, CancellationToken cancellationToken);
}

public sealed class CatalogImageTargetNotFoundException : Exception
{
    public CatalogImageTargetNotFoundException() : base("Elemento de catálogo no encontrado.")
    {
    }
}
