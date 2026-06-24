using Nido.Application.Common.Assets;
using Nido.Application.Common.Images;
using Nido.Application.Common.Storage;

namespace Nido.Application.Recetas.UploadRecipeImage;

public sealed class UploadRecipeImageHandler(IRecipeImageRepository repository, IImageProcessingService imageProcessingService, IFileStorageService storageService, StorageKeyFactory storageKeyFactory, IPublicAssetUrlResolver publicAssetUrlResolver)
{
    public async Task<UploadRecipeImageResult> Handle(UploadRecipeImageCommand command, CancellationToken cancellationToken)
    {
        var target = await repository.GetImageTargetAsync(command.RecipeId, cancellationToken)
            ?? throw new RecipeImageTargetNotFoundException();

        var processed = await imageProcessingService.ProcessAsync(command.Image, cancellationToken);
        var key = storageKeyFactory.ForRecipe();

        await using var stream = processed.OpenReadStream();
        await storageService.UploadAsync(stream, key, processed.ContentType, cancellationToken);

        await repository.UpdateImageKeyAsync(command.RecipeId, key, cancellationToken);

        if (StorageKeyRules.IsStorageKey(target.CurrentStorageKey))
        {
            await storageService.DeleteAsync(target.CurrentStorageKey!, cancellationToken);
        }

        return new UploadRecipeImageResult(publicAssetUrlResolver.Resolve(key) ?? key);
    }
}
