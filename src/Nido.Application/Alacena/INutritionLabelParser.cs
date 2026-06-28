using Nido.Application.Common.Images;

namespace Nido.Application.Alacena;

public interface INutritionLabelParser
{
    Task<NutritionInfoResult> ParseAsync(ImageUpload image, CancellationToken cancellationToken);
}

