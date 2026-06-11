using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;

namespace Nido.Application.Productos.UploadProductImage;

public sealed class UploadProductImageHandler
{
    private readonly IProductImageRepository _repository;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IFileStorageService _storageService;
    private readonly StorageKeyFactory _storageKeyFactory;
    private readonly IPublicAssetUrlResolver _publicAssetUrlResolver;

    public UploadProductImageHandler(
        IProductImageRepository repository,
        IImageProcessingService imageProcessingService,
        IFileStorageService storageService,
        StorageKeyFactory storageKeyFactory,
        IPublicAssetUrlResolver publicAssetUrlResolver)
    {
        _repository = repository;
        _imageProcessingService = imageProcessingService;
        _storageService = storageService;
        _storageKeyFactory = storageKeyFactory;
        _publicAssetUrlResolver = publicAssetUrlResolver;
    }

    public async Task<UploadProductImageResult> Handle(UploadProductImageCommand command, CancellationToken cancellationToken)
    {
        var target = await _repository.GetImageTargetAsync(command.ProductId, command.HogarId, cancellationToken)
            ?? throw new ProductImageTargetNotFoundException();

        var processed = await _imageProcessingService.ProcessAsync(command.Image, cancellationToken);
        var key = _storageKeyFactory.ForProduct();

        await using var stream = processed.OpenReadStream();
        await _storageService.UploadAsync(stream, key, processed.ContentType, cancellationToken);

        await _repository.UpdateImageKeyAsync(command.ProductId, command.HogarId, key, cancellationToken);

        if (StorageKeyRules.IsStorageKey(target.CurrentStorageKey))
        {
            await _storageService.DeleteAsync(target.CurrentStorageKey!, cancellationToken);
        }

        return new UploadProductImageResult(_publicAssetUrlResolver.Resolve(key) ?? key);
    }
}
