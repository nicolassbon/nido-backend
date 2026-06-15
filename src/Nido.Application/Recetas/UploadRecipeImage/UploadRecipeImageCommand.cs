using Nido.Application.Common.Images;

namespace Nido.Application.Recetas.UploadRecipeImage;

public sealed record UploadRecipeImageCommand(Guid RecipeId, ImageUpload Image);
public sealed record UploadRecipeImageResult(string ImagenUrl);
public sealed record RecipeImageTarget(Guid Id, string? CurrentStorageKey);

public interface IRecipeImageRepository
{
    Task<RecipeImageTarget?> GetImageTargetAsync(Guid recipeId, CancellationToken cancellationToken);
    Task UpdateImageKeyAsync(Guid recipeId, string storageKey, CancellationToken cancellationToken);
}

public sealed class RecipeImageTargetNotFoundException : Exception
{
    public RecipeImageTargetNotFoundException() : base("Receta no encontrada.")
    {
    }
}
