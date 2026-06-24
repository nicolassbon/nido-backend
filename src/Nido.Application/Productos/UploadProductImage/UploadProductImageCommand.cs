using Nido.Application.Common.Images;

namespace Nido.Application.Productos.UploadProductImage;

public sealed record UploadProductImageCommand(Guid ProductId, Guid HogarId, ImageUpload Image);

public sealed record UploadProductImageResult(string ImagenUrl);

public sealed record ProductImageTarget(Guid ProductId, string? CurrentStorageKey);

public interface IProductImageRepository
{
    Task<ProductImageTarget?> GetImageTargetAsync(Guid productId, Guid hogarId, CancellationToken cancellationToken);
    Task UpdateImageKeyAsync(Guid productId, Guid hogarId, string storageKey, CancellationToken cancellationToken);
}

public sealed class ProductImageTargetNotFoundException : Exception
{
    public ProductImageTargetNotFoundException() : base("Producto no encontrado.")
    {
    }
}
