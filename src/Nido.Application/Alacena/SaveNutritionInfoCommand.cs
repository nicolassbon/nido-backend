namespace Nido.Application.Alacena;

public sealed record SaveNutritionInfoCommand(
    Guid StockId,
    Guid HogarId,
    SaveNutritionInfoRequestModel Nutrition);

