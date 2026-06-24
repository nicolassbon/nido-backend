using Nido.Application.Common.Images;

namespace Nido.Application.Alacena;

public sealed record ScanNutritionInfoCommand(
    Guid StockId,
    Guid HogarId,
    ImageUpload Image);

