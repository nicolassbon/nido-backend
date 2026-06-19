using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;
using Nido.Application.Productos.UploadProductImage;

namespace Nido.Application.Tests.Productos;

public sealed class UploadProductImageHandlerTests
{
    [Fact]
    public async Task Handle_WhenUploadSucceeds_SavesGeneratedStorageKeyAndReturnsResolvedUrl()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakeProductImageRepository { ExistingImageKey = "products/old.webp", ExpectedHogarId = hogarId };
        var storage = new FakeFileStorageService();
        var handler = new UploadProductImageHandler(
            repository,
            new FakeImageProcessingService(),
            storage,
            new StorageKeyFactory(),
            new FakePublicAssetUrlResolver());

        var result = await handler.Handle(
            new UploadProductImageCommand(Guid.NewGuid(), hogarId, new ImageUpload("image.png", "image/png", [1, 2, 3])),
            CancellationToken.None);

        Assert.StartsWith("products/", repository.SavedImageKey, StringComparison.Ordinal);
        Assert.EndsWith(".webp", repository.SavedImageKey, StringComparison.Ordinal);
        Assert.Equal(repository.SavedImageKey, storage.UploadedKey);
        Assert.Equal(repository.ExpectedHogarId, repository.UpdatedHogarId);
        Assert.Equal("products/old.webp", storage.DeletedKey);
        Assert.Equal($"https://assets.test/{repository.SavedImageKey}", result.ImagenUrl);
    }

    [Fact]
    public async Task Handle_WhenStorageFails_DoesNotSaveImageKey()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakeProductImageRepository { ExpectedHogarId = hogarId };
        var storage = new FakeFileStorageService { ThrowOnUpload = true };
        var handler = new UploadProductImageHandler(
            repository,
            new FakeImageProcessingService(),
            storage,
            new StorageKeyFactory(),
            new FakePublicAssetUrlResolver());

        await Assert.ThrowsAsync<ImageStorageFailureException>(() => handler.Handle(
            new UploadProductImageCommand(Guid.NewGuid(), hogarId, new ImageUpload("image.png", "image/png", [1, 2, 3])),
            CancellationToken.None));

        Assert.Null(repository.SavedImageKey);
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ThrowsNotFoundBeforeUpload()
    {
        var hogarId = Guid.NewGuid();
        var repository = new FakeProductImageRepository { Exists = false, ExpectedHogarId = hogarId };
        var storage = new FakeFileStorageService();
        var handler = new UploadProductImageHandler(
            repository,
            new FakeImageProcessingService(),
            storage,
            new StorageKeyFactory(),
            new FakePublicAssetUrlResolver());

        await Assert.ThrowsAsync<ProductImageTargetNotFoundException>(() => handler.Handle(
            new UploadProductImageCommand(Guid.NewGuid(), hogarId, new ImageUpload("image.png", "image/png", [1, 2, 3])),
            CancellationToken.None));

        Assert.Null(storage.UploadedKey);
    }

    private sealed class FakeProductImageRepository : IProductImageRepository
    {
        public bool Exists { get; init; } = true;
        public string? ExistingImageKey { get; init; }
        public Guid ExpectedHogarId { get; init; } = Guid.NewGuid();
        public string? SavedImageKey { get; private set; }
        public Guid? UpdatedHogarId { get; private set; }

        public Task<ProductImageTarget?> GetImageTargetAsync(Guid productId, Guid hogarId, CancellationToken cancellationToken)
            => Task.FromResult(Exists && hogarId == ExpectedHogarId ? new ProductImageTarget(productId, ExistingImageKey) : null);

        public Task UpdateImageKeyAsync(Guid productId, Guid hogarId, string storageKey, CancellationToken cancellationToken)
        {
            UpdatedHogarId = hogarId;
            SavedImageKey = storageKey;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeImageProcessingService : IImageProcessingService
    {
        public Task<ProcessedImage> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessedImage([4, 5, 6], "image/webp", 10, 10, 3));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public bool ThrowOnUpload { get; init; }
        public string? UploadedKey { get; private set; }
        public string? DeletedKey { get; private set; }

        public Task<FileStorageUploadResult> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            if (ThrowOnUpload)
            {
                throw new ImageStorageFailureException();
            }

            UploadedKey = key;
            return Task.FromResult(new FileStorageUploadResult(key, $"https://assets.test/{key}"));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKey = key;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublicAssetUrlResolver : IPublicAssetUrlResolver
    {
        public string? Resolve(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : $"https://assets.test/{value}";
    }
}
