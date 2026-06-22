namespace Nido.Application.Alacena;

public sealed class SaveNutritionInfoHandler
{
    private readonly INutritionInfoRepository _nutritionRepository;

    public SaveNutritionInfoHandler(INutritionInfoRepository nutritionRepository)
    {
        _nutritionRepository = nutritionRepository;
    }

    public Task<NutritionInfoResult?> Handle(SaveNutritionInfoCommand command, CancellationToken ct)
        => _nutritionRepository.SaveForStockAsync(command.StockId, command.HogarId, command.Nutrition, ct);
}

