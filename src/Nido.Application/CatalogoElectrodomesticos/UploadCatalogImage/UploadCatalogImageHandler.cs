using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;

namespace Nido.Application.CatalogoElectrodomesticos.UploadCatalogImage;

public sealed class UploadCatalogImageHandler(ICatalogImageRepository repository, IImageProcessingService imageProcessingService, IFileStorageService storageService, StorageKeyFactory storageKeyFactory, IPublicAssetUrlResolver publicAssetUrlResolver)
{
    public async Task<UploadCatalogImageResult> Handle(UploadCatalogImageCommand command, CancellationToken cancellationToken)
    {
        var target = await repository.GetImageTargetAsync(command.CatalogItemId, cancellationToken)
            ?? throw new CatalogImageTargetNotFoundException();

        var processed = await imageProcessingService.ProcessAsync(command.Image, cancellationToken);
        var key = storageKeyFactory.ForCatalog();

        await using var stream = processed.OpenReadStream();
        await storageService.UploadAsync(stream, key, processed.ContentType, cancellationToken);

        await repository.UpdateImageKeyAsync(command.CatalogItemId, key, cancellationToken);

        if (StorageKeyRules.IsStorageKey(target.CurrentStorageKey))
        {
            await storageService.DeleteAsync(target.CurrentStorageKey!, cancellationToken);
        }

        return new UploadCatalogImageResult(publicAssetUrlResolver.Resolve(key) ?? key);
    }
}
